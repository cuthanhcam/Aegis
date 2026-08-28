using Aegis.Application.Features.AuthorizationModels;
using Aegis.Contracts.Administration;

namespace Aegis.UnitTests.Application.AuthorizationModels;

[Trait("Category", "ApplicationTests")]
[Trait("Feature", "AuthorizationModels")]
public sealed class AuthorizationModelValidatorTests
{
    private readonly AuthorizationModelValidator _validator = new();

    [Fact]
    public void Validate_WithSupportedRewriteFeatures_ReturnsDeterministicSummary()
    {
        var request = new ValidateAuthorizationModelRequestDto(
            "1.1",
            """
            type user

            type group
              relations
                define member: [user]

            type document
              relations
                define owner: [user]
                define viewer: owner or member from parent
                define editor: owner and viewer
                define restricted: viewer but not blocked
            """);

        var result = _validator.Validate(request);

        Assert.True(result.Valid);
        Assert.Empty(result.Errors);
        Assert.Equal(3, result.Summary.TypeCount);
        Assert.Equal(5, result.Summary.RelationCount);
        Assert.Equal(2, result.Summary.DirectRelationCount);
        Assert.True(result.Summary.HasUnion);
        Assert.True(result.Summary.HasIntersection);
        Assert.True(result.Summary.HasExclusion);
        Assert.True(result.Summary.HasTupleToUserset);
    }

    [Fact]
    public void Validate_WithInvalidStructure_PreservesStableIssueCodesAndLineNumbers()
    {
        var request = new ValidateAuthorizationModelRequestDto(
            "1.1",
            """
            define orphan: [user]
            type document
              relations
                define viewer: [user]
                define viewer: owner
            """);

        var result = _validator.Validate(request);

        Assert.False(result.Valid);
        Assert.Contains(result.Errors, issue => issue.Code == "RELATION_OUTSIDE_TYPE" && issue.Line == 1);
        Assert.Contains(result.Errors, issue => issue.Code == "DUPLICATE_RELATION" && issue.Line == 5);
    }

    [Fact]
    public void Validate_WhenCancellationIsRequested_StopsBeforeParsing()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            _validator.Validate(new ValidateAuthorizationModelRequestDto("1.1", "type user"), cancellation.Token));
    }
}
