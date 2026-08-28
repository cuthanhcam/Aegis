using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;

namespace Aegis.Application.Features.Users;

public sealed class UpdateUserUseCase
{
    private readonly IRbacAdminStore _rbacAdminStore;

    public UpdateUserUseCase(IRbacAdminStore rbacAdminStore)
    {
        _rbacAdminStore = rbacAdminStore;
    }

    public Task<UserDto?> ExecuteAsync(
        string tenantId,
        string userId,
        UpdateUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("Tenant ID and user ID are required.");
        }

        return _rbacAdminStore.UpdateUserAsync(
            tenantId,
            userId,
            request.Email,
            request.DisplayName,
            cancellationToken);
    }
}
