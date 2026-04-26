using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Query;
using System.Text.Json;

namespace Aegis.Application.Features.Query
{
    public sealed class ListUsersQueryUseCase
    {
        private readonly ResolveQueryModelContextUseCase _resolveQueryModelContextUseCase;
        private readonly QueryAllowTuplesUseCase _queryAllowTuplesUseCase;

        public ListUsersQueryUseCase(
            ResolveQueryModelContextUseCase resolveQueryModelContextUseCase,
            QueryAllowTuplesUseCase queryAllowTuplesUseCase)
        {
            _resolveQueryModelContextUseCase = resolveQueryModelContextUseCase;
            _queryAllowTuplesUseCase = queryAllowTuplesUseCase;
        }

        public async Task<ListUsersResponseDto> ExecuteAsync(
            string storeId,
            ListUsersRequestDto request,
            CancellationToken cancellationToken = default)
        {
            AuthorizationQueryHelper.ValidateObjectAndRelation(request.Object, request.Relation);
            _ = AuthorizationQueryHelper.ParseConsistency(request.Consistency);

            var modelContext = await _resolveQueryModelContextUseCase.ExecuteAsync(storeId, request.AuthorizationModelId, cancellationToken);
            _resolveQueryModelContextUseCase.ValidateTypeAndRelationExists(
                AuthorizationQueryHelper.GetTypeName(request.Object),
                request.Relation,
                modelContext.RelationIndex);

            var contextualTuples = AuthorizationQueryHelper.ParseContextualTuples(request.ContextualTuples);
            var users = await ResolveUsersAsync(
                storeId,
                request.Object,
                request.Relation,
                modelContext.Rules,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                contextualTuples,
                request.Context,
                cancellationToken);

            return new ListUsersResponseDto(users.OrderBy(x => x).ToList());
        }

        private async Task<HashSet<string>> ResolveUsersAsync(
            string storeId,
            string objectRef,
            string relation,
            Dictionary<(string TypeName, string Relation), IReadOnlyList<QueryRewriteTerm>> rules,
            ISet<string> visited,
            IReadOnlyList<RelationshipTuple>? contextualTuples,
            IReadOnlyDictionary<string, JsonElement>? requestContext,
            CancellationToken cancellationToken)
        {
            var visitedKey = $"{objectRef}|{relation}";
            if (!visited.Add(visitedKey))
            {
                return [];
            }

            try
            {
                var users = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var tuples = await _queryAllowTuplesUseCase.ExecuteAsync(
                    storeId,
                    null,
                    relation,
                    new ObjectRef(objectRef),
                    contextualTuples,
                    cancellationToken);

                var directUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var usersetTuples = new List<RelationshipTuple>();

                foreach (var tuple in tuples)
                {
                    var subject = tuple.Subject.Value;
                    if (subject.StartsWith("user:", StringComparison.OrdinalIgnoreCase))
                    {
                        directUsers.Add(subject);
                        continue;
                    }

                    usersetTuples.Add(tuple);

                    if (AuthorizationQueryHelper.TryParseUserset(subject, out var usersetObject, out var usersetRelation))
                    {
                        var nested = await ResolveUsersAsync(
                            storeId,
                            usersetObject,
                            usersetRelation,
                            rules,
                            visited,
                            contextualTuples,
                            requestContext,
                            cancellationToken);
                        users.UnionWith(nested);
                    }
                }

                var objectType = AuthorizationQueryHelper.GetTypeName(objectRef);
                if (rules.TryGetValue((objectType, relation), out var terms))
                {
                    foreach (var term in terms)
                    {
                        var includeSets = new List<HashSet<string>>();
                        foreach (var include in term.Includes)
                        {
                            includeSets.Add(await ResolveUsersForTokenAsync(
                                storeId,
                                objectRef,
                                include,
                                rules,
                                visited,
                                directUsers,
                                usersetTuples,
                                contextualTuples,
                                requestContext,
                                cancellationToken));
                        }

                        if (includeSets.Count == 0)
                        {
                            continue;
                        }

                        var termUsers = new HashSet<string>(includeSets[0], StringComparer.OrdinalIgnoreCase);
                        for (var i = 1; i < includeSets.Count; i++)
                        {
                            termUsers.IntersectWith(includeSets[i]);
                        }

                        foreach (var excludeClause in term.ExcludeClauses)
                        {
                            var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            foreach (var excludeToken in excludeClause)
                            {
                                var resolved = await ResolveUsersForTokenAsync(
                                    storeId,
                                    objectRef,
                                    excludeToken,
                                    rules,
                                    visited,
                                    directUsers,
                                    usersetTuples,
                                    contextualTuples,
                                    requestContext,
                                    cancellationToken);

                                if (excluded.Count == 0)
                                {
                                    excluded.UnionWith(resolved);
                                }
                                else
                                {
                                    excluded.IntersectWith(resolved);
                                }
                            }

                            termUsers.ExceptWith(excluded);
                        }

                        users.UnionWith(termUsers);
                    }
                }
                else
                {
                    users.UnionWith(directUsers);
                }

                return users;
            }
            finally
            {
                visited.Remove(visitedKey);
            }
        }

