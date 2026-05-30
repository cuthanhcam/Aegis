using Aegis.Application.Interfaces;
using Aegis.Contracts.Authentication;
using Aegis.SharedKernel.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Aegis.Infrastructure.Identity
{
    public sealed class JwtAuthSessionService : IAuthSessionService
    {
        private readonly AuthOptions _authOptions;
        private readonly JwtOptions _jwtOptions;
        private readonly JwtSecurityTokenHandler _tokenHandler = new();
        private readonly ConcurrentDictionary<string, RefreshSession> _refreshStore = new(StringComparer.Ordinal);

        public JwtAuthSessionService(IConfiguration configuration, IOptions<AuthOptions> authOptions, IOptions<JwtOptions> jwtOptions)
        {
            _authOptions = authOptions.Value;
            _jwtOptions = jwtOptions.Value;
        }

        public Task<LoginResponseDto?> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            var users = LoadDemoUsers();
            var user = users.FirstOrDefault(x => string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase) && x.Password == password);
            if (user is null)
            {
                return Task.FromResult<LoginResponseDto?>(null);
            }

            return Task.FromResult<LoginResponseDto?>(IssueTokens(user.Subject, user.Username, user.TenantId, user.Roles));
        }

        public Task<LoginResponseDto?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (!_refreshStore.TryGetValue(refreshToken, out var session) || session.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return Task.FromResult<LoginResponseDto?>(null);
            }

            _refreshStore.TryRemove(refreshToken, out _);
            return Task.FromResult<LoginResponseDto?>(IssueTokens(session.Subject, session.Username, session.TenantId, session.Roles));
        }

        public Task<bool> RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_refreshStore.TryRemove(refreshToken, out _));
        }

        public Task<int> RevokeAllAsync(string tenantId, string subject, CancellationToken cancellationToken = default)
        {
            var keysToRemove = _refreshStore
                .Where(x => string.Equals(x.Value.TenantId, tenantId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.Value.Subject, subject, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Key)
                .ToArray();

            var removed = 0;
            foreach (var key in keysToRemove)
            {
                if (_refreshStore.TryRemove(key, out _))
                {
                    removed++;
                }
            }

            return Task.FromResult(removed);
        }

        private LoginResponseDto IssueTokens(string subject, string username, string tenantId, IReadOnlyList<string> roles)
        {
            if (string.IsNullOrWhiteSpace(_jwtOptions.Secret))
            {
                throw new InvalidOperationException("Jwt:Secret configuration is missing.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes);
            var jwt = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims:
                [
                    new Claim(JwtRegisteredClaimNames.Sub, subject),
                    new Claim("preferred_username", username),
                    new Claim("unique_name", username),
                    new Claim("tenant_id", tenantId),
                    ..roles.Select(role => new Claim("role", role)),
                ],
                expires: expires,
                signingCredentials: credentials);

            var accessToken = _tokenHandler.WriteToken(jwt);
            var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            _refreshStore[refreshToken] = new RefreshSession(subject, username, tenantId, roles, DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenDays));

            return new LoginResponseDto(accessToken, refreshToken, _jwtOptions.AccessTokenMinutes * 60);
        }

        private IReadOnlyList<DemoUser> LoadDemoUsers()
        {
            var users = _authOptions.DemoUsers
                .Select(x => new DemoUser(x.Username, x.Password, x.Subject, x.TenantId, x.Roles))
                .ToList();

            return users is { Count: > 0 }
                ? users
                : throw new InvalidOperationException("Auth:DemoUsers configuration is missing.");
        }

        public sealed record DemoUser(string Username, string Password, string Subject, string TenantId, IReadOnlyList<string> Roles);

        private sealed record RefreshSession(string Subject, string Username, string TenantId, IReadOnlyList<string> Roles, DateTimeOffset ExpiresAt);
    }
}
