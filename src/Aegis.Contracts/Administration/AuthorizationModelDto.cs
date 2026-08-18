namespace Aegis.Contracts.Administration
{
    public static class AuthorizationModelLifecycleStates
    {
        public const string Draft = "Draft";
        public const string Validated = "Validated";
        public const string Published = "Published";
        public const string Archived = "Archived";
        public const string Deprecated = "Deprecated";
    }

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
        DateTimeOffset CreatedAt,
        string State = AuthorizationModelLifecycleStates.Draft,
        DateTimeOffset? PublishedAt = null,
        DateTimeOffset? ArchivedAt = null,
        string? SupersededBy = null,
        long Revision = 1);

    public sealed record PublishAuthorizationModelResponseDto(
        AuthorizationModelDto PublishedModel,
        string ActiveModelId,
        string Version);

    public sealed record RollbackAuthorizationModelResponseDto(
        AuthorizationModelDto ActiveModel,
        string ActiveModelId,
        string RolledBackFromModelId);

    public sealed record AuthorizationModelDiffDto(
        string LeftModelId,
        string RightModelId,
        IReadOnlyList<string> AddedTypes,
        IReadOnlyList<string> RemovedTypes,
        IReadOnlyList<string> ChangedTypes,
        IReadOnlyList<AuthorizationModelRelationDiffDto> AddedRelations,
        IReadOnlyList<AuthorizationModelRelationDiffDto> RemovedRelations,
        IReadOnlyList<AuthorizationModelRelationChangeDto> ChangedRelations,
        IReadOnlyList<string> BreakingChangeHints);

    public sealed record AuthorizationModelRelationDiffDto(
        string Type,
        string Relation,
        string Expression);

    public sealed record AuthorizationModelRelationChangeDto(
        string Type,
        string Relation,
        string LeftExpression,
        string RightExpression);

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
