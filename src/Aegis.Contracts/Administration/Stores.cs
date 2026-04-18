namespace Aegis.Contracts.Administration
{
    /// <summary>
    /// Request payload for creating a store.
    /// </summary>
    public sealed record CreateStoreRequestDto(string Name);

    /// <summary>
    /// Read model representing a tenant store and optional aggregate counts.
    /// </summary>
    public sealed record StoreDto(
        string Id,
        string Name,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        int? ModelCount = null,
        int? RelationshipCount = null);
}
