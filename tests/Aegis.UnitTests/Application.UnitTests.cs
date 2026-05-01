// Integration test namespace for Application layer
// This file contains comprehensive unit tests for the Aegis.Application layer
// Run with: dotnet test Aegis.UnitTests.csproj --filter "Category=Application"

using Aegis.Application.Features.Permissions;
using Aegis.Application.Features.Query;
using Aegis.Application.Interfaces;
using Aegis.Application.Services;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;
using Aegis.Contracts.Permissions;
using Moq;
using System.Globalization;

namespace Aegis.UnitTests.Application;

public sealed class CheckPermissionUseCaseTests
{
    private readonly Mock<IAuthorizationEngine> _mockAuthEngine;
    private readonly Mock<IAuditStore> _mockAuditStore;
    private readonly CheckPermissionUseCase _useCase;

    public CheckPermissionUseCaseTests()
    {
        _mockAuthEngine = new Mock<IAuthorizationEngine>();
        _mockAuditStore = new Mock<IAuditStore>();
        _useCase = new CheckPermissionUseCase(_mockAuthEngine.Object, _mockAuditStore.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ShouldReturnAllowedDecision()
    {
        // Arrange
        var decision = new DecisionResult(
            allowed: true,
            decision: "Allow",
            reasonCode: "AUTHORIZED",
            trace: new List<ExplainTraceStep>());

        _mockAuthEngine
            .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(decision);

        var request = new CheckRequestDto(
            Subject: "user:alice",
            Relation: "editor",
            Object: "doc:1",
            ContextualTuples: null,
            Consistency: null,
            AuthorizationModelId: null,
            Context: null);

        // Act
        var result = await _useCase.ExecuteAsync("store-1", request, includeTrace: false);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Allowed);
        Assert.Equal("Allow", result.Decision);
        Assert.Null(result.Trace);

        _mockAuthEngine.Verify(
            x => x.CheckAsync(
                It.IsAny<CheckRequest>(),
                false,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithDeniedDecision_ShouldReturnDeniedResponse()
    {
        // Arrange
        var decision = new DecisionResult(
            allowed: false,
            decision: "Deny",
            reasonCode: "UNAUTHORIZED",
            trace: new List<ExplainTraceStep>());

        _mockAuthEngine
            .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(decision);

        var request = new CheckRequestDto(
            Subject: "user:bob",
            Relation: "viewer",
            Object: "doc:2",
            ContextualTuples: null,
            Consistency: null,
            AuthorizationModelId: null,
            Context: null);

        // Act
        var result = await _useCase.ExecuteAsync("store-1", request, includeTrace: false);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Allowed);
        Assert.Equal("Deny", result.Decision);
    }

    [Fact]
    public async Task ExecuteAsync_WithIncludeTrace_ShouldReturnTraceSteps()
    {
        // Arrange
        var traceSteps = new List<ExplainTraceStep>
        {
            new ExplainTraceStep("step-1", true, "user:alice|editor|doc:1")
        };

        var decision = new DecisionResult(
            allowed: true,
            decision: "Allow",
            reasonCode: "AUTHORIZED",
            trace: traceSteps);

        _mockAuthEngine
            .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(decision);

        var request = new CheckRequestDto(
            Subject: "user:alice",
            Relation: "editor",
            Object: "doc:1",
            ContextualTuples: null,
            Consistency: null,
            AuthorizationModelId: null,
            Context: null);

        // Act
        var result = await _useCase.ExecuteAsync("store-1", request, includeTrace: true);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Trace);
        Assert.Single(result.Trace);
        Assert.Equal("step-1", result.Trace[0].Step);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallAuditStore()
    {
        // Arrange
        var decision = new DecisionResult(
            allowed: true,
            decision: "Allow",
            reasonCode: "AUTHORIZED",
            trace: new List<ExplainTraceStep>());

        _mockAuthEngine
            .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(decision);

        var request = new CheckRequestDto(
            Subject: "user:alice",
            Relation: "editor",
            Object: "doc:1",
            ContextualTuples: null,
            Consistency: null,
            AuthorizationModelId: null,
            Context: null);

        // Act
        await _useCase.ExecuteAsync("store-1", request, includeTrace: false);

        // Assert
        _mockAuditStore.Verify(
            x => x.WriteAsync(
                It.Is<AuditEvent>(ae =>
                    ae.TenantId == "store-1" &&
                    ae.Action == "check" &&
                    ae.Decision == "Allow"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithExplain_ShouldRecordExplainAction()
    {
        // Arrange
        var decision = new DecisionResult(
            allowed: true,
            decision: "Allow",
            reasonCode: "AUTHORIZED",
            trace: new List<ExplainTraceStep>());

        _mockAuthEngine
            .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(decision);

        var request = new CheckRequestDto(
            Subject: "user:alice",
            Relation: "editor",
            Object: "doc:1",
            ContextualTuples: null,
            Consistency: null,
            AuthorizationModelId: null,
            Context: null);

        // Act
        await _useCase.ExecuteAsync("store-1", request, includeTrace: true);

        // Assert
        _mockAuditStore.Verify(
            x => x.WriteAsync(
                It.Is<AuditEvent>(ae => ae.Action == "explain"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowIfValidationFails()
    {
        // Arrange - invalid request will fail validation in helper
        var request = new CheckRequestDto(
            Subject: "",
            Relation: "editor",
            Object: "doc:1",
            ContextualTuples: null,
            Consistency: null,
            AuthorizationModelId: null,
            Context: null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _useCase.ExecuteAsync("store-1", request, includeTrace: false));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPropagateAuthEngineCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockAuthEngine
            .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var request = new CheckRequestDto(
            Subject: "user:alice",
            Relation: "editor",
            Object: "doc:1",
            ContextualTuples: null,
            Consistency: null,
            AuthorizationModelId: null,
            Context: null);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _useCase.ExecuteAsync("store-1", request, includeTrace: false, cts.Token));
    }
}

public sealed class BatchCheckInStoreUseCaseTests
{
    private readonly Mock<IAuthorizationEngine> _mockAuthEngine;
    private readonly Mock<IAuditStore> _mockAuditStore;
    private readonly CheckPermissionUseCase _checkPermissionUseCase;
    private readonly BatchCheckInStoreUseCase _useCase;

    public BatchCheckInStoreUseCaseTests()
    {
        _mockAuthEngine = new Mock<IAuthorizationEngine>();
        _mockAuditStore = new Mock<IAuditStore>();
        _checkPermissionUseCase = new CheckPermissionUseCase(_mockAuthEngine.Object, _mockAuditStore.Object);
        _useCase = new BatchCheckInStoreUseCase(_checkPermissionUseCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidBatch_ShouldReturnAllResults()
    {
        // Arrange
        var decision = new DecisionResult(
            allowed: true,
            decision: "Allow",
            reasonCode: "AUTHORIZED",
            trace: new List<ExplainTraceStep>());

        _mockAuthEngine
            .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(decision);

        var request = new BatchCheckRequestDto(Items: new List<CheckInStoreBatchRequestItemDto>
        {
            new("user:alice", "editor", "doc:1", null, null, null, null, null),
            new("user:bob", "viewer", "doc:2", null, null, null, null, null),
        });

        // Act
        var result = await _useCase.ExecuteAsync("store-1", request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Results);
        Assert.Equal(2, result.Results.Count);
        Assert.All(result.Results, item => Assert.True(item.Result.Allowed));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGenerateCorrelationIds()
    {
        // Arrange
        var decision = new DecisionResult(
            allowed: true,
            decision: "Allow",
            reasonCode: "AUTHORIZED",
            trace: new List<ExplainTraceStep>());

        _mockAuthEngine
            .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(decision);

        var request = new BatchCheckRequestDto(Items: new List<CheckInStoreBatchRequestItemDto>
        {
            new("user:alice", "editor", "doc:1", null, null, null, null, null),
            new("user:bob", "viewer", "doc:2", null, null, null, null, "my-correlation-id"),
        });

        // Act
        var result = await _useCase.ExecuteAsync("store-1", request);

        // Assert
        Assert.Equal("1", result.Results[0].CorrelationId);
        Assert.Equal("my-correlation-id", result.Results[1].CorrelationId);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyItems_ShouldThrow()
    {
        // Arrange
        var request = new BatchCheckRequestDto(Items: new List<CheckInStoreBatchRequestItemDto>());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _useCase.ExecuteAsync("store-1", request));
    }

    [Fact]
    public async Task ExecuteAsync_WithNullItems_ShouldThrow()
    {
        // Arrange
        var request = new BatchCheckRequestDto(Items: null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _useCase.ExecuteAsync("store-1", request));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ExecuteAsync_ShouldProcessAllItemsRegardlessOfCount(int itemCount)
    {
        // Arrange
        var decision = new DecisionResult(
            allowed: true,
            decision: "Allow",
            reasonCode: "AUTHORIZED",
            trace: new List<ExplainTraceStep>());

        _mockAuthEngine
            .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(decision);

        var items = Enumerable
            .Range(1, itemCount)
            .Select(i => new CheckInStoreBatchRequestItemDto(
                User: $"user:{i}",
                Relation: "editor",
                Object: $"doc:{i}",
                ContextualTuples: null,
                Consistency: null,
                AuthorizationModelId: null,
                Context: null,
                CorrelationId: null))
            .ToList();

        var request = new BatchCheckRequestDto(Items: items);

        // Act
        var result = await _useCase.ExecuteAsync("store-1", request);

        // Assert
        Assert.Equal(itemCount, result.Results.Count);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStopOnValidationError()
    {
        // Arrange - second item has invalid subject (empty string)
        _mockAuthEngine
            .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DecisionResult(true, "Allow", "AUTHORIZED", new List<ExplainTraceStep>()));

        var request = new BatchCheckRequestDto(Items: new List<CheckInStoreBatchRequestItemDto>
        {
            new("user:alice", "editor", "doc:1", null, null, null, null, null),
            new("", "editor", "doc:2", null, null, null, null, null), // Invalid
        });

        // Act & Assert - should throw on second item validation
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _useCase.ExecuteAsync("store-1", request));

        // Only first check should have been attempted
        _mockAuthEngine.Verify(
            x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

public sealed class PermissionAppServiceTests
{
    private readonly Mock<IAuthorizationEngine> _mockAuthEngine;
    private readonly Mock<IAuditStore> _mockAuditStore;
    private readonly Mock<IStoreRegistry> _mockStoreRegistry;
    private readonly Mock<IAuthorizationModelRegistry> _mockAuthModelRegistry;
    private readonly PermissionAppService _service;

    public PermissionAppServiceTests()
    {
        _mockAuthEngine = new Mock<IAuthorizationEngine>();
        _mockAuditStore = new Mock<IAuditStore>();
        _mockStoreRegistry = new Mock<IStoreRegistry>();
        _mockAuthModelRegistry = new Mock<IAuthorizationModelRegistry>();

        _service = PermissionAppService.CreateForTests(
            _mockAuthEngine.Object,
            _mockAuditStore.Object,
            _mockStoreRegistry.Object,
            _mockAuthModelRegistry.Object);
    }

    [Fact]
    public async Task CheckAsync_ShouldCallCheckPermissionUseCase()
    {
        // Arrange
        _mockAuthEngine
            .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DecisionResult(true, "Allow", "AUTHORIZED", new List<ExplainTraceStep>()));

        var request = new CheckRequestDto(
            Subject: "user:alice",
            Relation: "editor",
            Object: "doc:1",
            ContextualTuples: null,
            Consistency: null,
            AuthorizationModelId: null,
            Context: null);

        // Act
        var result = await _service.CheckAsync("store-1", request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Allowed);
    }

    [Fact]
    public async Task ExplainAsync_ShouldIncludeTrace()
    {
        // Arrange
        var traceSteps = new List<ExplainTraceStep>
        {
            new ExplainTraceStep("step-1", true, "user:alice|editor|doc:1")
        };

        _mockAuthEngine
            .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DecisionResult(true, "Allow", "AUTHORIZED", traceSteps));

        var request = new CheckRequestDto(
            Subject: "user:alice",
            Relation: "editor",
            Object: "doc:1",
            ContextualTuples: null,
            Consistency: null,
            AuthorizationModelId: null,
            Context: null);

        // Act
        var result = await _service.ExplainAsync("store-1", request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Trace);
        Assert.Single(result.Trace);
    }

    [Fact]
    public async Task BatchCheckInStoreAsync_ShouldProcessBatch()
    {
        // Arrange
        _mockAuthEngine
            .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DecisionResult(true, "Allow", "AUTHORIZED", new List<ExplainTraceStep>()));

        var request = new BatchCheckRequestDto(Items: new List<CheckInStoreBatchRequestItemDto>
        {
            new("user:alice", "editor", "doc:1", null, null, null, null, null),
        });

        // Act
        var result = await _service.BatchCheckInStoreAsync("store-1", request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Results);
        Assert.Single(result.Results);
    }

    [Fact]
    public async Task QueryAuditAsync_ShouldReturnAuditEvents()
    {
        // Arrange
        var auditEvents = new List<AuditEventDto>
        {
            new(id: "1", tenantId: "store-1", action: "check", subject: "user:alice", relation: "editor",
                @object: "doc:1", decision: "Allow", reasonCode: "AUTHORIZED", timestamp: DateTimeOffset.UtcNow)
        };

        _mockAuditStore
            .Setup(x => x.QueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditEvents);

        // Act
        var result = await _service.QueryAuditAsync("store-1", action: "check", decision: null);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("check", result[0].Action);
    }
}

public sealed class QueryAllowTuplesUseCaseTests
{
    private readonly Mock<IRelationshipStore> _mockRelStore;
    private readonly QueryAllowTuplesUseCase _useCase;

    public QueryAllowTuplesUseCaseTests()
    {
        _mockRelStore = new Mock<IRelationshipStore>();
        _useCase = new QueryAllowTuplesUseCase(_mockRelStore.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoContextualTuples_ShouldReturnPersistedOnly()
    {
        // Arrange
        var persistedTuples = new List<RelationshipTuple>
        {
            new(new Subject("user:alice"), "editor", new ObjectRef("doc:1"), RelationshipEffect.Allow)
        };

        _mockRelStore
            .Setup(x => x.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<Subject>(),
                It.IsAny<string>(),
                It.IsAny<ObjectRef>(),
                RelationshipEffect.Allow,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(persistedTuples);

        // Act
        var result = await _useCase.ExecuteAsync(
            "store-1",
            subject: null,
            relation: null,
            @object: null,
            contextualTuples: null,
            CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("user:alice", result[0].Subject.Value);
    }

    [Fact]
    public async Task ExecuteAsync_WithContextualTuples_ShouldMergeResults()
    {
        // Arrange
        var persistedTuples = new List<RelationshipTuple>
        {
            new(new Subject("user:alice"), "editor", new ObjectRef("doc:1"), RelationshipEffect.Allow)
        };

        var contextualTuples = new List<RelationshipTuple>
        {
            new(new Subject("user:bob"), "viewer", new ObjectRef("doc:1"), RelationshipEffect.Allow)
        };

        _mockRelStore
            .Setup(x => x.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<Subject>(),
                It.IsAny<string>(),
                It.IsAny<ObjectRef>(),
                RelationshipEffect.Allow,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(persistedTuples);

        // Act
        var result = await _useCase.ExecuteAsync(
            "store-1",
            subject: null,
            relation: null,
            @object: null,
            contextualTuples: contextualTuples,
            CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WithDenyContextualTuple_ShouldRemoveFromMerged()
    {
        // Arrange
        var persistedTuples = new List<RelationshipTuple>
        {
            new(new Subject("user:alice"), "editor", new ObjectRef("doc:1"), RelationshipEffect.Allow)
        };

        var contextualTuples = new List<RelationshipTuple>
        {
            new(new Subject("user:alice"), "editor", new ObjectRef("doc:1"), RelationshipEffect.Deny)
        };

        _mockRelStore
            .Setup(x => x.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<Subject>(),
                It.IsAny<string>(),
                It.IsAny<ObjectRef>(),
                RelationshipEffect.Allow,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(persistedTuples);

        // Act
        var result = await _useCase.ExecuteAsync(
            "store-1",
            subject: null,
            relation: null,
            @object: null,
            contextualTuples: contextualTuples,
            CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFilterBySubjectIfProvided()
    {
        // Arrange
        var persistedTuples = new List<RelationshipTuple>
        {
            new(new Subject("user:alice"), "editor", new ObjectRef("doc:1"), RelationshipEffect.Allow)
        };

        _mockRelStore
            .Setup(x => x.QueryAsync(
                "store-1",
                new Subject("user:alice"),
                It.IsAny<string>(),
                It.IsAny<ObjectRef>(),
                RelationshipEffect.Allow,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(persistedTuples);

        // Act
        var result = await _useCase.ExecuteAsync(
            "store-1",
            subject: new Subject("user:alice"),
            relation: null,
            @object: null,
            contextualTuples: null,
            CancellationToken.None);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task ExecuteAsync_CaseInsensitiveMatching()
    {
        // Arrange
        var persistedTuples = new List<RelationshipTuple>
        {
            new(new Subject("USER:ALICE"), "EDITOR", new ObjectRef("DOC:1"), RelationshipEffect.Allow)
        };

        var contextualTuples = new List<RelationshipTuple>
        {
            new(new Subject("user:alice"), "editor", new ObjectRef("doc:1"), RelationshipEffect.Deny)
        };

        _mockRelStore
            .Setup(x => x.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<Subject>(),
                It.IsAny<string>(),
                It.IsAny<ObjectRef>(),
                RelationshipEffect.Allow,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(persistedTuples);

        // Act
        var result = await _useCase.ExecuteAsync(
            "store-1",
            subject: null,
            relation: null,
            @object: null,
            contextualTuples: contextualTuples,
            CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }
}
