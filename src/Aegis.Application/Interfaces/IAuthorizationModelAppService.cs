using Aegis.Contracts.Administration;

namespace Aegis.Application.Interfaces;

public interface IAuthorizationModelAppService
{
    Task<AuthorizationModelDto> CreateAsync(
        string storeId,
        CreateAuthorizationModelRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuthorizationModelDto>> ListAsync(
        string storeId,
        CancellationToken cancellationToken = default);

    Task<AuthorizationModelDto?> GetLatestAsync(
        string storeId,
        CancellationToken cancellationToken = default);

    Task<AuthorizationModelDto?> GetByIdAsync(
        string storeId,
        string authorizationModelId,
        CancellationToken cancellationToken = default);

    Task<AuthorizationModelDto?> UpdateAsync(
        string storeId,
        string authorizationModelId,
        CreateAuthorizationModelRequestDto request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string storeId,
        string authorizationModelId,
        CancellationToken cancellationToken = default);
}
