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
}
