using Aegis.Authorization.Caching;
using Aegis.Application.Interfaces;
using Aegis.Application.Contracts;
using Aegis.Contracts.Administration;
using Aegis.Domain.Entities;
using Aegis.Domain.Repositories;
using Npgsql;
using System.Text.Json;

namespace Aegis.Infrastructure.Persistence
{
    public sealed class PostgresStoreRegistry : IStoreRegistry, IAuthorizationModelRegistry, IStoreRepository, IAuthorizationModelRepository
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly AuthorizationCache? _authorizationCache;

        public PostgresStoreRegistry(NpgsqlDataSource dataSource, AuthorizationCache? authorizationCache = null)
        {
            _dataSource = dataSource;
            _authorizationCache = authorizationCache;
        }

        public async Task<StoreDto> CreateAsync(string name, CancellationToken cancellationToken = default)
        {
            return await CreateForTenantAsync(name, name, cancellationToken);
        }

        public async Task<StoreDto> CreateForTenantAsync(string tenantId, string name, CancellationToken cancellationToken = default)
        {
            var store = Store.Create(name);
            await ExecuteAsync(async connection =>
            {
                const string sql = @"INSERT INTO stores (id, tenant_id, name, created_at, updated_at)
                                     VALUES (@id, @tenant_id, @name, @created_at, @updated_at)
                                     ON CONFLICT (id) DO UPDATE SET tenant_id = EXCLUDED.tenant_id, name = EXCLUDED.name, updated_at = EXCLUDED.updated_at;";
                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("id", store.Id);
                command.Parameters.AddWithValue("tenant_id", tenantId);
                command.Parameters.AddWithValue("name", store.Name);
                command.Parameters.AddWithValue("created_at", store.CreatedAt);
                command.Parameters.AddWithValue("updated_at", store.UpdatedAt);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }, cancellationToken);
            return new StoreDto(store.Id, store.Name, store.CreatedAt, store.UpdatedAt, null, null, tenantId);
        }

