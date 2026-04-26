using Aegis.Application.Features.Query;
using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;
using Aegis.Contracts.Query;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace Aegis.Application.Services
{
    public sealed class AuthorizationQueryAppService
    {
        private readonly ResolveQueryModelContextUseCase _resolveQueryModelContextUseCase;
        private readonly ListUsersQueryUseCase _listUsersQueryUseCase;
        private readonly ListObjectsQueryUseCase _listObjectsQueryUseCase;
        private readonly ExpandQueryUseCase _expandQueryUseCase;
        private readonly ResolveUsersetEntriesFromRelationFiltersUseCase _resolveUsersetEntriesFromRelationFiltersUseCase;

        [ActivatorUtilitiesConstructor]
        public AuthorizationQueryAppService(
            ResolveQueryModelContextUseCase resolveQueryModelContextUseCase,
            ListUsersQueryUseCase listUsersQueryUseCase,
            ListObjectsQueryUseCase listObjectsQueryUseCase,
            ExpandQueryUseCase expandQueryUseCase,
            ResolveUsersetEntriesFromRelationFiltersUseCase resolveUsersetEntriesFromRelationFiltersUseCase)
        {
            _resolveQueryModelContextUseCase = resolveQueryModelContextUseCase;
            _listUsersQueryUseCase = listUsersQueryUseCase;
            _listObjectsQueryUseCase = listObjectsQueryUseCase;
            _expandQueryUseCase = expandQueryUseCase;
            _resolveUsersetEntriesFromRelationFiltersUseCase = resolveUsersetEntriesFromRelationFiltersUseCase;
        }

        public static AuthorizationQueryAppService CreateForTests(
            IStoreRegistry storeRegistry,
            IAuthorizationModelRegistry authorizationModelRegistry,
            IRelationshipStore relationshipStore,
            IAuthorizationEngine authorizationEngine)
        {
            return new AuthorizationQueryAppService(
                new ResolveQueryModelContextUseCase(storeRegistry, authorizationModelRegistry),
                new ListUsersQueryUseCase(
                    new ResolveQueryModelContextUseCase(storeRegistry, authorizationModelRegistry),
                    new QueryAllowTuplesUseCase(relationshipStore)),
                new ListObjectsQueryUseCase(
                    new ResolveQueryModelContextUseCase(storeRegistry, authorizationModelRegistry),
                    new QueryAllowTuplesUseCase(relationshipStore),
                    authorizationEngine),
                new ExpandQueryUseCase(
                    new ResolveQueryModelContextUseCase(storeRegistry, authorizationModelRegistry),
                    new QueryAllowTuplesUseCase(relationshipStore)),
                new ResolveUsersetEntriesFromRelationFiltersUseCase(
                    new QueryAllowTuplesUseCase(relationshipStore)));
        }

        public async Task<ListUsersResponseDto> ListUsersAsync(string storeId, ListUsersRequestDto request, CancellationToken cancellationToken = default)
        {
            return await _listUsersQueryUseCase.ExecuteAsync(storeId, request, cancellationToken);
        }

        public async Task<AegisCompatListUsersResponseDto> ListUsersAegisCompatAsync(
            string storeId,
            AegisCompatListUsersRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var result = await ListUsersAsync(
                storeId,
                new ListUsersRequestDto(
                    request.Relation,
                    AuthorizationQueryHelper.ToObjectRef(request.Object),
                    request.Consistency,
                    AuthorizationQueryHelper.ToContextualTuples(request.ContextualTuples),
                    request.AuthorizationModelId,
                    request.Context),
                cancellationToken);

            var users = result.Users
                .Select(AuthorizationQueryHelper.ParseAegisCompatObject)
                .Select(x => new AegisCompatUserEntryDto(x))
                .ToList();

            users = AuthorizationQueryHelper.ApplyUserFilters(users, request.UserFilters);

            var usersetEntries = await _resolveUsersetEntriesFromRelationFiltersUseCase.ExecuteAsync(storeId, request, cancellationToken);
            if (usersetEntries.Count > 0)
            {
                users.AddRange(usersetEntries);
                users = users
                    .GroupBy(x => $"{x.Object.Type}|{x.Object.Id}|{x.Object.Relation}", StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .ToList();
            }

            return new AegisCompatListUsersResponseDto(users);
        }

        public async Task<ListObjectsResponseDto> ListObjectsAsync(string storeId, ListObjectsRequestDto request, CancellationToken cancellationToken = default)
        {
            return await _listObjectsQueryUseCase.ExecuteAsync(storeId, request, cancellationToken);
        }

        public async Task<AegisCompatListObjectsResponseDto> ListObjectsAegisCompatAsync(
            string storeId,
            AegisCompatListObjectsRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var result = await ListObjectsAsync(
                storeId,
                new ListObjectsRequestDto(
                    request.User,
                    request.Relation,
                    request.Type,
                    request.Consistency,
                    AuthorizationQueryHelper.ToContextualTuples(request.ContextualTuples),
                    request.AuthorizationModelId,
                    request.Context),
                cancellationToken);

            return new AegisCompatListObjectsResponseDto(result.Objects);
        }

        public async IAsyncEnumerable<AegisCompatStreamedListObjectsResponseDto> StreamedListObjectsAegisCompatAsync(
            string storeId,
            AegisCompatListObjectsRequestDto request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var mappedRequest = new ListObjectsRequestDto(
                request.User,
                request.Relation,
                request.Type,
                request.Consistency,
                AuthorizationQueryHelper.ToContextualTuples(request.ContextualTuples),
                request.AuthorizationModelId,
                request.Context);

            await foreach (var obj in _listObjectsQueryUseCase.StreamObjectsAsync(storeId, mappedRequest, cancellationToken))
            {
                yield return new AegisCompatStreamedListObjectsResponseDto(obj);
            }
        }

        public async Task<ExpandNodeDto> ExpandAsync(string storeId, ExpandRequestDto request, CancellationToken cancellationToken = default)
        {
            return await _expandQueryUseCase.ExecuteAsync(storeId, request, cancellationToken);
        }

        public async Task<AegisCompatExpandResponseDto> ExpandAegisCompatAsync(
            string storeId,
            AegisCompatExpandRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var tree = await ExpandAsync(
                storeId,
                new ExpandRequestDto(
                    request.TupleKey.Relation,
                    request.TupleKey.Object,
                    request.Consistency,
                    AuthorizationQueryHelper.ToContextualTuples(request.ContextualTuples),
                    request.AuthorizationModelId,
                    request.Context),
                cancellationToken);

            return new AegisCompatExpandResponseDto(tree);
        }

        public async Task<string> ResolveAuthorizationModelIdForStoreAsync(
            string storeId,
            string? authorizationModelId,
            CancellationToken cancellationToken = default)
        {
            var context = await _resolveQueryModelContextUseCase.ExecuteAsync(storeId, authorizationModelId, cancellationToken);
            return context.AuthorizationModelId;
        }
    }
}
