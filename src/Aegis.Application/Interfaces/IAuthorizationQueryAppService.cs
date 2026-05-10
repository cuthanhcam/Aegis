using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;
using Aegis.Contracts.Query;

namespace Aegis.Application.Interfaces;

public interface IAuthorizationQueryAppService
{
    Task<ListUsersResponseDto> ListUsersAsync(string storeId, ListUsersRequestDto request, CancellationToken cancellationToken = default);

    Task<AegisCompatListUsersResponseDto> ListUsersAegisCompatAsync(string storeId, AegisCompatListUsersRequestDto request, CancellationToken cancellationToken = default);

    Task<ListObjectsResponseDto> ListObjectsAsync(string storeId, ListObjectsRequestDto request, CancellationToken cancellationToken = default);

    Task<AegisCompatListObjectsResponseDto> ListObjectsAegisCompatAsync(string storeId, AegisCompatListObjectsRequestDto request, CancellationToken cancellationToken = default);

    IAsyncEnumerable<AegisCompatStreamedListObjectsResponseDto> StreamedListObjectsAegisCompatAsync(string storeId, AegisCompatListObjectsRequestDto request, CancellationToken cancellationToken = default);

    Task<ExpandNodeDto> ExpandAsync(string storeId, ExpandRequestDto request, CancellationToken cancellationToken = default);

    Task<AegisCompatExpandResponseDto> ExpandAegisCompatAsync(string storeId, AegisCompatExpandRequestDto request, CancellationToken cancellationToken = default);

    Task<string> ResolveAuthorizationModelIdForStoreAsync(string storeId, string? authorizationModelId, CancellationToken cancellationToken = default);
}
