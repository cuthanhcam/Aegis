using Npgsql;
using System.Reflection;

namespace Aegis.Infrastructure.Persistence
{
    internal static class PostgresMigrationRunner
    {
        private const string MigrationHistoryTable = "schema_migrations";

        public static async Task MigrateAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken = default)
        {
            var migrationResources = typeof(PostgresMigrationRunner).Assembly
                .GetManifestResourceNames()
                .Where(name => name.Contains("Persistence.Migrations", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using (var historyCommand = connection.CreateCommand())
            {
                historyCommand.CommandText = $@"CREATE TABLE IF NOT EXISTS {MigrationHistoryTable} (
                    migration_name TEXT PRIMARY KEY,
                    applied_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );";
                await historyCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            var applied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var selectCommand = connection.CreateCommand())
            {
                selectCommand.CommandText = $"SELECT migration_name FROM {MigrationHistoryTable};";
                await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    applied.Add(reader.GetString(0));
                }
            }

            foreach (var resourceName in migrationResources)
            {
                var migrationName = Path.GetFileNameWithoutExtension(resourceName);
                if (applied.Contains(migrationName))
                {
                    continue;
                }

                var sql = await ReadResourceAsync(resourceName, cancellationToken);
                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                await using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = sql;
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                await using (var recordCommand = connection.CreateCommand())
                {
                    recordCommand.Transaction = transaction;
                    recordCommand.CommandText = $"INSERT INTO {MigrationHistoryTable} (migration_name) VALUES (@migration_name);";
                    recordCommand.Parameters.AddWithValue("migration_name", migrationName);
                    await recordCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }
        }

        private static async Task<string> ReadResourceAsync(string resourceName, CancellationToken cancellationToken)
        {
            await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Migration resource '{resourceName}' was not found.");

            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(cancellationToken);
        }
    }
}
