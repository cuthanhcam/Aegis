// Additional comprehensive tests for Aegis.Application layer
// Focus on risk scenarios identified during code review

using Aegis.Application.Features.Query;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Moq;

namespace Aegis.UnitTests.Application;

/// <summary>
/// Tests for query use cases - critical for authorization engine performance
/// </summary>
public sealed class AuthorizationQueryValidationTests
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void ValidateCheckInput_WithEmptySubject_ShouldThrow(string? invalidSubject)
    {
        // This tests a critical validation path - authorization checks must have valid subject
        Assert.Throws<ArgumentException>(() =>
            AuthorizationQueryHelper.ValidateCheckInput(invalidSubject!, "relation", "object"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ValidateCheckInput_WithEmptyRelation_ShouldThrow(string? invalidRelation)
    {
        Assert.Throws<ArgumentException>(() =>
            AuthorizationQueryHelper.ValidateCheckInput("subject", invalidRelation!, "object"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ValidateCheckInput_WithEmptyObject_ShouldThrow(string? invalidObject)
    {
        Assert.Throws<ArgumentException>(() =>
            AuthorizationQueryHelper.ValidateCheckInput("subject", "relation", invalidObject!));
    }

    [Fact]
    public void ValidateCheckInput_WithValidInputs_ShouldNotThrow()
    {
        // Should not throw for valid inputs
        AuthorizationQueryHelper.ValidateCheckInput("user:alice", "editor", "doc:1");
    }
}

/// <summary>
/// Risk Test: Null dependency injection in services
/// If dependencies are null, service fails at unpredictable times
/// </summary>
public sealed class ServiceDependencyInjectionTests
{
    [Fact]
    public void ResolveAuthorizationModelUseCase_ShouldRequireStoreRegistry()
    {
        // Risk: Constructor doesn't validate null dependencies
        Assert.Throws<ArgumentNullException>(() =>
            new ResolveAuthorizationModelUseCase(null!, new Mock<IAuthorizationModelRegistry>().Object));
    }

    [Fact]
    public void ResolveAuthorizationModelUseCase_ShouldRequireModelRegistry()
    {
        // Risk: Constructor doesn't validate null dependencies
        Assert.Throws<ArgumentNullException>(() =>
            new ResolveAuthorizationModelUseCase(new Mock<IStoreRegistry>().Object, null!));
    }

    [Fact]
    public void CheckPermissionUseCase_ShouldRequireAuthEngine()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CheckPermissionUseCase(null!, new Mock<IAuditStore>().Object));
    }

    [Fact]
    public void CheckPermissionUseCase_ShouldRequireAuditStore()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CheckPermissionUseCase(new Mock<IAuthorizationEngine>().Object, null!));
    }
}

/// <summary>
/// Risk Test: CancellationToken propagation
/// If cancellation isn't properly propagated, operations may hang
/// </summary>
public sealed class CancellationTokenPropagationTests
{
    [Fact]
    public async Task QueryAllowTuplesUseCase_ShouldRespectCancellation()
    {
        // Arrange
        var mockRelStore = new Mock<IRelationshipStore>();
        using var cts = new CancellationTokenSource();

        mockRelStore
            .Setup(x => x.QueryAsync(
                It.IsAny<string>(),
                It.IsAny<Subject>(),
                It.IsAny<string>(),
                It.IsAny<ObjectRef>(),
                It.IsAny<RelationshipEffect>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var useCase = new QueryAllowTuplesUseCase(mockRelStore.Object);
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            useCase.ExecuteAsync("store-1", null, null, null, null, cts.Token));
    }
}

/// <summary>
/// Risk Test: Error propagation and exception handling
/// Different use cases handle errors differently - should be consistent
/// </summary>
public sealed class ErrorHandlingTests
{
    [Fact]
    public async Task CheckPermissionUseCase_ShouldPropagateValidationErrors()
    {
        // Arrange
        var mockAuthEngine = new Mock<IAuthorizationEngine>();
        var mockAuditStore = new Mock<IAuditStore>();
        var useCase = new CheckPermissionUseCase(mockAuthEngine.Object, mockAuditStore.Object);

        var invalidRequest = new Contracts.Permissions.CheckRequestDto(
            Subject: "", // Invalid
            Relation: "editor",
            Object: "doc:1",
            ContextualTuples: null,
            Consistency: null,
            AuthorizationModelId: null,
            Context: null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync("store-1", invalidRequest, false));

        // Verify auth engine was never called for invalid input
        mockAuthEngine.Verify(
            x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckPermissionUseCase_ShouldPropagateAuthEngineFaults()
    {
        // Arrange
        var mockAuthEngine = new Mock<IAuthorizationEngine>();
        var mockAuditStore = new Mock<IAuditStore>();
        var useCase = new CheckPermissionUseCase(mockAuthEngine.Object, mockAuditStore.Object);

        mockAuthEngine
            .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Auth engine error"));

        var validRequest = new Contracts.Permissions.CheckRequestDto(
            Subject: "user:alice",
            Relation: "editor",
            Object: "doc:1",
            ContextualTuples: null,
            Consistency: null,
            AuthorizationModelId: null,
            Context: null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync("store-1", validRequest, false));

        Assert.Equal("Auth engine error", ex.Message);
    }
}

/// <summary>
/// Risk Test: Audit store write side effects
/// If audit write fails, should it fail the whole request or just log?
/// Current behavior: propagates exception to caller
/// Risk: Could mask real authorization decision
/// </summary>
public sealed class AuditStoreWriteTests
{
    [Fact]
    public async Task CheckPermissionUseCase_ShouldPropagateAuditStoreFailures()
    {
        // Arrange - this is the current behavior which could be risky
        var mockAuthEngine = new Mock<IAuthorizationEngine>();
        var mockAuditStore = new Mock<IAuditStore>();
        var useCase = new CheckPermissionUseCase(mockAuthEngine.Object, mockAuditStore.Object);

        mockAuthEngine
            .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DecisionResult(true, "Allow", "AUTHORIZED", new List<ExplainTraceStep>()));

        mockAuditStore
            .Setup(x => x.WriteAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Audit store is unavailable"));

        var validRequest = new Contracts.Permissions.CheckRequestDto(
            Subject: "user:alice",
            Relation: "editor",
            Object: "doc:1",
            ContextualTuples: null,
            Consistency: null,
            AuthorizationModelId: null,
            Context: null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync("store-1", validRequest, false));

        // Current behavior: audit failure propagates to caller
        // RISK: Client doesn't know if authorization failed or audit failed
        // RECOMMENDATION: Make audit non-blocking or use circuit breaker pattern
        Assert.Equal("Audit store is unavailable", ex.Message);
    }
}

/// <summary>
/// Risk Test: Batch operation failure modes
/// Batch should handle individual item failures gracefully
/// </summary>
public sealed class BatchProcessingEdgeCasesTests
{
    [Fact]
    public async Task BatchCheckInStoreUseCase_WithMixedValidInvalid_ShouldStopOnFirstInvalid()
    {
        // Arrange
        var mockAuthEngine = new Mock<IAuthorizationEngine>();
        var mockAuditStore = new Mock<IAuditStore>();
        var checkUseCase = new CheckPermissionUseCase(mockAuthEngine.Object, mockAuditStore.Object);
        var batchUseCase = new BatchCheckInStoreUseCase(checkUseCase);

        mockAuthEngine
            .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DecisionResult(true, "Allow", "AUTHORIZED", new List<ExplainTraceStep>()));

        var request = new Contracts.Permissions.BatchCheckRequestDto(
            Items: new List<Contracts.Permissions.CheckInStoreBatchRequestItemDto>
            {
                new("user:alice", "editor", "doc:1", null, null, null, null, "1"),
                new("", "editor", "doc:2", null, null, null, null, "2"), // Invalid
                new("user:charlie", "viewer", "doc:3", null, null, null, null, "3"),
            });

        // Act & Assert - should throw on second item
        await Assert.ThrowsAsync<ArgumentException>(() =>
            batchUseCase.ExecuteAsync("store-1", request));

        // Only first check should execute
        mockAuthEngine.Verify(
            x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BatchCheckInStoreUseCase_ShouldGenerateNumericCorrelationIdsWhenMissing()
    {
        // Arrange
        var mockAuthEngine = new Mock<IAuthorizationEngine>();
        var mockAuditStore = new Mock<IAuditStore>();
        var checkUseCase = new CheckPermissionUseCase(mockAuthEngine.Object, mockAuditStore.Object);
        var batchUseCase = new BatchCheckInStoreUseCase(checkUseCase);

        mockAuthEngine
            .Setup(x => x.CheckAsync(It.IsAny<CheckRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DecisionResult(true, "Allow", "AUTHORIZED", new List<ExplainTraceStep>()));

        var request = new Contracts.Permissions.BatchCheckRequestDto(
            Items: new List<Contracts.Permissions.CheckInStoreBatchRequestItemDto>
            {
                new("user:alice", "editor", "doc:1", null, null, null, null, null), // No correlation ID
                new("user:bob", "viewer", "doc:2", null, null, null, null, "custom-id"),
            });

        // Act
        var result = await batchUseCase.ExecuteAsync("store-1", request);

        // Assert
        Assert.Equal("1", result.Results[0].CorrelationId);
        Assert.Equal("custom-id", result.Results[1].CorrelationId);
    }
}

/// <summary>
/// Risk Test: Query tuple merging with deduplication
/// Complex logic prone to bugs, especially with case sensitivity
/// </summary>
public sealed class TupleMergingDeduplicationTests
{
    private readonly Mock<IRelationshipStore> _mockRelStore;
    private readonly QueryAllowTuplesUseCase _useCase;

    public TupleMergingDeduplicationTests()
    {
        _mockRelStore = new Mock<IRelationshipStore>();
        _useCase = new QueryAllowTuplesUseCase(_mockRelStore.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDeduplicateTuplesAcrossPersistedAndContextual()
    {
        // Arrange
        var persistedTuples = new List<RelationshipTuple>
        {
            new(new Subject("user:alice"), "editor", new ObjectRef("doc:1"), RelationshipEffect.Allow),
            new(new Subject("user:alice"), "editor", new ObjectRef("doc:1"), RelationshipEffect.Allow), // Duplicate
        };

        var contextualTuples = new List<RelationshipTuple>
        {
            new(new Subject("user:alice"), "editor", new ObjectRef("doc:1"), RelationshipEffect.Allow),
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

        // Assert - only one tuple should remain
        Assert.Single(result);
    }

    [Fact]
    public async Task ExecuteAsync_DenyTuplesShouldTakePrecedence()
    {
        // Arrange
        var persistedTuples = new List<RelationshipTuple>
        {
            new(new Subject("user:alice"), "editor", new ObjectRef("doc:1"), RelationshipEffect.Allow),
        };

        var contextualTuples = new List<RelationshipTuple>
        {
            new(new Subject("user:alice"), "editor", new ObjectRef("doc:1"), RelationshipEffect.Deny),
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

        // Assert - deny tuple should prevent allow
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleTuples_ShouldFilterCorrectly()
    {
        // Arrange
        var persistedTuples = new List<RelationshipTuple>
        {
            new(new Subject("user:alice"), "editor", new ObjectRef("doc:1"), RelationshipEffect.Allow),
            new(new Subject("user:bob"), "viewer", new ObjectRef("doc:1"), RelationshipEffect.Allow),
            new(new Subject("user:charlie"), "admin", new ObjectRef("doc:1"), RelationshipEffect.Allow),
        };

        var contextualTuples = new List<RelationshipTuple>
        {
            new(new Subject("user:bob"), "viewer", new ObjectRef("doc:1"), RelationshipEffect.Deny),
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

        // Assert - bob's tuple should be removed, others remain
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, t => t.Subject.Value == "user:bob");
    }
}
