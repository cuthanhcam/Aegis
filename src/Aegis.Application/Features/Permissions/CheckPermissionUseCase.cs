using Aegis.Application.Features.Query;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Common;
using Aegis.Contracts.Permissions;

namespace Aegis.Application.Features.Permissions;

public sealed class CheckPermissionUseCase
{
    private readonly IAuthorizationEngine _authorizationEngine;
    private readonly IAuditStore _auditStore;

    public CheckPermissionUseCase(
        IAuthorizationEngine authorizationEngine,
        IAuditStore auditStore)
    {
        _authorizationEngine = authorizationEngine ?? throw new ArgumentNullException(nameof(authorizationEngine));
        _auditStore = auditStore ?? throw new ArgumentNullException(nameof(auditStore));
    }

    public async Task<CheckResponseDto> ExecuteAsync(
        string tenantId,
        CheckRequestDto request,
        bool includeTrace,
        CancellationToken cancellationToken = default,
        string? storeId = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        AuthorizationQueryHelper.ValidateCheckInput(request.Subject, request.Relation, request.Object);

        var decision = await _authorizationEngine.CheckAsync(
            new CheckRequest(
                tenantId,
                new Subject(request.Subject),
                request.Relation,
                new ObjectRef(request.Object),
                AuthorizationQueryHelper.ParseContextualTuples(request.ContextualTuples),
                AuthorizationQueryHelper.ParseConsistency(request.Consistency),
                request.AuthorizationModelId,
                request.Context,
                storeId),
            includeTrace,
            cancellationToken);

        await _auditStore.WriteAsync(
            new AuditEvent(
                tenantId,
                includeTrace ? "explain" : "check",
                request.Subject,
                request.Relation,
                request.Object,
                decision.Decision,
                decision.ReasonCode,
                DateTimeOffset.UtcNow,
                storeId),
            cancellationToken);

        return ToDto(decision, includeTrace);
    }

    private static CheckResponseDto ToDto(DecisionResult decision, bool includeTrace)
    {
        var trace = includeTrace
            ? decision.Trace.Select(x => new ExplainTraceStepDto(x.Step, x.Result, x.Tuple)).ToList()
            : null;

        return new CheckResponseDto(decision.Allowed, decision.Decision, decision.ReasonCode, trace);
    }
}
