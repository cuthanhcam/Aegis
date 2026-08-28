using Aegis.Application.Interfaces;

namespace Aegis.Application.Features.Users;

public sealed class DeleteUserUseCase
{
    private readonly IRbacAdminStore _rbacAdminStore;

    public DeleteUserUseCase(IRbacAdminStore rbacAdminStore)
    {
        _rbacAdminStore = rbacAdminStore;
    }

    public Task<bool> ExecuteAsync(
        string tenantId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("Tenant ID and user ID are required.");
        }

        return _rbacAdminStore.DeleteUserAsync(tenantId, userId, cancellationToken);
    }
}
