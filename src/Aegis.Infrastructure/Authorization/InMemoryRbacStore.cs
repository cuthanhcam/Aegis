using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Administration;
using Aegis.Domain.ValueObjects;
using System.Collections.Concurrent;

namespace Aegis.Infrastructure.Authorization
{
    public sealed class InMemoryRbacStore : IRbacProvider, IRbacAdminStore
    {
        private readonly ConcurrentDictionary<string, string?> _roles = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, byte> _permissions = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, byte> _rolePermissions = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, byte> _userRoles = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, InMemoryUser> _users = new(StringComparer.OrdinalIgnoreCase);

        public Task<bool> HasPermissionAsync(
            CheckRequest request,
            CancellationToken cancellationToken = default)
        {
            var userKeyPrefix = $"{request.TenantId}|{request.Subject.Value}|";

            foreach (var userRole in _userRoles.Keys.Where(k => k.StartsWith(userKeyPrefix, StringComparison.OrdinalIgnoreCase)))
            {
                var roleName = userRole.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[2];
                var rolePermissionPrefix = $"{request.TenantId}|{roleName}|";
                foreach (var rolePermission in _rolePermissions.Keys.Where(k => k.StartsWith(rolePermissionPrefix, StringComparison.OrdinalIgnoreCase)))
                {
                    var parts = rolePermission.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (parts.Length < 4)
                    {
                        continue;
                    }

                    var relationPattern = parts[2];
                    var objectPattern = parts[3];
                    if (MatchesRelationPattern(relationPattern, request.Relation)
                        && MatchesObjectPattern(objectPattern, request.Object.Value))
                    {
                        return Task.FromResult(true);
                    }
                }
            }

            return Task.FromResult(false);
        }

        public Task UpsertRoleAsync(
            string tenantId,
            string roleName,
            string? description,
            CancellationToken cancellationToken = default)
        {
            _roles[RoleKey(tenantId, roleName)] = description;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RoleDto>> GetRolesAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            var prefix = $"{tenantId}|";
            var data = _roles
                .Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(x => new RoleDto(x.Key.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[1], x.Value))
                .OrderBy(x => x.Name)
                .ToList();

            return Task.FromResult<IReadOnlyList<RoleDto>>(data);
        }

        public Task UpsertPermissionAsync(
            string tenantId,
            string relation,
            string obj,
            CancellationToken cancellationToken = default)
        {
            _permissions[PermissionKey(tenantId, relation, obj)] = 1;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            var prefix = $"{tenantId}|";
            var data = _permissions.Keys
                .Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(x =>
                {
                    var parts = x.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    return new PermissionDto(parts[1], parts[2]);
                })
                .OrderBy(x => x.Relation)
                .ThenBy(x => x.Object)
                .ToList();

            return Task.FromResult<IReadOnlyList<PermissionDto>>(data);
        }

        public Task AssignPermissionToRoleAsync(
            string tenantId,
            string roleName,
            string relation,
            string obj,
            CancellationToken cancellationToken = default)
        {
            _rolePermissions[RolePermissionKey(tenantId, roleName, relation, obj)] = 1;
            return Task.CompletedTask;
        }

        public Task AssignRoleToUserAsync(
            string tenantId,
            string userId,
            string roleName,
            CancellationToken cancellationToken = default)
        {
            _userRoles[UserRoleKey(tenantId, userId, roleName)] = 1;
            return Task.CompletedTask;
        }

        public Task<UserDto> CreateUserAsync(
            string tenantId,
            string userId,
            string? email,
            string? displayName,
            CancellationToken cancellationToken = default)
        {
            var key = UserKey(tenantId, userId);
            var user = new InMemoryUser(userId, email, displayName, DateTimeOffset.UtcNow);
            if (!_users.TryAdd(key, user))
            {
                throw new InvalidOperationException($"User with ID '{userId}' already exists.");
            }

            return Task.FromResult(new UserDto(user.UserId, user.CreatedAt, user.Email, user.DisplayName));
        }

        public Task<IReadOnlyList<UserDto>> GetUsersAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            var prefix = $"{tenantId}|";
            var users = _users
                .Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(x => new UserDto(x.Value.UserId, x.Value.CreatedAt, x.Value.Email, x.Value.DisplayName))
                .OrderBy(x => x.UserId)
                .ToList();

            return Task.FromResult<IReadOnlyList<UserDto>>(users);
        }

        public Task<UserDto?> GetUserAsync(
            string tenantId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var key = UserKey(tenantId, userId);
            if (!_users.TryGetValue(key, out var user))
            {
                return Task.FromResult<UserDto?>(null);
            }

            return Task.FromResult<UserDto?>(new UserDto(user.UserId, user.CreatedAt, user.Email, user.DisplayName));
        }

        public Task<bool> UpdateUserAsync(
            string tenantId,
            string userId,
            string? email,
            string? displayName,
            CancellationToken cancellationToken = default)
        {
            var key = UserKey(tenantId, userId);
            if (!_users.TryGetValue(key, out var existing))
            {
                return Task.FromResult(false);
            }

            _users[key] = existing with { Email = email ?? existing.Email, DisplayName = displayName ?? existing.DisplayName };
            return Task.FromResult(true);
        }

        public Task<bool> DeleteUserAsync(
            string tenantId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var removed = _users.TryRemove(UserKey(tenantId, userId), out _);
            if (!removed)
            {
                return Task.FromResult(false);
            }

            var prefix = $"{tenantId}|{userId}|";
            foreach (var key in _userRoles.Keys.Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
            {
                _userRoles.TryRemove(key, out _);
            }

            return Task.FromResult(true);
        }

        public Task<UserRolesDto> GetUserRolesAsync(
            string tenantId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var prefix = $"{tenantId}|{userId}|";
            var roles = _userRoles.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(k => k.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[2])
                .OrderBy(x => x)
                .ToList();

            return Task.FromResult(new UserRolesDto(userId, roles));
        }

        private static string RoleKey(
            string tenantId,
            string roleName)
        {
            return $"{tenantId}|{roleName}";
        }
        private static string PermissionKey(
            string tenantId,
            string relation,
            string obj)
        {
            return $"{tenantId}|{relation}|{obj}";
        }

        private static string RolePermissionKey(
            string tenantId,
            string roleName,
            string relation,
            string obj)
        {
            return $"{tenantId}|{roleName}|{relation}|{obj}";
        }

        private static string UserRoleKey(
            string tenantId,
            string userId,
            string roleName)
        {
            return $"{tenantId}|{userId}|{roleName}";
        }

        private static string UserKey(
            string tenantId,
            string userId)
        {
            return $"{tenantId}|{userId}";
        }

        private static bool MatchesRelationPattern(string relationPattern, string relation)
        {
            if (string.IsNullOrWhiteSpace(relationPattern) || relationPattern == "*")
            {
                return true;
            }

            return relationPattern.Equals(relation, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesObjectPattern(string objectPattern, string objectRef)
        {
            if (string.IsNullOrWhiteSpace(objectPattern) || objectPattern == "*")
            {
                return true;
            }

            if (objectPattern.Equals(objectRef, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var objectType = GetTypeName(objectRef);
            return objectPattern.Equals($"{objectType}:*", StringComparison.OrdinalIgnoreCase)
                || objectPattern.Equals(objectType, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetTypeName(string value)
        {
            var split = value.IndexOf(':');
            return split > 0 ? value[..split] : value;
        }

        private sealed record InMemoryUser(
            string UserId,
            string? Email,
            string? DisplayName,
            DateTimeOffset CreatedAt);
    }
}
