using Aegis.Contracts.Compatibility;

namespace Aegis.Application.Interfaces;

public sealed record AssertionSetSnapshot(
    string StoreId,
    string AuthorizationModelId,
    long Revision,
    IReadOnlyList<AegisCompatAssertionDto> Assertions);

public sealed class AssertionSetCapacityExceededException(int maximum)
    : Exception($"Assertion set exceeds max allowed items of {maximum}.")
{
    public int Maximum { get; } = maximum;
}

public interface IAssertionRepository
{
    Task<AssertionSetSnapshot> ReadAsync(
        string storeId,
        string authorizationModelId,
        CancellationToken cancellationToken = default);

    Task<AssertionSetSnapshot> ReplaceAsync(
        string storeId,
        string authorizationModelId,
        IReadOnlyList<AegisCompatAssertionDto> assertions,
        CancellationToken cancellationToken = default);

    Task<AssertionSetSnapshot> AppendDistinctAsync(
        string storeId,
        string authorizationModelId,
        IReadOnlyList<AegisCompatAssertionDto> assertions,
        int maximum,
        CancellationToken cancellationToken = default);

    Task PurgeStoreAsync(string storeId, CancellationToken cancellationToken = default);
}
