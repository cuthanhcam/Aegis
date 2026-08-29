using Npgsql;

namespace Aegis.Infrastructure.Persistence;

public sealed record StoreConstraintViolationSample(string TenantId, string StoreId);

public sealed record StoreConstraintTableReport(
    string Table,
    string Constraint,
    bool Validated,
    long ViolationCount,
    IReadOnlyList<StoreConstraintViolationSample> Samples);

public sealed record StoreConstraintReconciliationReport(
    DateTimeOffset GeneratedAt,
    string Database,
    bool ValidationRequested,
    bool ValidationCompleted,
    long TotalViolations,
    IReadOnlyList<StoreConstraintTableReport> Tables);

public sealed class StoreConstraintReconciliationService
{
    private const int SampleLimit = 20;
    private static readonly (string Table, string Constraint)[] Targets =
    [
        ("relationships", "fk_relationships_store"),
        ("relationship_changes", "fk_relationship_changes_store"),
        ("rbac_roles", "fk_rbac_roles_store"),
        ("rbac_permissions", "fk_rbac_permissions_store"),
        ("rbac_role_permissions", "fk_rbac_role_permissions_store"),
        ("rbac_user_roles", "fk_rbac_user_roles_store"),
    ];

    private readonly NpgsqlDataSource _dataSource;

    public StoreConstraintReconciliationService(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<StoreConstraintReconciliationReport> AuditAsync(
        bool validate,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var database = connection.Database;
        var tables = await ReadReportsAsync(connection, cancellationToken);
        var totalViolations = tables.Sum(x => x.ViolationCount);
        var validationCompleted = false;

        if (validate)
        {
            if (totalViolations > 0)
            {
                return CreateReport(database, true, false, totalViolations, tables);
            }

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            foreach (var target in Targets)
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $"ALTER TABLE {target.Table} VALIDATE CONSTRAINT {target.Constraint};";
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            validationCompleted = true;
            tables = await ReadReportsAsync(connection, cancellationToken);
        }

        return CreateReport(database, validate, validationCompleted, totalViolations, tables);
    }

    private static StoreConstraintReconciliationReport CreateReport(
        string database,
        bool validationRequested,
        bool validationCompleted,
        long totalViolations,
        IReadOnlyList<StoreConstraintTableReport> tables)
    {
        return new StoreConstraintReconciliationReport(
            DateTimeOffset.UtcNow,
            database,
            validationRequested,
            validationCompleted,
            totalViolations,
            tables);
    }

    private static async Task<IReadOnlyList<StoreConstraintTableReport>> ReadReportsAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        var reports = new List<StoreConstraintTableReport>(Targets.Length);
        foreach (var target in Targets)
        {
            await using var countCommand = connection.CreateCommand();
            countCommand.CommandText = $"""
                SELECT COUNT(*)
                FROM {target.Table} AS child
                LEFT JOIN stores AS parent
                  ON parent.tenant_id = child.tenant_id
                 AND parent.id = child.store_id
                WHERE parent.id IS NULL;
                """;
            var violationCount = (long)(await countCommand.ExecuteScalarAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Could not count violations for {target.Table}."));

            await using var sampleCommand = connection.CreateCommand();
            sampleCommand.CommandText = $"""
                SELECT child.tenant_id, child.store_id
                FROM {target.Table} AS child
                LEFT JOIN stores AS parent
                  ON parent.tenant_id = child.tenant_id
                 AND parent.id = child.store_id
                WHERE parent.id IS NULL
                ORDER BY child.tenant_id, child.store_id
                LIMIT {SampleLimit};
                """;
            var samples = new List<StoreConstraintViolationSample>();
            await using (var reader = await sampleCommand.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    samples.Add(new StoreConstraintViolationSample(reader.GetString(0), reader.GetString(1)));
                }
            }

            await using var statusCommand = connection.CreateCommand();
            statusCommand.CommandText = """
                SELECT constraint_row.convalidated
                FROM pg_constraint AS constraint_row
                WHERE constraint_row.conname = @constraint_name
                  AND constraint_row.conrelid = @table_name::regclass;
                """;
            statusCommand.Parameters.AddWithValue("constraint_name", target.Constraint);
            statusCommand.Parameters.AddWithValue("table_name", target.Table);
            var validated = (bool)(await statusCommand.ExecuteScalarAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Constraint {target.Constraint} was not found."));

            reports.Add(new StoreConstraintTableReport(
                target.Table,
                target.Constraint,
                validated,
                violationCount,
                samples));
        }

        return reports;
    }
}
