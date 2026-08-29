namespace Aegis.Application.Interfaces;

public interface IStoreDeletionRepository
{
    Task<bool> DeleteAsync(
        string tenantId,
        string storeId,
        CancellationToken cancellationToken = default);
}
