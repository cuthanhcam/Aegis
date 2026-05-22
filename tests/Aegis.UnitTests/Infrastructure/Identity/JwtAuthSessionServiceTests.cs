using System.IdentityModel.Tokens.Jwt;
using Aegis.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Aegis.UnitTests.Infrastructure.Identity;

public sealed class JwtAuthSessionServiceTests
{
    [Fact]
    public async Task Login_async_issues_authorization_admin_role_for_admin_user()
    {
        var configuration = new Mock<IConfiguration>();
        configuration.SetupGet(x => x["Jwt:Secret"]).Returns("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");
        configuration.SetupGet(x => x["Jwt:Issuer"]).Returns("Aegis");
        configuration.SetupGet(x => x["Jwt:Audience"]).Returns("Aegis.Client");
        configuration.SetupGet(x => x["Jwt:AccessTokenMinutes"]).Returns("60");
        configuration.SetupGet(x => x["Jwt:RefreshTokenDays"]).Returns("7");

        var service = new JwtAuthSessionService(configuration.Object);

        var login = await service.LoginAsync("admin", "admin123");

        Assert.NotNull(login);
        Assert.NotNull(login!.AccessToken);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken);
        Assert.Contains(token.Claims, claim => claim.Type == "role" && claim.Value == "authorization_admin");
        Assert.Contains(token.Claims, claim => claim.Type == "tenant_id" && claim.Value == "default");
    }
}
