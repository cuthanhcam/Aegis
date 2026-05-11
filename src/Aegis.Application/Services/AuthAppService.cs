using Aegis.Application.Interfaces;
using Aegis.Contracts.Authentication;

namespace Aegis.Application.Services;

public sealed class AuthAppService : IAuthAppService
{
    private readonly IAuthSessionService _authSessionService;

    public AuthAppService(IAuthSessionService authSessionService)
    {
        _authSessionService = authSessionService;
    }

    public Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("username and password are required.");
        }

        return _authSessionService.LoginAsync(request.Username, request.Password, cancellationToken);
    }

    public Task<LoginResponseDto?> RefreshAsync(RefreshRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Task.FromResult<LoginResponseDto?>(null);
        }

        return _authSessionService.RefreshAsync(request.RefreshToken, cancellationToken);
    }

    public Task<bool> LogoutAsync(RefreshRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Task.FromResult(false);
        }

        return _authSessionService.RevokeAsync(request.RefreshToken, cancellationToken);
    }

    public Task<int> LogoutAllAsync(string tenantId, string subject, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("tenantId and subject are required.");
        }

        return _authSessionService.RevokeAllAsync(tenantId, subject, cancellationToken);
    }
}
