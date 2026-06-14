using Aegis.Contracts.Administration;
using Aegis.Contracts.Compatibility;
using Aegis.Contracts.Permissions;

namespace Aegis.Application.Interfaces;

public interface IPermissionAppService
{
    Task<CheckResponseDto> CheckAsync(
        string tenantId,
        CheckRequestDto request,
        CancellationToken cancellationToken = default);

    Task<CheckResponseDto> ExplainAsync(
        string tenantId,
        CheckRequestDto request,
        CancellationToken cancellationToken = default);

    Task<CheckResponseDto> CheckInStoreAsync(
        string storeId,
        StoreCheckRequestDto request,
        CancellationToken cancellationToken = default);

    Task<CheckResponseDto> CheckInStoreAsync(
        string tenantId,
        string storeId,
        StoreCheckRequestDto request,
        CancellationToken cancellationToken = default);

    Task<CheckResponseDto> ExplainInStoreAsync(
        string storeId,
        StoreCheckRequestDto request,
        CancellationToken cancellationToken = default);

    Task<CheckResponseDto> ExplainInStoreAsync(
        string tenantId,
        string storeId,
        StoreCheckRequestDto request,
        CancellationToken cancellationToken = default);

    Task<BatchCheckResponseDto> BatchCheckInStoreAsync(
        string storeId,
        BatchCheckRequestDto request,
        CancellationToken cancellationToken = default);

    Task<BatchCheckResponseDto> BatchCheckInStoreAsync(
        string tenantId,
        string storeId,
        BatchCheckRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AegisCompatCheckResponseDto> CheckAegisCompatInStoreAsync(
        string storeId,
        AegisCompatCheckRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AegisCompatBatchCheckResponseDto> BatchCheckAegisCompatInStoreAsync(
        string storeId,
        AegisCompatBatchCheckRequestDto request,
        CancellationToken cancellationToken = default);

    Task<string> ResolveAuthorizationModelIdForStoreAsync(
        string storeId,
        string? requestedAuthorizationModelId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditEventDto>> QueryAuditAsync(
        string tenantId,
        string? action,
        string? decision,
        string? storeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditEventDto>> QueryAuditAsync(
        string tenantId,
        string? action,
        string? decision,
        CancellationToken cancellationToken = default)
    {
        return QueryAuditAsync(tenantId, action, decision, storeId: null, cancellationToken);
    }
}
