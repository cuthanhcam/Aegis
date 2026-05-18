using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Npgsql;

namespace Aegis.Infrastructure.Authorization
{
    public sealed class PostgresAuditStore : IAuditStore
    {
        private readonly NpgsqlDataSource _dataSource;

        public PostgresAuditStore(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            const string sql = @"INSERT INTO audit_events (id, tenant_id, action, subject, relation, object_ref, decision, reason_code, created_at)
                                 VALUES (@id, @tenant_id, @action, @subject, @relation, @object_ref, @decision, @reason_code, @created_at);";

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", Guid.NewGuid());
            command.Parameters.AddWithValue("tenant_id", auditEvent.TenantId);
            command.Parameters.AddWithValue("action", auditEvent.Action);
            command.Parameters.AddWithValue("subject", auditEvent.Subject);
            command.Parameters.AddWithValue("relation", auditEvent.Relation);
            command.Parameters.AddWithValue("object_ref", auditEvent.Object);
            command.Parameters.AddWithValue("decision", auditEvent.Decision);
            command.Parameters.AddWithValue("reason_code", auditEvent.ReasonCode);
            command.Parameters.AddWithValue("created_at", auditEvent.CreatedAt);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<AuditEvent>> QueryAsync(string tenantId, string? action, string? decision, CancellationToken cancellationToken = default)
        {
            const string sql = @"SELECT action, subject, relation, object_ref, decision, reason_code, created_at
                                 FROM audit_events
                                 WHERE tenant_id = @tenant_id
                                   AND (@action IS NULL OR action = @action)
                                   AND (@decision IS NULL OR decision = @decision)
                                 ORDER BY created_at DESC;";

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("tenant_id", tenantId);
            command.Parameters.AddWithValue("action", (object?)action ?? DBNull.Value);
            command.Parameters.AddWithValue("decision", (object?)decision ?? DBNull.Value);

            var results = new List<AuditEvent>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new AuditEvent(
                    tenantId,
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5),
                    reader.GetFieldValue<DateTimeOffset>(6)));
            }

            return results;
        }
    }
}
