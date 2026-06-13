using Aegis.Authorization.Caching;
using Aegis.Application.Interfaces;
using Aegis.Authorization.RBAC;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Administration;
using Npgsql;

namespace Aegis.Infrastructure.Authorization
{
    public sealed class PostgresRbacStore : IRbacProvider, IRbacAdminStore
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;
        private readonly AuthorizationCache? _authorizationCache;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentBag<string>> _tenantGrantKeys = new();

        public PostgresRbacStore(NpgsqlDataSource dataSource, Microsoft.Extensions.Caching.Memory.IMemoryCache cache, AuthorizationCache? authorizationCache = null)
        {
            _dataSource = dataSource;
            _cache = cache;
            _authorizationCache = authorizationCache;
        }

        public async Task<bool> HasPermissionAsync(CheckRequest request, CancellationToken cancellationToken = default)
        {
            var evaluator = new RbacPermissionEvaluator((tenantId, subject, token) =>
                GetGrantsAsync(tenantId, request.EffectiveStoreId, subject, token));
            return await evaluator.HasPermissionAsync(request, cancellationToken);
        }

        private async Task<IReadOnlyList<RbacPermissionGrant>> GetGrantsAsync(
            string tenantId,
            string storeId,
            Subject subject,
            CancellationToken cancellationToken)
        {
            var cacheKey = $"rbac_grants:{tenantId}:{storeId}:{subject.Value}";
            if (_cache.TryGetValue(cacheKey, out var cachedObj) && cachedObj is IReadOnlyList<RbacPermissionGrant> cached)
            {
                return cached;
            }

            const string sql = @"SELECT ur.role_name, rp.relation, rp.object_ref, rp.condition_name
                                 FROM rbac_user_roles ur
                                 JOIN rbac_role_permissions rp ON rp.tenant_id = ur.tenant_id AND rp.store_id = ur.store_id AND rp.role_name = ur.role_name
                                 WHERE ur.tenant_id = @tenant_id AND ur.store_id = @store_id AND ur.user_id = @user_id;";

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("tenant_id", tenantId);
            command.Parameters.AddWithValue("store_id", storeId);
            command.Parameters.AddWithValue("user_id", subject.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var grants = new List<RbacPermissionGrant>();
            while (await reader.ReadAsync(cancellationToken))
            {
                grants.Add(new RbacPermissionGrant(
                    SubjectPattern: subject.Value,
                    RelationPattern: reader.GetString(1),
                    ObjectPattern: reader.GetString(2),
                    IsDeny: false,
                    ConditionName: reader.IsDBNull(3) ? null : reader.GetString(3)));
            }

            var options = new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60),
                Priority = Microsoft.Extensions.Caching.Memory.CacheItemPriority.Normal
            };

            using (var entry = _cache.CreateEntry(cacheKey))
            {
                entry.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
                entry.Priority = options.Priority;
                entry.Value = grants.AsReadOnly();
            }

            var bag = _tenantGrantKeys.GetOrAdd(tenantId, _ => new System.Collections.Concurrent.ConcurrentBag<string>());
            bag.Add(cacheKey);

