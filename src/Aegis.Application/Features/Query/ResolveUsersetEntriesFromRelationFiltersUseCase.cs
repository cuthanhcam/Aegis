using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Compatibility;

namespace Aegis.Application.Features.Query
{
    public sealed class ResolveUsersetEntriesFromRelationFiltersUseCase
    {
        private readonly QueryAllowTuplesUseCase _queryAllowTuplesUseCase;

        public ResolveUsersetEntriesFromRelationFiltersUseCase(QueryAllowTuplesUseCase queryAllowTuplesUseCase)
        {
            _queryAllowTuplesUseCase = queryAllowTuplesUseCase;
        }

        public async Task<List<AegisCompatUserEntryDto>> ExecuteAsync(
            string storeId,
            AegisCompatListUsersRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.UserFilters is null || request.UserFilters.Count == 0)
            {
                return [];
            }

            var relationFilters = request.UserFilters
                .Where(x => !string.IsNullOrWhiteSpace(x.Relation))
                .ToList();

            if (relationFilters.Count == 0)
            {
                return [];
            }

            var objectRef = AuthorizationQueryHelper.ToObjectRef(request.Object);
            var contextualTuples = AuthorizationQueryHelper.ParseContextualTuples(
                AuthorizationQueryHelper.ToContextualTuples(request.ContextualTuples));

            var tuples = await _queryAllowTuplesUseCase.ExecuteAsync(
                storeId,
                null,
                request.Relation,
                new ObjectRef(objectRef),
                contextualTuples,
                cancellationToken);

            var usersets = new List<AegisCompatUserEntryDto>();
            foreach (var tuple in tuples)
            {
                if (!AuthorizationQueryHelper.TryParseUserset(tuple.Subject.Value, out var usersetObject, out var usersetRelation))
                {
                    continue;
                }

                var usersetType = AuthorizationQueryHelper.GetTypeName(usersetObject);
                var usersetId = usersetObject[(usersetType.Length + 1)..];
                if (relationFilters.Any(filter =>
                    string.Equals(filter.Type, usersetType, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(filter.Relation, usersetRelation, StringComparison.OrdinalIgnoreCase)))
                {
                    usersets.Add(new AegisCompatUserEntryDto(new AegisCompatObjectRefDto(usersetType, usersetId, usersetRelation)));
                }
            }

            return usersets;
        }
    }
}
