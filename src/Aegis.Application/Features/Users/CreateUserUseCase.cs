using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;

namespace Aegis.Application.Features.Users;

public sealed class CreateUserUseCase
{
    private readonly IRbacAdminStore _rbacAdminStore;

    public CreateUserUseCase(IRbacAdminStore rbacAdminStore)
    {
        _rbacAdminStore = rbacAdminStore;
    }

    public Task<UserDto> ExecuteAsync(
        string tenantId,
        CreateUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new ArgumentException("Tenant ID and user ID are required.");
        }

        return _rbacAdminStore.CreateUserAsync(
            tenantId,
            request.UserId,
            request.Email,
            request.DisplayName,
            cancellationToken);
    }
}
