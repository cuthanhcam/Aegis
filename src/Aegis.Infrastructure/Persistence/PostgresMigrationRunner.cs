using Npgsql;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Aegis.Infrastructure.Persistence;

public sealed record PostgresMigrationOptions(TimeSpan LockTimeout, TimeSpan StatementTimeout)
{
    public static PostgresMigrationOptions Default { get; } = new(TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2));
}

public static class PostgresMigrationRunner
{
    private const string MigrationHistoryTable = "schema_migrations";
    private const long AdvisoryLockKey = 6_344_347_956_172_591_941L;

    public static Task MigrateAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken = default) =>
        MigrateAsync(dataSource, PostgresMigrationOptions.Default, cancellationToken);

    public static async Task MigrateAsync(
        NpgsqlDataSource dataSource,
        PostgresMigrationOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        ArgumentNullException.ThrowIfNull(options);
        if (options.LockTimeout <= TimeSpan.Zero || options.StatementTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Migration timeouts must be positive.");
        }

        var migrations = await ReadMigrationsAsync(cancellationToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var lockAcquired = false;
        try
        {
            lockAcquired = await AcquireLockAsync(connection, options.LockTimeout, cancellationToken);
            if (!lockAcquired)
            {
                throw new TimeoutException($"Could not acquire the Aegis migration lock within {options.LockTimeout}.");
            }

            await EnsureHistoryTableAsync(connection, options.StatementTimeout, cancellationToken);
            var applied = await ReadAppliedAsync(connection, options.StatementTimeout, cancellationToken);
            await VerifyAndBootstrapChecksumsAsync(connection, migrations, applied, options.StatementTimeout, cancellationToken);

            foreach (var migration in migrations)
            {
                if (applied.ContainsKey(migration.Name))
                {
                    continue;
                }

                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                await using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandTimeout = ToCommandTimeout(options.StatementTimeout);
                    command.CommandText = migration.Sql;
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                await using (var recordCommand = connection.CreateCommand())
                {
                    recordCommand.Transaction = transaction;
                    recordCommand.CommandTimeout = ToCommandTimeout(options.StatementTimeout);
                    recordCommand.CommandText = $"INSERT INTO {MigrationHistoryTable} (migration_name, checksum_sha256) VALUES (@migration_name, @checksum_sha256);";
                    recordCommand.Parameters.AddWithValue("migration_name", migration.Name);
                    recordCommand.Parameters.AddWithValue("checksum_sha256", migration.Checksum);
                    await recordCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }
        }
        finally
        {
            if (lockAcquired)
            {
                await ReleaseLockAsync(connection);
            }
        }
    }

    private static async Task<bool> AcquireLockAsync(NpgsqlConnection connection, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        do
        {
            await using var command = connection.CreateCommand();
            command.CommandTimeout = Math.Max(1, Math.Min(ToCommandTimeout(timeout), 5));
            command.CommandText = "SELECT pg_try_advisory_lock(@lock_key);";
            command.Parameters.AddWithValue("lock_key", AdvisoryLockKey);
            if ((bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
        }
        while (DateTimeOffset.UtcNow < deadline);

        return false;
    }

    private static async Task ReleaseLockAsync(NpgsqlConnection connection)
    {
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandTimeout = 5;
            command.CommandText = "SELECT pg_advisory_unlock(@lock_key);";
            command.Parameters.AddWithValue("lock_key", AdvisoryLockKey);
            await command.ExecuteScalarAsync(CancellationToken.None);
        }
        catch
        {
            // Closing the session releases the advisory lock; preserve the original migration outcome.
        }
    }

    private static async Task EnsureHistoryTableAsync(NpgsqlConnection connection, TimeSpan timeout, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandTimeout = ToCommandTimeout(timeout);
        command.CommandText = $"""
            CREATE TABLE IF NOT EXISTS {MigrationHistoryTable} (
                migration_name TEXT PRIMARY KEY,
                checksum_sha256 CHAR(64) NULL,
                applied_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            ALTER TABLE {MigrationHistoryTable}
                ADD COLUMN IF NOT EXISTS checksum_sha256 CHAR(64) NULL;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<Dictionary<string, string?>> ReadAppliedAsync(NpgsqlConnection connection, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var applied = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = ToCommandTimeout(timeout);
        command.CommandText = $"SELECT migration_name, checksum_sha256 FROM {MigrationHistoryTable};";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            applied.Add(reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1).Trim());
        }

        return applied;
    }

    private static async Task VerifyAndBootstrapChecksumsAsync(
        NpgsqlConnection connection,
        IReadOnlyList<MigrationResource> migrations,
        IReadOnlyDictionary<string, string?> applied,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var known = migrations.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var record in applied)
        {
            if (!known.TryGetValue(record.Key, out var migration))
            {
                throw new InvalidOperationException($"Applied migration '{record.Key}' is not embedded in this Aegis build. Migration history is append-only.");
            }

            if (record.Value is not null && !string.Equals(record.Value, migration.Checksum, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Checksum mismatch for applied migration '{record.Key}'. Applied migrations must not be modified.");
            }

            if (record.Value is not null)
            {
                continue;
            }

            await using var update = connection.CreateCommand();
            update.CommandTimeout = ToCommandTimeout(timeout);
            update.CommandText = $"UPDATE {MigrationHistoryTable} SET checksum_sha256 = @checksum WHERE migration_name = @name AND checksum_sha256 IS NULL;";
            update.Parameters.AddWithValue("checksum", migration.Checksum);
            update.Parameters.AddWithValue("name", migration.Name);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<IReadOnlyList<MigrationResource>> ReadMigrationsAsync(CancellationToken cancellationToken)
    {
        var resourceNames = typeof(PostgresMigrationRunner).Assembly.GetManifestResourceNames()
            .Where(name => name.Contains("Persistence.Migrations", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
        var migrations = new List<MigrationResource>();
        foreach (var resourceName in resourceNames)
        {
            var sql = await ReadResourceAsync(resourceName, cancellationToken);
            var normalized = sql.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
            var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
            migrations.Add(new MigrationResource(Path.GetFileNameWithoutExtension(resourceName), sql, checksum));
        }

        return migrations;
    }

    private static async Task<string> ReadResourceAsync(string resourceName, CancellationToken cancellationToken)
    {
        await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Migration resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static int ToCommandTimeout(TimeSpan timeout) => Math.Max(1, checked((int)Math.Ceiling(timeout.TotalSeconds)));

    private sealed record MigrationResource(string Name, string Sql, string Checksum);
}
