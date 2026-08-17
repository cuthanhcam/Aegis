using Aegis.Api.Controllers.Helpers;
using Aegis.Application.Interfaces;
using Aegis.Contracts.Authentication;
using Aegis.Contracts.Common;
using Aegis.SharedKernel.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Aegis.Api.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    [AllowAnonymous]
    public sealed class AuthController : ControllerBase
    {
        private readonly IAuthAppService _authAppService;
        private readonly JwtOptions _jwtOptions;
        private const string RefreshCookieName = "aegis.refreshToken";
        private const string RefreshCookiePath = "/api/v1/auth";

        public AuthController(IAuthAppService authAppService, IOptions<JwtOptions> jwtOptions)
        {
            _authAppService = authAppService;
            _jwtOptions = jwtOptions.Value;
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth-sensitive")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
            [FromBody] LoginRequestDto request,
            CancellationToken cancellationToken)
        {
            var result = await _authAppService.LoginAsync(request, cancellationToken);
            if (result is null)
            {
                return Unauthorized(ApiResponse<LoginResponseDto>.Fail(NativeErrorCodes.InvalidCredentials, "Invalid username or password."));
            }

            if (!string.IsNullOrWhiteSpace(result.RefreshToken))
            {
                AuthControllerHelpers.SetRefreshCookie(Request, Response, RefreshCookieName, result.RefreshToken, RefreshCookiePath, TimeSpan.FromDays(_jwtOptions.RefreshTokenDays));
            }

            return this.OkResponse(result with { RefreshToken = null });
        }

        [HttpPost("refresh")]
        [EnableRateLimiting("auth-sensitive")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Refresh(
            [FromBody] RefreshRequestDto request,
            CancellationToken cancellationToken)
        {
            var refreshToken = AuthControllerHelpers.ResolveRefreshToken(Request, request, RefreshCookieName);

            var result = await _authAppService.RefreshAsync(new RefreshRequestDto(refreshToken), cancellationToken);
            if (result is null)
            {
                AuthControllerHelpers.DeleteRefreshCookie(Response, RefreshCookieName, "/api/v1/auth");
                return Unauthorized(ApiResponse<LoginResponseDto>.Fail(NativeErrorCodes.InvalidRefreshToken, "refresh token is invalid or expired."));
            }

            if (!string.IsNullOrWhiteSpace(result.RefreshToken))
            {
                AuthControllerHelpers.SetRefreshCookie(Request, Response, RefreshCookieName, result.RefreshToken, RefreshCookiePath, TimeSpan.FromDays(_jwtOptions.RefreshTokenDays));
            }

            return this.OkResponse(result with { RefreshToken = null });
        }

        [HttpGet("me")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        public ActionResult<ApiResponse<UserProfileDto>> Me()
        {
            var profile = AuthControllerHelpers.CreateUserProfile(User);
            return this.OkResponse(profile);
        }

        [HttpPost("logout")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> Logout(
            [FromBody] RefreshRequestDto request,
            CancellationToken cancellationToken)
        {
            var refreshToken = AuthControllerHelpers.ResolveRefreshToken(Request, request, RefreshCookieName);

            var revoked = await _authAppService.LogoutAsync(new RefreshRequestDto(refreshToken), cancellationToken);
            AuthControllerHelpers.DeleteRefreshCookie(Response, RefreshCookieName, RefreshCookiePath);
            return this.OkResponse(revoked ? "revoked" : "not-found");
        }

        [HttpPost("logout-all")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> LogoutAll(CancellationToken cancellationToken)
        {
            var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? string.Empty;

            var tenantId = User.FindFirst("tenant_id")?.Value
                ?? User.FindFirst("tid")?.Value
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(tenantId))
            {
                return Unauthorized(ApiResponse<string>.Fail(NativeErrorCodes.Unauthorized, "Missing subject or tenant in token claims."));
            }

            var revokedCount = await _authAppService.LogoutAllAsync(tenantId, subject, cancellationToken);
            AuthControllerHelpers.DeleteRefreshCookie(Response, RefreshCookieName, RefreshCookiePath);
            return this.OkResponse($"revoked:{revokedCount}");
        }
    }
}
