using Aegis.Application.Interfaces;
using Aegis.Contracts.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Aegis.Infrastructure.Identity
{
    public sealed class JwtAuthSessionService : IAuthSessionService
    {
        private readonly IConfiguration _configuration;
        private readonly JwtSecurityTokenHandler _tokenHandler = new();
        private readonly ConcurrentDictionary<string, RefreshSession> _refreshStore = new(StringComparer.Ordinal);

        private static readonly IReadOnlyList<DemoUser> Users =
        [
            new DemoUser("admin", "admin123", "user:admin", "default", ["authorization_admin"]),
            new DemoUser("dev", "dev123", "user:dev", "tenant-dev", Array.Empty<string>()),
        ];

        public JwtAuthSessionService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<LoginResponseDto?> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            var user = Users.FirstOrDefault(x => string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase) && x.Password == password);
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
            var secret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret configuration is missing.");
            var issuer = _configuration["Jwt:Issuer"] ?? "Aegis";
            var audience = _configuration["Jwt:Audience"] ?? "Aegis.Client";
            var accessMinutes = int.TryParse(_configuration["Jwt:AccessTokenMinutes"], out var am) ? am : 60;
            var refreshDays = int.TryParse(_configuration["Jwt:RefreshTokenDays"], out var rd) ? rd : 7;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddMinutes(accessMinutes);
            var jwt = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
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
            _refreshStore[refreshToken] = new RefreshSession(subject, username, tenantId, roles, DateTimeOffset.UtcNow.AddDays(refreshDays));

            return new LoginResponseDto(accessToken, refreshToken, accessMinutes * 60);
        }

        private sealed record DemoUser(string Username, string Password, string Subject, string TenantId, IReadOnlyList<string> Roles);

        private sealed record RefreshSession(string Subject, string Username, string TenantId, IReadOnlyList<string> Roles, DateTimeOffset ExpiresAt);
    }
}