        private async Task<HashSet<string>> ResolveUsersForTokenAsync(
            string storeId,
            string objectRef,
            string token,
            Dictionary<(string TypeName, string Relation), IReadOnlyList<QueryRewriteTerm>> rules,
            ISet<string> visited,
            IReadOnlySet<string> directUsers,
            IReadOnlyList<RelationshipTuple> usersetTuples,
            IReadOnlyList<RelationshipTuple>? contextualTuples,
            IReadOnlyDictionary<string, JsonElement>? requestContext,
            CancellationToken cancellationToken)
        {
            if (AuthorizationQueryHelper.TryParseConditionedToken(token, out var baseToken, out var conditionName))
            {
                if (!AuthorizationQueryHelper.EvaluateCondition(conditionName, requestContext))
                {
                    return [];
                }

                token = baseToken;
            }

            if (token.Equals("this", StringComparison.OrdinalIgnoreCase) || token.StartsWith("user", StringComparison.OrdinalIgnoreCase))
            {
                return new HashSet<string>(directUsers, StringComparer.OrdinalIgnoreCase);
            }

            if (AuthorizationQueryHelper.TryParseTupleToUsersetToken(token, out var computedRelation, out var tuplesetRelation))
            {
                var users = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var tuples = await _queryAllowTuplesUseCase.ExecuteAsync(
                    storeId,
                    null,
                    tuplesetRelation,
                    new ObjectRef(objectRef),
                    contextualTuples,
                    cancellationToken);

                foreach (var tuple in tuples)
                {
                    var nested = await ResolveUsersAsync(
                        storeId,
                        tuple.Subject.Value,
                        computedRelation,
                        rules,
                        visited,
                        contextualTuples,
                        requestContext,
                        cancellationToken);
                    users.UnionWith(nested);
                }

                return users;
            }

            if (token.Contains('#'))
            {
                if (!AuthorizationQueryHelper.TryParseUsersetToken(token, out var typeName, out var usersetRelation))
                {
                    return [];
                }

                var users = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var tuple in usersetTuples)
                {
                    if (!AuthorizationQueryHelper.TryParseUserset(tuple.Subject.Value, out var usersetObject, out var tupleUsersetRelation))
                    {
                        continue;
                    }

                    if (!string.Equals(tupleUsersetRelation, usersetRelation, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(typeName)
                        && !string.Equals(AuthorizationQueryHelper.GetTypeName(usersetObject), typeName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var nested = await ResolveUsersAsync(
                        storeId,
                        usersetObject,
                        usersetRelation,
                        rules,
                        visited,
                        contextualTuples,
                        requestContext,
                        cancellationToken);
                    users.UnionWith(nested);
                }

                return users;
            }

            return await ResolveUsersAsync(
                storeId,
                objectRef,
                token,
                rules,
                visited,
                contextualTuples,
                requestContext,
                cancellationToken);
        }
    }
}