            return grants;
        }

        private void EvictTenantGrantCache(string tenantId)
        {
            if (_tenantGrantKeys.TryRemove(tenantId, out var bag))
            {
                while (bag.TryTake(out var key))
                {
                    _cache.Remove(key);
                }
            }

            _authorizationCache?.InvalidateTenant(tenantId);
        }

        public async Task UpsertRoleAsync(string tenantId, string roleName, string? description, CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(async connection =>
            {
                const string sql = @"INSERT INTO rbac_roles (tenant_id, store_id, role_name, description, created_at, updated_at)
                                     VALUES (@tenant_id, @store_id, @role_name, @description, @created_at, @updated_at)
                                     ON CONFLICT (tenant_id, store_id, role_name) DO UPDATE SET description = EXCLUDED.description, updated_at = EXCLUDED.updated_at;";
                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("tenant_id", tenantId);
                command.Parameters.AddWithValue("store_id", tenantId);
                command.Parameters.AddWithValue("role_name", roleName);
                command.Parameters.AddWithValue("description", (object?)description ?? DBNull.Value);
                command.Parameters.AddWithValue("created_at", DateTimeOffset.UtcNow);
                command.Parameters.AddWithValue("updated_at", DateTimeOffset.UtcNow);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }, cancellationToken);

            EvictTenantGrantCache(tenantId);
        }

        public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT role_name, description FROM rbac_roles WHERE tenant_id = @tenant_id AND store_id = @store_id ORDER BY role_name;";
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("tenant_id", tenantId);
            command.Parameters.AddWithValue("store_id", tenantId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var roles = new List<RoleDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                roles.Add(new RoleDto(reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
            }

            return roles;
        }

        public Task UpsertPermissionAsync(string tenantId, string relation, string obj, CancellationToken cancellationToken = default)
        {
            return UpsertPermissionAsync(tenantId, relation, obj, null, cancellationToken);
        }

        public async Task UpsertPermissionAsync(string tenantId, string relation, string obj, string? conditionName = null, CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(async connection =>
            {
                const string sql = @"INSERT INTO rbac_permissions (tenant_id, store_id, relation, object_ref, condition_name, created_at)
                                     VALUES (@tenant_id, @store_id, @relation, @object_ref, @condition_name, @created_at)
                                     ON CONFLICT (tenant_id, store_id, relation, object_ref) DO UPDATE SET condition_name = EXCLUDED.condition_name;";
                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("tenant_id", tenantId);
                command.Parameters.AddWithValue("store_id", tenantId);
                command.Parameters.AddWithValue("relation", relation);
                command.Parameters.AddWithValue("object_ref", obj);
                command.Parameters.AddWithValue("condition_name", (object?)conditionName ?? DBNull.Value);
                command.Parameters.AddWithValue("created_at", DateTimeOffset.UtcNow);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }, cancellationToken);

            EvictTenantGrantCache(tenantId);
        }

        public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT relation, object_ref, condition_name FROM rbac_permissions WHERE tenant_id = @tenant_id AND store_id = @store_id ORDER BY relation, object_ref;";
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("tenant_id", tenantId);
            command.Parameters.AddWithValue("store_id", tenantId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var permissions = new List<PermissionDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                permissions.Add(new PermissionDto(reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2)));
            }

            return permissions;
        }

        public async Task<PermissionDto?> GetPermissionAsync(string tenantId, string relation, string obj, CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT relation, object_ref, condition_name FROM rbac_permissions WHERE tenant_id = @tenant_id AND store_id = @store_id AND relation = @relation AND object_ref = @object_ref LIMIT 1;";
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("tenant_id", tenantId);
            command.Parameters.AddWithValue("store_id", tenantId);
            command.Parameters.AddWithValue("relation", relation);
            command.Parameters.AddWithValue("object_ref", obj);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new PermissionDto(reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2));
        }

        public Task AssignPermissionToRoleAsync(string tenantId, string roleName, string relation, string obj, CancellationToken cancellationToken = default)
        {
            return AssignPermissionToRoleAsync(tenantId, roleName, relation, obj, null, cancellationToken);
        }

        public async Task AssignPermissionToRoleAsync(string tenantId, string roleName, string relation, string obj, string? conditionName = null, CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(async connection =>
            {
                const string sql = @"INSERT INTO rbac_role_permissions (tenant_id, store_id, role_name, relation, object_ref, condition_name, created_at)
                                     VALUES (@tenant_id, @store_id, @role_name, @relation, @object_ref, @condition_name, @created_at)
                                     ON CONFLICT (tenant_id, store_id, role_name, relation, object_ref) DO UPDATE SET condition_name = EXCLUDED.condition_name;";
                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("tenant_id", tenantId);
                command.Parameters.AddWithValue("store_id", tenantId);
                command.Parameters.AddWithValue("role_name", roleName);
                command.Parameters.AddWithValue("relation", relation);
                command.Parameters.AddWithValue("object_ref", obj);
                command.Parameters.AddWithValue("condition_name", (object?)conditionName ?? DBNull.Value);
                command.Parameters.AddWithValue("created_at", DateTimeOffset.UtcNow);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }, cancellationToken);

            EvictTenantGrantCache(tenantId);
        }

        public async Task AssignRoleToUserAsync(string tenantId, string userId, string roleName, CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(async connection =>
            {
                const string sql = @"INSERT INTO rbac_user_roles (tenant_id, store_id, user_id, role_name, created_at)
                                     VALUES (@tenant_id, @store_id, @user_id, @role_name, @created_at)
                                     ON CONFLICT (tenant_id, store_id, user_id, role_name) DO NOTHING;";
                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("tenant_id", tenantId);
                command.Parameters.AddWithValue("store_id", tenantId);
                command.Parameters.AddWithValue("user_id", userId);
                command.Parameters.AddWithValue("role_name", roleName);
                command.Parameters.AddWithValue("created_at", DateTimeOffset.UtcNow);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }, cancellationToken);

            EvictTenantGrantCache(tenantId);
        }

        public async Task<UserDto> CreateUserAsync(string tenantId, string userId, string? email, string? displayName, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = @"INSERT INTO rbac_users (tenant_id, user_id, email, display_name, created_at, updated_at)
                                 VALUES (@tenant_id, @user_id, @email, @display_name, @created_at, @updated_at)
                                 ON CONFLICT DO NOTHING
                                 RETURNING user_id, created_at, email, display_name;";
            await using var command = new NpgsqlCommand(sql, connection);
            var now = DateTimeOffset.UtcNow;
            command.Parameters.AddWithValue("tenant_id", tenantId);
            command.Parameters.AddWithValue("user_id", userId);
            command.Parameters.AddWithValue("email", (object?)email ?? DBNull.Value);
            command.Parameters.AddWithValue("display_name", (object?)displayName ?? DBNull.Value);
            command.Parameters.AddWithValue("created_at", now);
            command.Parameters.AddWithValue("updated_at", now);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException($"User with ID '{userId}' already exists.");
            }

            return new UserDto(reader.GetString(0), reader.GetFieldValue<DateTimeOffset>(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3));
        }

        public async Task<IReadOnlyList<UserDto>> GetUsersAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT user_id, created_at, email, display_name FROM rbac_users WHERE tenant_id = @tenant_id ORDER BY user_id;";
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("tenant_id", tenantId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var users = new List<UserDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                users.Add(new UserDto(reader.GetString(0), reader.GetFieldValue<DateTimeOffset>(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3)));
            }

            return users;
        }

        public async Task<UserDto?> GetUserAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT user_id, created_at, email, display_name FROM rbac_users WHERE tenant_id = @tenant_id AND user_id = @user_id;";
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("tenant_id", tenantId);
            command.Parameters.AddWithValue("user_id", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new UserDto(reader.GetString(0), reader.GetFieldValue<DateTimeOffset>(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3));
        }

        public async Task<bool> UpdateUserAsync(string tenantId, string userId, string? email, string? displayName, CancellationToken cancellationToken = default)
        {
            const string sql = @"UPDATE rbac_users
                                 SET email = COALESCE(@email, email), display_name = COALESCE(@display_name, display_name), updated_at = @updated_at
                                 WHERE tenant_id = @tenant_id AND user_id = @user_id;";
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("tenant_id", tenantId);
            command.Parameters.AddWithValue("user_id", userId);
            command.Parameters.AddWithValue("email", (object?)email ?? DBNull.Value);
            command.Parameters.AddWithValue("display_name", (object?)displayName ?? DBNull.Value);
            command.Parameters.AddWithValue("updated_at", DateTimeOffset.UtcNow);
            var updated = await command.ExecuteNonQueryAsync(cancellationToken) > 0;
            if (updated)
            {
                EvictTenantGrantCache(tenantId);
            }

            return updated;
        }

        public async Task<bool> DeleteUserAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand("DELETE FROM rbac_user_roles WHERE tenant_id = @tenant_id AND user_id = @user_id; DELETE FROM rbac_users WHERE tenant_id = @tenant_id AND user_id = @user_id;", connection);
            command.Parameters.AddWithValue("tenant_id", tenantId);
            command.Parameters.AddWithValue("user_id", userId);
            var deleted = await command.ExecuteNonQueryAsync(cancellationToken) > 0;
            if (deleted)
            {
                EvictTenantGrantCache(tenantId);
            }

            return deleted;
        }

        public async Task<UserRolesDto> GetUserRolesAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT role_name FROM rbac_user_roles WHERE tenant_id = @tenant_id AND store_id = @store_id AND user_id = @user_id ORDER BY role_name;";
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("tenant_id", tenantId);
            command.Parameters.AddWithValue("store_id", tenantId);
            command.Parameters.AddWithValue("user_id", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var roles = new List<string>();
            while (await reader.ReadAsync(cancellationToken))
            {
                roles.Add(reader.GetString(0));
            }

            return new UserRolesDto(userId, roles);
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
