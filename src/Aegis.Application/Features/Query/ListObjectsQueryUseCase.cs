using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Query;
using System.Runtime.CompilerServices;

namespace Aegis.Application.Features.Query
{
    public sealed class ListObjectsQueryUseCase
    {
        private readonly ResolveQueryModelContextUseCase _resolveQueryModelContextUseCase;
        private readonly QueryAllowTuplesUseCase _queryAllowTuplesUseCase;
        private readonly IAuthorizationEngine _authorizationEngine;

        public ListObjectsQueryUseCase(
            ResolveQueryModelContextUseCase resolveQueryModelContextUseCase,
            QueryAllowTuplesUseCase queryAllowTuplesUseCase,
            IAuthorizationEngine authorizationEngine)
        {
            _resolveQueryModelContextUseCase = resolveQueryModelContextUseCase;
            _queryAllowTuplesUseCase = queryAllowTuplesUseCase;
            _authorizationEngine = authorizationEngine;
        }

        public async Task<ListObjectsResponseDto> ExecuteAsync(
            string storeId,
            ListObjectsRequestDto request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(storeId, storeId, request, cancellationToken);
        }

        public async Task<ListObjectsResponseDto> ExecuteAsync(
            string tenantId,
            string storeId,
            ListObjectsRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var allowed = new List<string>();
            await foreach (var obj in StreamObjectsAsync(tenantId, storeId, request, cancellationToken))
            {
                allowed.Add(obj);
            }

            return new ListObjectsResponseDto(allowed.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList());
        }

        public async IAsyncEnumerable<string> StreamObjectsAsync(
            string storeId,
            ListObjectsRequestDto request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var obj in StreamObjectsAsync(storeId, storeId, request, cancellationToken))
            {
                yield return obj;
            }
        }

        public async IAsyncEnumerable<string> StreamObjectsAsync(
            string tenantId,
            string storeId,
            ListObjectsRequestDto request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            AuthorizationQueryHelper.ValidateListObjectsInput(request.User, request.Relation, request.Type);

            var consistency = AuthorizationQueryHelper.ParseConsistency(request.Consistency);
            var modelContext = await _resolveQueryModelContextUseCase.ExecuteAsync(tenantId, storeId, request.AuthorizationModelId, cancellationToken);
            _resolveQueryModelContextUseCase.ValidateTypeAndRelationExists(request.Type, request.Relation, modelContext.RelationIndex);
            var contextualTuples = AuthorizationQueryHelper.ParseContextualTuples(request.ContextualTuples);
            var candidateObjects = await CollectObjectCandidatesAsync(
                tenantId,
                storeId,
                request.User,
                request.Relation,
                request.Type,
                modelContext.Rules,
                contextualTuples,
                cancellationToken);

            var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var obj in candidateObjects.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var decision = await _authorizationEngine.CheckAsync(
                    new CheckRequest(tenantId, new Subject(request.User), request.Relation, new ObjectRef(obj), contextualTuples, consistency, request.AuthorizationModelId, request.Context, storeId),
                    includeTrace: false,
                    cancellationToken);

                if (decision.Allowed && emitted.Add(obj))
                {
                    yield return obj;
                }
            }
        }

        private async Task<HashSet<string>> CollectObjectCandidatesAsync(
            string tenantId,
            string storeId,
            string user,
            string relation,
            string objectType,
            Dictionary<(string TypeName, string Relation), IReadOnlyList<QueryRewriteTerm>> rules,
            IReadOnlyList<RelationshipTuple>? contextualTuples,
            CancellationToken cancellationToken)
        {
            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var directForUser = await _queryAllowTuplesUseCase.ExecuteAsync(tenantId, storeId, new Subject(user), relation, null, contextualTuples, cancellationToken);
            foreach (var tuple in directForUser)
            {
                if (AuthorizationQueryHelper.GetTypeName(tuple.Object.Value).Equals(objectType, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(tuple.Object.Value);
                }
            }

            var directWithUserset = await _queryAllowTuplesUseCase.ExecuteAsync(tenantId, storeId, null, relation, null, contextualTuples, cancellationToken);
            foreach (var tuple in directWithUserset.Where(x => AuthorizationQueryHelper.GetTypeName(x.Object.Value).Equals(objectType, StringComparison.OrdinalIgnoreCase)))
            {
                if (AuthorizationQueryHelper.IsValidUsersetRef(tuple.Subject.Value))
                {
                    candidates.Add(tuple.Object.Value);
                }
            }

            if (rules.TryGetValue((objectType, relation), out var terms))
            {
                foreach (var term in terms)
                {
                    foreach (var token in term.Includes)
                    {
                        if (AuthorizationQueryHelper.TryParseTupleToUsersetToken(token, out _, out var tuplesetRelation))
                        {
                            var tuples = await _queryAllowTuplesUseCase.ExecuteAsync(tenantId, storeId, null, tuplesetRelation, null, contextualTuples, cancellationToken);
                            foreach (var tuple in tuples)
                            {
                                if (AuthorizationQueryHelper.GetTypeName(tuple.Object.Value).Equals(objectType, StringComparison.OrdinalIgnoreCase))
                                {
                                    candidates.Add(tuple.Object.Value);
                                }
                            }

                            continue;
                        }

                        if (token.Contains('#') || token.Equals("this", StringComparison.OrdinalIgnoreCase) || token.StartsWith("user", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var computedTuples = await _queryAllowTuplesUseCase.ExecuteAsync(tenantId, storeId, new Subject(user), token, null, contextualTuples, cancellationToken);
                        foreach (var tuple in computedTuples)
                        {
                            if (AuthorizationQueryHelper.GetTypeName(tuple.Object.Value).Equals(objectType, StringComparison.OrdinalIgnoreCase))
                            {
                                candidates.Add(tuple.Object.Value);
                            }
                        }
                    }
                }
            }

            return candidates;
        }
    }
}
