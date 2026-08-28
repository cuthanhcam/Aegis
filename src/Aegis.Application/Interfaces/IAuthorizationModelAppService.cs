using Aegis.Contracts.Administration;

namespace Aegis.Application.Interfaces;

public interface IAuthorizationModelAppService
{
    Task<AuthorizationModelValidationResultDto> ValidateAsync(
        ValidateAuthorizationModelRequestDto request,
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

    Task<AuthorizationModelDiffDto?> DiffAsync(
        string storeId,
        string leftAuthorizationModelId,
        string rightAuthorizationModelId,
        CancellationToken cancellationToken = default);

}
