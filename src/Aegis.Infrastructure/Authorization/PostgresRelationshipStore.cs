using Aegis.Authorization.Caching;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Domain.Repositories;
using Npgsql;
using DomainRelationship = Aegis.Domain.Entities.Relationship;
using DomainRelationshipChange = Aegis.Domain.Entities.RelationshipChangeEntry;
using DomainRelationshipPermissionEffect = Aegis.Domain.Enums.RelationshipPermissionEffect;

namespace Aegis.Infrastructure.Authorization
{
    public sealed class PostgresRelationshipStore : IRelationshipStore, IRelationshipRepository
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly AuthorizationCache? _authorizationCache;

        public PostgresRelationshipStore(NpgsqlDataSource dataSource, AuthorizationCache? authorizationCache = null)
        {
            _dataSource = dataSource;
            _authorizationCache = authorizationCache;
        }

        public async Task<IReadOnlyList<RelationshipTuple>> QueryAsync(string tenantId, Subject? subject, string? relation, ObjectRef? obj, RelationshipEffect? effect, CancellationToken cancellationToken = default, string? storeId = null)
        {
            var effectiveStoreId = ResolveStoreId(tenantId, storeId);
            const string sql = @"SELECT subject, relation, object_ref, effect, created_at
                                 FROM relationships
                                 WHERE tenant_id = @tenant_id
                                   AND store_id = @store_id
                                   AND (@subject IS NULL OR subject = @subject)
                                   AND (@relation IS NULL OR relation = @relation)
                                   AND (@object_ref IS NULL OR object_ref = @object_ref)
                                   AND (@effect IS NULL OR effect = @effect)
                                 ORDER BY created_at DESC;";

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("tenant_id", tenantId);
            command.Parameters.AddWithValue("store_id", effectiveStoreId);
            command.Parameters.AddWithValue("subject", (object?)subject?.Value ?? DBNull.Value);
            command.Parameters.AddWithValue("relation", (object?)relation ?? DBNull.Value);
            command.Parameters.AddWithValue("object_ref", (object?)obj?.Value ?? DBNull.Value);
            command.Parameters.AddWithValue("effect", (object?)effect?.ToString() ?? DBNull.Value);

            var results = new List<RelationshipTuple>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new RelationshipTuple(
                    new Subject(reader.GetString(0)),
                    reader.GetString(1),
                    new ObjectRef(reader.GetString(2)),
                    Enum.Parse<RelationshipEffect>(reader.GetString(3), ignoreCase: true),
                    reader.GetFieldValue<DateTimeOffset>(4)));
            }

            return results;
        }

        public async Task<IReadOnlyList<IReadOnlyList<RelationshipTuple>>> QueryMultipleAsync(
            string tenantId,
            IReadOnlyList<(Subject? subject, string? relation, ObjectRef? obj, RelationshipEffect? effect)> queries,
            CancellationToken cancellationToken = default,
            string? storeId = null)
        {
            if (queries.Count == 0)
            {
                return Array.Empty<IReadOnlyList<RelationshipTuple>>();
            }

            var effectiveStoreId = ResolveStoreId(tenantId, storeId);
            var sqlBuilder = new System.Text.StringBuilder();
            sqlBuilder.Append(@"SELECT subject, relation, object_ref, effect, created_at
                               FROM relationships
                               WHERE tenant_id = @tenant_id AND store_id = @store_id AND (");

            for (int i = 0; i < queries.Count; i++)
            {
                if (i > 0)
                    sqlBuilder.Append(" OR ");

                var (subject, relation, obj, effect) = queries[i];
                var conditions = new List<string>();
                if (subject is not null)
                    conditions.Add($"subject = @subject_{i}");
                if (relation is not null)
                    conditions.Add($"relation = @relation_{i}");
                if (obj is not null)
                    conditions.Add($"object_ref = @obj_{i}");
                if (effect is not null)
                    conditions.Add($"effect = @effect_{i}");

                if (conditions.Count == 0)
                {
                    sqlBuilder.Append("true");
                }
                else
                {
                    sqlBuilder.Append("(").Append(string.Join(" AND ", conditions)).Append(")");
                }
            }

            sqlBuilder.Append(@")
                                 ORDER BY created_at DESC;");

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sqlBuilder.ToString(), connection);
            command.Parameters.AddWithValue("tenant_id", tenantId);
            command.Parameters.AddWithValue("store_id", effectiveStoreId);

            for (int i = 0; i < queries.Count; i++)
            {
                var (subject, relation, obj, effect) = queries[i];
                if (subject is not null)
                    command.Parameters.AddWithValue($"subject_{i}", subject.Value);
                if (relation is not null)
                    command.Parameters.AddWithValue($"relation_{i}", relation);
                if (obj is not null)
                    command.Parameters.AddWithValue($"obj_{i}", obj.Value);
                if (effect is not null)
                    command.Parameters.AddWithValue($"effect_{i}", effect.ToString()!);
            }

            var allResults = new List<RelationshipTuple>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                allResults.Add(new RelationshipTuple(
                    new Subject(reader.GetString(0)),
                    reader.GetString(1),
                    new ObjectRef(reader.GetString(2)),
                    Enum.Parse<RelationshipEffect>(reader.GetString(3), ignoreCase: true),
                    reader.GetFieldValue<DateTimeOffset>(4)));
            }

            // Group results by matching query — for simplicity, just run individual queries to match results back
            // (A more sophisticated approach would tag each result with its matching query index)
            var results = new List<IReadOnlyList<RelationshipTuple>>(queries.Count);
            for (int i = 0; i < queries.Count; i++)
            {
                var (subject, relation, obj, effect) = queries[i];
                var matched = allResults
                    .Where(t => subject is null || t.Subject == subject)
                    .Where(t => relation is null || t.Relation.Equals(relation, StringComparison.OrdinalIgnoreCase))
                    .Where(t => obj is null || t.Object == obj)
                    .Where(t => effect is null || t.Effect == effect)
                    .ToList();
                results.Add(matched);
            }

            return results;
        }

        public async Task UpsertAsync(string tenantId, RelationshipTuple tuple, CancellationToken cancellationToken = default, string? storeId = null)
        {
            var effectiveStoreId = ResolveStoreId(tenantId, storeId);
            const string sql = @"INSERT INTO relationships (id, tenant_id, store_id, subject, relation, object_ref, effect, created_at, updated_at)
                                 VALUES (@id, @tenant_id, @store_id, @subject, @relation, @object_ref, @effect, @created_at, @updated_at)
                                 ON CONFLICT (tenant_id, store_id, subject, relation, object_ref)
                                 DO UPDATE SET effect = EXCLUDED.effect, updated_at = EXCLUDED.updated_at;";

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Transaction = transaction;
            command.Parameters.AddWithValue("id", Guid.NewGuid());
            command.Parameters.AddWithValue("tenant_id", tenantId);
            command.Parameters.AddWithValue("store_id", effectiveStoreId);
            command.Parameters.AddWithValue("subject", tuple.Subject.Value);
            command.Parameters.AddWithValue("relation", tuple.Relation);
            command.Parameters.AddWithValue("object_ref", tuple.Object.Value);
            command.Parameters.AddWithValue("effect", tuple.Effect.ToString());
            command.Parameters.AddWithValue("created_at", tuple.CreatedAt);
            command.Parameters.AddWithValue("updated_at", tuple.CreatedAt);
            await command.ExecuteNonQueryAsync(cancellationToken);

            await WriteChangeAsync(connection, transaction, tenantId, effectiveStoreId, tuple.Subject.Value, tuple.Relation, tuple.Object.Value, "upsert", tuple.CreatedAt, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _authorizationCache?.InvalidateTenant(tenantId);
        }

        public async Task<bool> DeleteAsync(string tenantId, Subject subject, string relation, ObjectRef obj, CancellationToken cancellationToken = default, string? storeId = null)
        {
            var effectiveStoreId = ResolveStoreId(tenantId, storeId);
            const string sql = "DELETE FROM relationships WHERE tenant_id = @tenant_id AND store_id = @store_id AND subject = @subject AND relation = @relation AND object_ref = @object_ref RETURNING 1;";

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Transaction = transaction;
            command.Parameters.AddWithValue("tenant_id", tenantId);
            command.Parameters.AddWithValue("store_id", effectiveStoreId);
            command.Parameters.AddWithValue("subject", subject.Value);
            command.Parameters.AddWithValue("relation", relation);
            command.Parameters.AddWithValue("object_ref", obj.Value);

            var deleted = await command.ExecuteScalarAsync(cancellationToken);
            if (deleted is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await WriteChangeAsync(connection, transaction, tenantId, effectiveStoreId, subject.Value, relation, obj.Value, "delete", DateTimeOffset.UtcNow, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _authorizationCache?.InvalidateTenant(tenantId);
            return true;
        }

        public async Task<IReadOnlyList<RelationshipChange>> ReadChangesAsync(string tenantId, int offset, int limit, CancellationToken cancellationToken = default, string? storeId = null)
        {
            var effectiveStoreId = ResolveStoreId(tenantId, storeId);
            const string sql = @"SELECT subject, relation, object_ref, operation, created_at
                                 FROM relationship_changes
                                 WHERE tenant_id = @tenant_id
                                   AND store_id = @store_id
                                 ORDER BY created_at ASC
                                 OFFSET @offset LIMIT @limit;";

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("tenant_id", tenantId);
            command.Parameters.AddWithValue("store_id", effectiveStoreId);
            command.Parameters.AddWithValue("offset", offset);
            command.Parameters.AddWithValue("limit", limit);

            var results = new List<RelationshipChange>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new RelationshipChange(
                    tenantId,
                    new Subject(reader.GetString(0)),
                    reader.GetString(1),
                    new ObjectRef(reader.GetString(2)),
                    reader.GetString(3),
                    reader.GetFieldValue<DateTimeOffset>(4),
                    effectiveStoreId));
            }

            return results;
        }

        public async Task PurgeTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await using var command = new NpgsqlCommand("DELETE FROM relationships WHERE tenant_id = @tenant_id; DELETE FROM relationship_changes WHERE tenant_id = @tenant_id;", connection);
            command.Transaction = transaction;
            command.Parameters.AddWithValue("tenant_id", tenantId);
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _authorizationCache?.InvalidateTenant(tenantId);
        }

        Task<IReadOnlyList<DomainRelationship>> IRelationshipRepository.QueryAsync(string tenantId, string? subject, string? relation, string? obj, DomainRelationshipPermissionEffect? effect, CancellationToken cancellationToken)
        {
            return QueryDomainAsync(tenantId, subject, relation, obj, effect, cancellationToken);
        }

        Task IRelationshipRepository.UpsertAsync(DomainRelationship relationship, CancellationToken cancellationToken)
        {
            return UpsertAsync(relationship.TenantId, new RelationshipTuple(new Subject(relationship.Subject.Value), relationship.Relation.Value, new ObjectRef(relationship.Object.Value), relationship.Effect == DomainRelationshipPermissionEffect.Deny ? RelationshipEffect.Deny : RelationshipEffect.Allow, relationship.CreatedAt), cancellationToken);
        }

        Task<bool> IRelationshipRepository.DeleteAsync(string tenantId, string subject, string relation, string obj, CancellationToken cancellationToken)
        {
            return DeleteAsync(tenantId, new Subject(subject), relation, new ObjectRef(obj), cancellationToken);
        }

        Task<IReadOnlyList<DomainRelationshipChange>> IRelationshipRepository.ReadChangesAsync(string tenantId, int offset, int limit, CancellationToken cancellationToken)
        {
            return QueryChangesDomainAsync(tenantId, offset, limit, cancellationToken);
        }

        Task IRelationshipRepository.PurgeTenantAsync(string tenantId, CancellationToken cancellationToken)
        {
            return PurgeTenantAsync(tenantId, cancellationToken);
        }

        private async Task WriteChangeAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string tenantId, string storeId, string subject, string relation, string objectRef, string operation, DateTimeOffset createdAt, CancellationToken cancellationToken)
        {
            const string sql = @"INSERT INTO relationship_changes (id, tenant_id, store_id, subject, relation, object_ref, operation, created_at)
                                 VALUES (@id, @tenant_id, @store_id, @subject, @relation, @object_ref, @operation, @created_at);";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Transaction = transaction;
            command.Parameters.AddWithValue("id", Guid.NewGuid());
            command.Parameters.AddWithValue("tenant_id", tenantId);
            command.Parameters.AddWithValue("store_id", storeId);
            command.Parameters.AddWithValue("subject", subject);
            command.Parameters.AddWithValue("relation", relation);
            command.Parameters.AddWithValue("object_ref", objectRef);
            command.Parameters.AddWithValue("operation", operation);
            command.Parameters.AddWithValue("created_at", createdAt);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static string ResolveStoreId(string tenantId, string? storeId)
        {
            return string.IsNullOrWhiteSpace(storeId) ? tenantId : storeId;
        }

        private async Task<IReadOnlyList<DomainRelationship>> QueryDomainAsync(string tenantId, string? subject, string? relation, string? obj, DomainRelationshipPermissionEffect? effect, CancellationToken cancellationToken)
        {
            var tuples = await QueryAsync(
                tenantId,
                subject is null ? null : new Subject(subject),
                relation,
                obj is null ? null : new ObjectRef(obj),
                effect is null ? null : (effect == DomainRelationshipPermissionEffect.Deny ? RelationshipEffect.Deny : RelationshipEffect.Allow),
                cancellationToken);

            return tuples.Select(x => DomainRelationship.Rehydrate(Guid.NewGuid(), tenantId, x.Subject.Value, x.Relation, x.Object.Value, x.Effect == RelationshipEffect.Deny ? DomainRelationshipPermissionEffect.Deny : DomainRelationshipPermissionEffect.Allow, x.CreatedAt, x.CreatedAt)).ToList();
        }

        private async Task<IReadOnlyList<DomainRelationshipChange>> QueryChangesDomainAsync(string tenantId, int offset, int limit, CancellationToken cancellationToken)
        {
            var changes = await ReadChangesAsync(tenantId, offset, limit, cancellationToken);
            return changes.Select(x => DomainRelationshipChange.Rehydrate(Guid.NewGuid(), tenantId, x.Subject.Value, x.Relation, x.Object.Value, x.Operation, x.CreatedAt)).ToList();
        }
    }
}