        public async Task<IReadOnlyList<StoreDto>> ListAsync(CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = "SELECT id, name, created_at, updated_at, tenant_id FROM stores ORDER BY created_at DESC;";

            var items = new List<StoreDto>();
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new StoreDto(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetFieldValue<DateTimeOffset>(2),
                    reader.GetFieldValue<DateTimeOffset>(3),
                    null,
                    null,
                    reader.GetString(4)));
            }

            return items;
        }

        public async Task<IReadOnlyList<StoreDto>> ListForTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = "SELECT id, name, created_at, updated_at, tenant_id FROM stores WHERE tenant_id = @tenant_id ORDER BY created_at DESC;";

            var items = new List<StoreDto>();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("tenant_id", tenantId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new StoreDto(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetFieldValue<DateTimeOffset>(2),
                    reader.GetFieldValue<DateTimeOffset>(3),
                    null,
                    null,
                    reader.GetString(4)));
            }

            return items;
        }

        public async Task<StoreDto?> GetAsync(string storeId, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = "SELECT id, name, created_at, updated_at, tenant_id FROM stores WHERE id = @id;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", storeId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new StoreDto(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetFieldValue<DateTimeOffset>(2),
                reader.GetFieldValue<DateTimeOffset>(3),
                null,
                null,
                reader.GetString(4));
        }

        public async Task<StoreDto?> GetForTenantAsync(string tenantId, string storeId, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = "SELECT id, name, created_at, updated_at, tenant_id FROM stores WHERE tenant_id = @tenant_id AND id = @id;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("tenant_id", tenantId);
            command.Parameters.AddWithValue("id", storeId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new StoreDto(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetFieldValue<DateTimeOffset>(2),
                reader.GetFieldValue<DateTimeOffset>(3),
                null,
                null,
                reader.GetString(4));
        }

        public async Task<bool> DeleteAsync(string storeId, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = "DELETE FROM stores WHERE id = @id;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", storeId);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }

        public async Task<bool> DeleteForTenantAsync(string tenantId, string storeId, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = "DELETE FROM stores WHERE tenant_id = @tenant_id AND id = @id;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("tenant_id", tenantId);
            command.Parameters.AddWithValue("id", storeId);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }

        Task IStoreRepository.AddAsync(Store store, CancellationToken cancellationToken)
        {
            return ExecuteAsync(async connection =>
            {
                const string sql = @"INSERT INTO stores (id, tenant_id, name, created_at, updated_at)
                                     VALUES (@id, @tenant_id, @name, @created_at, @updated_at)
                                     ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, updated_at = EXCLUDED.updated_at;";
                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("id", store.Id);
                command.Parameters.AddWithValue("tenant_id", store.Id);
                command.Parameters.AddWithValue("name", store.Name);
                command.Parameters.AddWithValue("created_at", store.CreatedAt);
                command.Parameters.AddWithValue("updated_at", store.UpdatedAt);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }, cancellationToken);
        }

        async Task<IdempotentStoreAddResult> IStoreRepository.AddIdempotentAsync(
            Store store,
            IdempotentMutation mutation,
            CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var tx = await connection.BeginTransactionAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow;

            const string deleteExpiredSql = @"DELETE FROM store_creation_idempotency_records
                                              WHERE tenant_id = @tenant_id
                                                AND actor_id = @actor_id
                                                AND operation = @operation
                                                AND idempotency_key = @idempotency_key
                                                AND expires_at <= @now;";
            await using (var deleteExpired = new NpgsqlCommand(deleteExpiredSql, connection, tx))
            {
                AddStoreIdempotencyScopeParameters(deleteExpired, mutation);
                deleteExpired.Parameters.AddWithValue("now", now);
                await deleteExpired.ExecuteNonQueryAsync(cancellationToken);
            }

            const string reserveSql = @"INSERT INTO store_creation_idempotency_records
                                           (tenant_id, actor_id, operation, idempotency_key, request_fingerprint, response_json, created_at, expires_at)
                                       VALUES
                                           (@tenant_id, @actor_id, @operation, @idempotency_key, @request_fingerprint, NULL, @created_at, @expires_at)
                                       ON CONFLICT DO NOTHING
                                       RETURNING 1;";
            var reserved = false;
            await using (var reserve = new NpgsqlCommand(reserveSql, connection, tx))
            {
                AddStoreIdempotencyScopeParameters(reserve, mutation);
                reserve.Parameters.AddWithValue("request_fingerprint", mutation.RequestFingerprint);
                reserve.Parameters.AddWithValue("created_at", now);
                reserve.Parameters.AddWithValue("expires_at", mutation.ExpiresAt);
                reserved = await reserve.ExecuteScalarAsync(cancellationToken) is not null;
            }

            if (reserved)
            {
                const string insertStoreSql = @"INSERT INTO stores (id, tenant_id, name, created_at, updated_at)
                                                VALUES (@id, @tenant_id, @name, @created_at, @updated_at);";
                await using (var insertStore = new NpgsqlCommand(insertStoreSql, connection, tx))
                {
                    insertStore.Parameters.AddWithValue("id", store.Id);
                    insertStore.Parameters.AddWithValue("tenant_id", mutation.TenantId);
                    insertStore.Parameters.AddWithValue("name", store.Name);
                    insertStore.Parameters.AddWithValue("created_at", store.CreatedAt);
                    insertStore.Parameters.AddWithValue("updated_at", store.UpdatedAt);
                    await insertStore.ExecuteNonQueryAsync(cancellationToken);
                }

                var response = new StoreDto(store.Id, store.Name, store.CreatedAt, store.UpdatedAt, null, null, mutation.TenantId);
                const string completeSql = @"UPDATE store_creation_idempotency_records
                                             SET response_json = @response_json
                                             WHERE tenant_id = @tenant_id
                                               AND actor_id = @actor_id
                                               AND operation = @operation
                                               AND idempotency_key = @idempotency_key;";
                await using (var complete = new NpgsqlCommand(completeSql, connection, tx))
                {
                    AddStoreIdempotencyScopeParameters(complete, mutation);
                    complete.Parameters.AddWithValue("response_json", NpgsqlTypes.NpgsqlDbType.Jsonb, JsonSerializer.Serialize(response));
                    await complete.ExecuteNonQueryAsync(cancellationToken);
                }

                await tx.CommitAsync(cancellationToken);
                return new IdempotentStoreAddResult(store, true);
            }

            const string replaySql = @"SELECT request_fingerprint, response_json
                                       FROM store_creation_idempotency_records
                                       WHERE tenant_id = @tenant_id
                                         AND actor_id = @actor_id
                                         AND operation = @operation
                                         AND idempotency_key = @idempotency_key
                                       FOR UPDATE;";
            await using var replay = new NpgsqlCommand(replaySql, connection, tx);
            AddStoreIdempotencyScopeParameters(replay, mutation);
            await using var reader = await replay.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Store idempotency reservation disappeared before replay.");
            }

            if (!string.Equals(reader.GetString(0).TrimEnd(), mutation.RequestFingerprint, StringComparison.Ordinal))
            {
                throw new IdempotencyConflictException("Idempotency-Key was already used with a different request payload.");
            }

            if (reader.IsDBNull(1))
            {
                throw new InvalidOperationException("Store idempotency response is incomplete.");
            }

            var replayDto = JsonSerializer.Deserialize<StoreDto>(reader.GetString(1))
                ?? throw new InvalidOperationException("Stored store idempotency response is invalid.");
            await reader.DisposeAsync();
            await tx.CommitAsync(cancellationToken);
            return new IdempotentStoreAddResult(
                Store.Rehydrate(replayDto.Id, replayDto.Name, replayDto.CreatedAt, replayDto.UpdatedAt),
                false);
        }

        async Task<Store?> IStoreRepository.GetByIdAsync(string storeId, CancellationToken cancellationToken)
        {
            var dto = await GetAsync(storeId, cancellationToken);
            return dto is null ? null : Store.Rehydrate(dto.Id, dto.Name, dto.CreatedAt, dto.UpdatedAt);
        }

        async Task<IReadOnlyList<Store>> IStoreRepository.ListAsync(CancellationToken cancellationToken)
        {
            var items = await ListAsync(cancellationToken);
            return items.Select(x => Store.Rehydrate(x.Id, x.Name, x.CreatedAt, x.UpdatedAt)).ToList();
        }

        Task<bool> IStoreRepository.DeleteAsync(Store store, CancellationToken cancellationToken)
        {
            return DeleteAsync(store.Id, cancellationToken);
        }

        public async Task<AuthorizationModelDto> CreateAsync(string storeId, string schemaVersion, string model, CancellationToken cancellationToken = default)
        {
            var authorizationModel = AuthorizationModel.Create(storeId, schemaVersion, model);
            await ((IAuthorizationModelRepository)this).AddAsync(authorizationModel, cancellationToken);
            return new AuthorizationModelDto(authorizationModel.Id, authorizationModel.StoreId, authorizationModel.SchemaVersion, authorizationModel.Model, authorizationModel.CreatedAt);
        }

        public async Task<IReadOnlyList<AuthorizationModelDto>> ListAsync(string storeId, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = @"SELECT id, store_id, schema_version, model, created_at, state, published_at, archived_at, superseded_by, revision
                                 FROM authorization_models
                                 WHERE store_id = @store_id
                                 ORDER BY (state = 'Published') DESC, COALESCE(published_at, created_at) DESC, created_at DESC;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("store_id", storeId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<AuthorizationModelDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(ReadAuthorizationModel(reader));
            }

            return items;
        }

        public async Task<AuthorizationModelDto?> GetLatestAsync(string storeId, CancellationToken cancellationToken = default)
        {
            var items = await ListAsync(storeId, cancellationToken);
            return items.FirstOrDefault();
        }

        public async Task<AuthorizationModelDto?> GetPublishedAsync(string storeId, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = @"SELECT id, store_id, schema_version, model, created_at, state, published_at, archived_at, superseded_by, revision
                                 FROM authorization_models
                                 WHERE store_id = @store_id AND state = 'Published'
                                 ORDER BY COALESCE(published_at, created_at) DESC
                                 LIMIT 1;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("store_id", storeId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? ReadAuthorizationModel(reader) : null;
        }

        public async Task<AuthorizationModelDto?> GetByIdAsync(string storeId, string authorizationModelId, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = @"SELECT id, store_id, schema_version, model, created_at, state, published_at, archived_at, superseded_by, revision
                                 FROM authorization_models
                                 WHERE store_id = @store_id AND id = @id;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("store_id", storeId);
            command.Parameters.AddWithValue("id", authorizationModelId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return ReadAuthorizationModel(reader);
        }

        public async Task<AuthorizationModelDto?> UpdateAsync(string storeId, string authorizationModelId, string schemaVersion, string model, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = @"UPDATE authorization_models
                                 SET schema_version = @schema_version,
                                     model = @model,
                                     state = CASE WHEN state = 'Published' THEN 'Deprecated' ELSE 'Draft' END,
                                     archived_at = NULL,
                                     superseded_by = NULL,
                                     revision = revision + 1
                                 WHERE store_id = @store_id AND id = @id
                                 RETURNING id, store_id, schema_version, model, created_at, state, published_at, archived_at, superseded_by, revision;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("store_id", storeId);
            command.Parameters.AddWithValue("id", authorizationModelId);
            command.Parameters.AddWithValue("schema_version", schemaVersion);
            command.Parameters.AddWithValue("model", model);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return ReadAuthorizationModel(reader);
        }

        public async Task<AuthorizationModelDto?> UpdateStateAsync(
            string storeId,
            string authorizationModelId,
            string state,
            DateTimeOffset? publishedAt,
            DateTimeOffset? archivedAt,
            string? supersededBy,
            CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = @"UPDATE authorization_models
                                 SET state = @state,
                                     published_at = @published_at,
                                     archived_at = @archived_at,
                                     superseded_by = @superseded_by,
                                     revision = revision + 1
                                 WHERE store_id = @store_id AND id = @id
                                 RETURNING id, store_id, schema_version, model, created_at, state, published_at, archived_at, superseded_by, revision;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("store_id", storeId);
            command.Parameters.AddWithValue("id", authorizationModelId);
            command.Parameters.AddWithValue("state", state);
            command.Parameters.AddWithValue("published_at", (object?)publishedAt ?? DBNull.Value);
            command.Parameters.AddWithValue("archived_at", (object?)archivedAt ?? DBNull.Value);
            command.Parameters.AddWithValue("superseded_by", (object?)supersededBy ?? DBNull.Value);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? ReadAuthorizationModel(reader) : null;
        }

        public async Task<bool> DeleteAsync(string storeId, string authorizationModelId, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = "DELETE FROM authorization_models WHERE store_id = @store_id AND id = @id;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("store_id", storeId);
            command.Parameters.AddWithValue("id", authorizationModelId);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }

        Task IAuthorizationModelRepository.AddAsync(AuthorizationModel authorizationModel, CancellationToken cancellationToken)
        {
            return ExecuteAsync(async connection =>
            {
                const string sql = @"INSERT INTO authorization_models (id, store_id, schema_version, model, created_at, state, published_at, archived_at, superseded_by, revision)
                                     VALUES (@id, @store_id, @schema_version, @model, @created_at, @state, @published_at, @archived_at, @superseded_by, @revision)
                                     ON CONFLICT (id) DO UPDATE SET
                                        schema_version = EXCLUDED.schema_version,
                                        model = EXCLUDED.model,
                                        state = EXCLUDED.state,
                                        published_at = EXCLUDED.published_at,
                                        archived_at = EXCLUDED.archived_at,
                                        superseded_by = EXCLUDED.superseded_by,
                                        revision = authorization_models.revision + 1;";
                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("id", authorizationModel.Id);
                command.Parameters.AddWithValue("store_id", authorizationModel.StoreId);
                command.Parameters.AddWithValue("schema_version", authorizationModel.SchemaVersion);
                command.Parameters.AddWithValue("model", authorizationModel.Model);
                command.Parameters.AddWithValue("created_at", authorizationModel.CreatedAt);
                command.Parameters.AddWithValue("state", authorizationModel.State);
                command.Parameters.AddWithValue("published_at", (object?)authorizationModel.PublishedAt ?? DBNull.Value);
                command.Parameters.AddWithValue("archived_at", (object?)authorizationModel.ArchivedAt ?? DBNull.Value);
                command.Parameters.AddWithValue("superseded_by", (object?)authorizationModel.SupersededBy ?? DBNull.Value);
                command.Parameters.AddWithValue("revision", authorizationModel.Revision);
                await command.ExecuteNonQueryAsync(cancellationToken);
                _authorizationCache?.InvalidateTenant(authorizationModel.StoreId);
            }, cancellationToken);
        }

        async Task<IdempotentAuthorizationModelAddResult> IAuthorizationModelRepository.AddIdempotentAsync(
            AuthorizationModel authorizationModel,
            IdempotentMutation mutation,
            CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var tx = await connection.BeginTransactionAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow;

            const string deleteExpiredSql = @"DELETE FROM idempotency_records
                                              WHERE tenant_id = @tenant_id
                                                AND actor_id = @actor_id
                                                AND store_id = @store_id
                                                AND operation = @operation
                                                AND idempotency_key = @idempotency_key
                                                AND expires_at <= @now;";
            await using (var deleteExpired = new NpgsqlCommand(deleteExpiredSql, connection, tx))
            {
                AddIdempotencyScopeParameters(deleteExpired, authorizationModel.StoreId, mutation);
                deleteExpired.Parameters.AddWithValue("now", now);
                await deleteExpired.ExecuteNonQueryAsync(cancellationToken);
            }

            const string reserveSql = @"INSERT INTO idempotency_records
                                           (tenant_id, actor_id, store_id, operation, idempotency_key, request_fingerprint, response_json, created_at, expires_at)
                                       VALUES
                                           (@tenant_id, @actor_id, @store_id, @operation, @idempotency_key, @request_fingerprint, NULL, @created_at, @expires_at)
                                       ON CONFLICT DO NOTHING
                                       RETURNING 1;";
            var reserved = false;
            await using (var reserve = new NpgsqlCommand(reserveSql, connection, tx))
            {
                AddIdempotencyScopeParameters(reserve, authorizationModel.StoreId, mutation);
                reserve.Parameters.AddWithValue("request_fingerprint", mutation.RequestFingerprint);
                reserve.Parameters.AddWithValue("created_at", now);
                reserve.Parameters.AddWithValue("expires_at", mutation.ExpiresAt);
                reserved = await reserve.ExecuteScalarAsync(cancellationToken) is not null;
            }

            if (reserved)
            {
                const string insertModelSql = @"INSERT INTO authorization_models
                                                   (id, store_id, schema_version, model, created_at, state, published_at, archived_at, superseded_by, revision)
                                               VALUES
                                                   (@id, @store_id, @schema_version, @model, @created_at, @state, @published_at, @archived_at, @superseded_by, @revision);";
                await using (var insertModel = new NpgsqlCommand(insertModelSql, connection, tx))
                {
                    insertModel.Parameters.AddWithValue("id", authorizationModel.Id);
                    insertModel.Parameters.AddWithValue("store_id", authorizationModel.StoreId);
                    insertModel.Parameters.AddWithValue("schema_version", authorizationModel.SchemaVersion);
                    insertModel.Parameters.AddWithValue("model", authorizationModel.Model);
                    insertModel.Parameters.AddWithValue("created_at", authorizationModel.CreatedAt);
                    insertModel.Parameters.AddWithValue("state", authorizationModel.State);
                    insertModel.Parameters.AddWithValue("published_at", (object?)authorizationModel.PublishedAt ?? DBNull.Value);
                    insertModel.Parameters.AddWithValue("archived_at", (object?)authorizationModel.ArchivedAt ?? DBNull.Value);
                    insertModel.Parameters.AddWithValue("superseded_by", (object?)authorizationModel.SupersededBy ?? DBNull.Value);
                    insertModel.Parameters.AddWithValue("revision", authorizationModel.Revision);
                    await insertModel.ExecuteNonQueryAsync(cancellationToken);
                }

                var responseJson = JsonSerializer.Serialize(ToDto(authorizationModel));
                const string completeSql = @"UPDATE idempotency_records
                                             SET response_json = @response_json
                                             WHERE tenant_id = @tenant_id
                                               AND actor_id = @actor_id
                                               AND store_id = @store_id
                                               AND operation = @operation
                                               AND idempotency_key = @idempotency_key;";
                await using (var complete = new NpgsqlCommand(completeSql, connection, tx))
                {
                    AddIdempotencyScopeParameters(complete, authorizationModel.StoreId, mutation);
                    complete.Parameters.AddWithValue("response_json", NpgsqlTypes.NpgsqlDbType.Jsonb, responseJson);
                    await complete.ExecuteNonQueryAsync(cancellationToken);
                }

                await tx.CommitAsync(cancellationToken);
                _authorizationCache?.InvalidateTenant(authorizationModel.StoreId);
                return new IdempotentAuthorizationModelAddResult(authorizationModel, true);
            }

            const string replaySql = @"SELECT request_fingerprint, response_json
                                       FROM idempotency_records
                                       WHERE tenant_id = @tenant_id
                                         AND actor_id = @actor_id
                                         AND store_id = @store_id
                                         AND operation = @operation
                                         AND idempotency_key = @idempotency_key
                                       FOR UPDATE;";
            await using var replay = new NpgsqlCommand(replaySql, connection, tx);
            AddIdempotencyScopeParameters(replay, authorizationModel.StoreId, mutation);
            await using var reader = await replay.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Idempotency reservation disappeared before replay.");
            }

            var storedFingerprint = reader.GetString(0).TrimEnd();
            if (!string.Equals(storedFingerprint, mutation.RequestFingerprint, StringComparison.Ordinal))
            {
                throw new IdempotencyConflictException("Idempotency-Key was already used with a different request payload.");
            }

            if (reader.IsDBNull(1))
            {
                throw new InvalidOperationException("Idempotency response is incomplete.");
            }

            var replayDto = JsonSerializer.Deserialize<AuthorizationModelDto>(reader.GetString(1))
                ?? throw new InvalidOperationException("Stored idempotency response is invalid.");
            await reader.DisposeAsync();
            await tx.CommitAsync(cancellationToken);
            return new IdempotentAuthorizationModelAddResult(ToAggregate(replayDto), false);
        }

        async Task<IReadOnlyList<AuthorizationModel>> IAuthorizationModelRepository.ListByStoreAsync(string storeId, CancellationToken cancellationToken)
        {
            var items = await ListAsync(storeId, cancellationToken);
            return items.Select(ToAggregate).ToList();
        }

        async Task<AuthorizationModel?> IAuthorizationModelRepository.GetLatestByStoreAsync(string storeId, CancellationToken cancellationToken)
        {
            var dto = await GetLatestAsync(storeId, cancellationToken);
            return dto is null ? null : ToAggregate(dto);
        }

        async Task<AuthorizationModel?> IAuthorizationModelRepository.GetPublishedByStoreAsync(string storeId, CancellationToken cancellationToken)
        {
            var dto = await GetPublishedAsync(storeId, cancellationToken);
            return dto is null ? null : ToAggregate(dto);
        }

        async Task<AuthorizationModel?> IAuthorizationModelRepository.GetByIdAsync(string storeId, string authorizationModelId, CancellationToken cancellationToken)
        {
            var dto = await GetByIdAsync(storeId, authorizationModelId, cancellationToken);
            return dto is null ? null : ToAggregate(dto);
        }

        async Task<AuthorizationModel?> IAuthorizationModelRepository.UpdateAsync(AuthorizationModel authorizationModel, long expectedRevision, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = @"UPDATE authorization_models
                                 SET schema_version = @schema_version,
                                     model = @model,
                                     state = @state,
                                     published_at = @published_at,
                                     archived_at = @archived_at,
                                     superseded_by = @superseded_by,
                                     revision = revision + 1
                                 WHERE store_id = @store_id AND id = @id AND revision = @expected_revision
                                 RETURNING id, store_id, schema_version, model, created_at, state, published_at, archived_at, superseded_by, revision;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("store_id", authorizationModel.StoreId);
            command.Parameters.AddWithValue("id", authorizationModel.Id);
            command.Parameters.AddWithValue("schema_version", authorizationModel.SchemaVersion);
            command.Parameters.AddWithValue("model", authorizationModel.Model);
            command.Parameters.AddWithValue("state", authorizationModel.State);
            command.Parameters.AddWithValue("published_at", (object?)authorizationModel.PublishedAt ?? DBNull.Value);
            command.Parameters.AddWithValue("archived_at", (object?)authorizationModel.ArchivedAt ?? DBNull.Value);
            command.Parameters.AddWithValue("superseded_by", (object?)authorizationModel.SupersededBy ?? DBNull.Value);
            command.Parameters.AddWithValue("expected_revision", expectedRevision);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var dto = await reader.ReadAsync(cancellationToken) ? ReadAuthorizationModel(reader) : null;
            if (dto is not null)
            {
                _authorizationCache?.InvalidateTenant(authorizationModel.StoreId);
            }

            return dto is null ? null : ToAggregate(dto);
        }

        async Task<IReadOnlyList<AuthorizationModel>> IAuthorizationModelRepository.PublishAsync(
            string storeId,
            string authorizationModelId,
            long expectedRevision,
            CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var tx = await connection.BeginTransactionAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow;

            const string lockStoreSql = "SELECT 1 FROM stores WHERE id = @store_id FOR UPDATE;";
            await using (var lockCommand = new NpgsqlCommand(lockStoreSql, connection, tx))
            {
                lockCommand.Parameters.AddWithValue("store_id", storeId);
                await lockCommand.ExecuteScalarAsync(cancellationToken);
            }

            const string updateSql = @"WITH target AS (
                                           SELECT id
                                           FROM authorization_models
                                           WHERE store_id = @store_id AND id = @id AND revision = @expected_revision
                                       )
                                       UPDATE authorization_models
                                       SET state = CASE WHEN id = @id THEN 'Published' WHEN state = 'Published' THEN 'Archived' ELSE state END,
                                           published_at = CASE WHEN id = @id THEN @now ELSE published_at END,
                                           archived_at = CASE WHEN id <> @id AND state = 'Published' THEN @now ELSE archived_at END,
                                           superseded_by = CASE WHEN id <> @id AND state = 'Published' THEN @id ELSE NULL END,
                                           revision = revision + 1
                                       WHERE store_id = @store_id
                                         AND (id = @id OR state = 'Published')
                                         AND EXISTS (SELECT 1 FROM target)
                                       RETURNING id, store_id, schema_version, model, created_at, state, published_at, archived_at, superseded_by, revision;";
            await using var command = new NpgsqlCommand(updateSql, connection, tx);
            command.Parameters.AddWithValue("store_id", storeId);
            command.Parameters.AddWithValue("id", authorizationModelId);
            command.Parameters.AddWithValue("expected_revision", expectedRevision);
            command.Parameters.AddWithValue("now", now);
            var updated = new List<AuthorizationModel>();
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    updated.Add(ToAggregate(ReadAuthorizationModel(reader)));
                }
            }

            await tx.CommitAsync(cancellationToken);
            _authorizationCache?.InvalidateTenant(storeId);
            return updated;
        }

        async Task<AuthorizationModel?> IAuthorizationModelRepository.RollbackAsync(
            string storeId,
            string authorizationModelId,
            long expectedRevision,
            CancellationToken cancellationToken)
        {
            var updated = await ((IAuthorizationModelRepository)this).PublishAsync(storeId, authorizationModelId, expectedRevision, cancellationToken);
            return updated.FirstOrDefault(x => x.Id.Equals(authorizationModelId, StringComparison.OrdinalIgnoreCase));
        }

        Task<bool> IAuthorizationModelRepository.DeleteAsync(AuthorizationModel authorizationModel, long expectedRevision, CancellationToken cancellationToken)
        {
            return DeleteAndInvalidateAsync(authorizationModel, expectedRevision, cancellationToken);
        }

        private async Task<bool> DeleteAndInvalidateAsync(AuthorizationModel authorizationModel, long expectedRevision, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = "DELETE FROM authorization_models WHERE store_id = @store_id AND id = @id AND revision = @expected_revision;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("store_id", authorizationModel.StoreId);
            command.Parameters.AddWithValue("id", authorizationModel.Id);
            command.Parameters.AddWithValue("expected_revision", expectedRevision);
            var deleted = await command.ExecuteNonQueryAsync(cancellationToken) > 0;
            if (deleted)
            {
                _authorizationCache?.InvalidateTenant(authorizationModel.StoreId);
            }

            return deleted;
        }

        private static AuthorizationModelDto ReadAuthorizationModel(NpgsqlDataReader reader)
        {
            return new AuthorizationModelDto(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetFieldValue<DateTimeOffset>(4),
                reader.IsDBNull(5) ? AuthorizationModelLifecycleStates.Draft : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
                reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.GetInt64(9));
        }

        private static AuthorizationModel ToAggregate(AuthorizationModelDto dto)
        {
            return AuthorizationModel.Rehydrate(
                dto.Id,
                dto.StoreId,
                dto.SchemaVersion,
                dto.Model,
                dto.CreatedAt,
                dto.State,
                dto.PublishedAt,
                dto.ArchivedAt,
                dto.SupersededBy,
                dto.Revision);
        }

        private static AuthorizationModelDto ToDto(AuthorizationModel model)
        {
            return new AuthorizationModelDto(
                model.Id,
                model.StoreId,
                model.SchemaVersion,
                model.Model,
                model.CreatedAt,
                model.State,
                model.PublishedAt,
                model.ArchivedAt,
                model.SupersededBy,
                model.Revision);
        }

        private static void AddIdempotencyScopeParameters(
            NpgsqlCommand command,
            string storeId,
            IdempotentMutation mutation)
        {
            command.Parameters.AddWithValue("tenant_id", mutation.TenantId);
            command.Parameters.AddWithValue("actor_id", mutation.ActorId);
            command.Parameters.AddWithValue("store_id", storeId);
            command.Parameters.AddWithValue("operation", mutation.Operation);
            command.Parameters.AddWithValue("idempotency_key", mutation.Key);
        }

        private static void AddStoreIdempotencyScopeParameters(
            NpgsqlCommand command,
            IdempotentMutation mutation)
        {
            command.Parameters.AddWithValue("tenant_id", mutation.TenantId);
            command.Parameters.AddWithValue("actor_id", mutation.ActorId);
            command.Parameters.AddWithValue("operation", mutation.Operation);
            command.Parameters.AddWithValue("idempotency_key", mutation.Key);
        }

        private Task ExecuteAsync(Func<NpgsqlConnection, Task> action, CancellationToken cancellationToken)
        {
            return ExecuteAsyncInternal(action, cancellationToken);
        }

        private async Task ExecuteAsyncInternal(Func<NpgsqlConnection, Task> action, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await action(connection);
        }
    }
}
