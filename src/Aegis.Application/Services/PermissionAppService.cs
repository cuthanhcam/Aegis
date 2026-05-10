using Aegis.Application.Features.Permissions;
using Aegis.Application.Features.Query;
using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Compatibility;
using Aegis.Contracts.Permissions;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Application.Services
{
    public sealed class PermissionAppService : IPermissionAppService
    {
        private readonly CheckPermissionUseCase _checkPermissionUseCase;
        private readonly BatchCheckInStoreUseCase _batchCheckInStoreUseCase;
        private readonly ResolveAuthorizationModelUseCase _resolveAuthorizationModelUseCase;
        private readonly BatchCheckAegisCompatUseCase _batchCheckAegisCompatUseCase;
        private readonly QueryAuditUseCase _queryAuditUseCase;

        [ActivatorUtilitiesConstructor]
        public PermissionAppService(
            CheckPermissionUseCase checkPermissionUseCase,
            BatchCheckInStoreUseCase batchCheckInStoreUseCase,
            ResolveAuthorizationModelUseCase resolveAuthorizationModelUseCase,
            BatchCheckAegisCompatUseCase batchCheckAegisCompatUseCase,
            QueryAuditUseCase queryAuditUseCase)
        {
            _checkPermissionUseCase = checkPermissionUseCase;
            _batchCheckInStoreUseCase = batchCheckInStoreUseCase;
            _resolveAuthorizationModelUseCase = resolveAuthorizationModelUseCase;
            _batchCheckAegisCompatUseCase = batchCheckAegisCompatUseCase;
            _queryAuditUseCase = queryAuditUseCase;
        }

        public static PermissionAppService CreateForTests(
            IAuthorizationEngine authorizationEngine,
            IAuditStore auditStore,
            IStoreRegistry storeRegistry,
            IAuthorizationModelRegistry authorizationModelRegistry)
        {
            var resolveAuthorizationModelUseCase = new ResolveAuthorizationModelUseCase(storeRegistry, authorizationModelRegistry);
            var checkPermissionUseCase = new CheckPermissionUseCase(authorizationEngine, auditStore);
            var batchCheckInStoreUseCase = new BatchCheckInStoreUseCase(checkPermissionUseCase);
            var queryAuditUseCase = new QueryAuditUseCase(auditStore);

            return new PermissionAppService(
                checkPermissionUseCase,
                batchCheckInStoreUseCase,
                resolveAuthorizationModelUseCase,
                new BatchCheckAegisCompatUseCase(storeRegistry, resolveAuthorizationModelUseCase, checkPermissionUseCase),
                queryAuditUseCase);
        }

        public async Task<CheckResponseDto> CheckAsync(string tenantId, CheckRequestDto request, CancellationToken cancellationToken = default)
        {
            return await _checkPermissionUseCase.ExecuteAsync(tenantId, request, includeTrace: false, cancellationToken);
        }

        public async Task<CheckResponseDto> ExplainAsync(string tenantId, CheckRequestDto request, CancellationToken cancellationToken = default)
        {
            return await _checkPermissionUseCase.ExecuteAsync(tenantId, request, includeTrace: true, cancellationToken);
        }

        public Task<CheckResponseDto> CheckInStoreAsync(string storeId, StoreCheckRequestDto request, CancellationToken cancellationToken = default)
        {
            return CheckAsync(
                storeId,
                new CheckRequestDto(request.User, request.Relation, request.Object, request.ContextualTuples, request.Consistency, request.AuthorizationModelId, request.Context),
                cancellationToken);
        }

        public Task<CheckResponseDto> ExplainInStoreAsync(string storeId, StoreCheckRequestDto request, CancellationToken cancellationToken = default)
        {
            return ExplainAsync(
                storeId,
                new CheckRequestDto(request.User, request.Relation, request.Object, request.ContextualTuples, request.Consistency, request.AuthorizationModelId, request.Context),
                cancellationToken);
        }

        public async Task<BatchCheckResponseDto> BatchCheckInStoreAsync(
            string storeId,
            BatchCheckRequestDto request,
            CancellationToken cancellationToken = default)
        {
            return await _batchCheckInStoreUseCase.ExecuteAsync(storeId, request, cancellationToken);
        }

        public async Task<AegisCompatCheckResponseDto> CheckAegisCompatInStoreAsync(
            string storeId,
            AegisCompatCheckRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var resolvedAuthorizationModelId = await _resolveAuthorizationModelUseCase.ExecuteAsync(
                storeId,
                request.AuthorizationModelId,
                cancellationToken);

            var check = await _checkPermissionUseCase.ExecuteAsync(
                storeId,
                new CheckRequestDto(
                    request.TupleKey.User,
                    request.TupleKey.Relation,
                    request.TupleKey.Object,
                    AuthorizationQueryHelper.ToContextualTuples(request.ContextualTuples),
                    request.Consistency,
                    resolvedAuthorizationModelId,
                    request.Context),
                includeTrace: false,
                cancellationToken);

            return new AegisCompatCheckResponseDto(check.Allowed);
        }

        public async Task<AegisCompatBatchCheckResponseDto> BatchCheckAegisCompatInStoreAsync(
            string storeId,
            AegisCompatBatchCheckRequestDto request,
            CancellationToken cancellationToken = default)
        {
            return await _batchCheckAegisCompatUseCase.ExecuteAsync(storeId, request, cancellationToken);
        }

        public Task<string> ResolveAuthorizationModelIdForStoreAsync(
            string storeId,
            string? requestedAuthorizationModelId,
            CancellationToken cancellationToken = default)
        {
            return _resolveAuthorizationModelUseCase.ExecuteAsync(storeId, requestedAuthorizationModelId, cancellationToken);
        }

        public async Task<IReadOnlyList<AuditEventDto>> QueryAuditAsync(
            string tenantId,
            string? action,
            string? decision,
            CancellationToken cancellationToken = default)
        {
            return await _queryAuditUseCase.ExecuteAsync(tenantId, action, decision, cancellationToken);
        }
    }
}
