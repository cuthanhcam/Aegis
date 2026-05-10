using Aegis.Contracts.Authentication;

namespace Aegis.Application.Interfaces;

public interface IAuthAppService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    Task<LoginResponseDto?> RefreshAsync(RefreshRequestDto request, CancellationToken cancellationToken = default);

    Task<bool> LogoutAsync(RefreshRequestDto request, CancellationToken cancellationToken = default);

    Task<int> LogoutAllAsync(string tenantId, string subject, CancellationToken cancellationToken = default);
}
