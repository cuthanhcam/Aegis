namespace Aegis.Contracts.Administration
{
    /// <summary>
    /// Request payload for creating a new authorization model.
    /// </summary>
    public sealed record CreateAuthorizationModelRequestDto(
        string SchemaVersion,
        string Model);

    /// <summary>
    /// Read model for an authorization model stored in a tenant store.
    /// </summary>
    public sealed record AuthorizationModelDto(
        string Id,
        string StoreId,
        string SchemaVersion,
        string Model,
        DateTimeOffset CreatedAt);

    /// <summary>
    /// Validation request payload for an authorization model draft.
    /// </summary>
    public sealed record ValidateAuthorizationModelRequestDto(
        string SchemaVersion,
        string Model);

    /// <summary>
    /// One validation issue for an authorization model draft.
    /// </summary>
    public sealed record AuthorizationModelValidationIssueDto(
        string Code,
        string Message,
        int? Line = null);

    /// <summary>
    /// Parsed model summary returned by validation.
    /// </summary>
    public sealed record AuthorizationModelValidationSummaryDto(
        int TypeCount,
        int RelationCount,
        int DirectRelationCount,
        bool HasUnion,
        bool HasIntersection,
        bool HasExclusion,
        bool HasTupleToUserset);

    /// <summary>
    /// Validation result for an authorization model draft.
    /// </summary>
    public sealed record AuthorizationModelValidationResultDto(
        bool Valid,
        AuthorizationModelValidationSummaryDto Summary,
        IReadOnlyList<AuthorizationModelValidationIssueDto> Errors,
        IReadOnlyList<AuthorizationModelValidationIssueDto> Warnings);
}
