using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Common;
using Aegis.Contracts.Query;

namespace Aegis.Application.Features.Query
{
    public sealed class ExpandQueryUseCase
    {
        private readonly ResolveQueryModelContextUseCase _resolveQueryModelContextUseCase;
        private readonly QueryAllowTuplesUseCase _queryAllowTuplesUseCase;

        public ExpandQueryUseCase(
            ResolveQueryModelContextUseCase resolveQueryModelContextUseCase,
            QueryAllowTuplesUseCase queryAllowTuplesUseCase)
        {
            _resolveQueryModelContextUseCase = resolveQueryModelContextUseCase;
            _queryAllowTuplesUseCase = queryAllowTuplesUseCase;
        }

        public async Task<ExpandNodeDto> ExecuteAsync(
            string storeId,
            ExpandRequestDto request,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(storeId, storeId, request, cancellationToken);
        }

        public async Task<ExpandNodeDto> ExecuteAsync(
            string tenantId,
            string storeId,
            ExpandRequestDto request,
            CancellationToken cancellationToken = default)
        {
            AuthorizationQueryHelper.ValidateObjectAndRelation(request.Object, request.Relation);
            _ = AuthorizationQueryHelper.ParseConsistency(request.Consistency);

            var modelContext = await _resolveQueryModelContextUseCase.ExecuteAsync(
                storeId,
                request.AuthorizationModelId,
                cancellationToken);

            _resolveQueryModelContextUseCase.ValidateTypeAndRelationExists(
                AuthorizationQueryHelper.GetTypeName(request.Object),
                request.Relation,
                modelContext.RelationIndex);

            var contextualTuples = AuthorizationQueryHelper.ParseContextualTuples(request.ContextualTuples);
            return await BuildExpandNodeAsync(
                tenantId,
                storeId,
                request.Object,
                request.Relation,
                modelContext.Rules,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                contextualTuples,
                cancellationToken);
        }

        private async Task<ExpandNodeDto> BuildExpandNodeAsync(
            string tenantId,
            string storeId,
            string objectRef,
            string relation,
            Dictionary<(string TypeName, string Relation), IReadOnlyList<QueryRewriteTerm>> rules,
            ISet<string> visited,
            IReadOnlyList<RelationshipTuple>? contextualTuples,
            CancellationToken cancellationToken)
        {
            var visitedKey = $"{objectRef}|{relation}";
            if (!visited.Add(visitedKey))
            {
                return new ExpandNodeDto($"{objectRef}#{relation}", "cycle", [], []);
            }

            try
            {
                var tuples = await _queryAllowTuplesUseCase.ExecuteAsync(
                    tenantId,
                    storeId,
                    null,
                    relation,
                    new ObjectRef(objectRef),
                    contextualTuples,
                    cancellationToken);

                var users = new List<string>();
                var children = new List<ExpandNodeDto>();

                foreach (var tuple in tuples)
                {
                    var subject = tuple.Subject.Value;
                    if (subject.StartsWith("user:", StringComparison.OrdinalIgnoreCase))
                    {
                        users.Add(subject);
                        continue;
                    }

                    if (AuthorizationQueryHelper.TryParseUserset(subject, out var usersetObject, out var usersetRelation))
                    {
                        var nested = await BuildExpandNodeAsync(
                            tenantId,
                            storeId,
                            usersetObject,
                            usersetRelation,
                            rules,
                            visited,
                            contextualTuples,
                            cancellationToken);
                        children.Add(new ExpandNodeDto(subject, "userset", nested.Users, nested.Children));
                    }
                    else
                    {
                        children.Add(new ExpandNodeDto(subject, "subject", [], []));
                    }
                }

                var objectType = AuthorizationQueryHelper.GetTypeName(objectRef);
                if (rules.TryGetValue((objectType, relation), out var terms))
                {
                    foreach (var term in terms)
                    {
                        foreach (var token in term.Includes)
                        {
                            if (token.Contains('#')
                                || token.StartsWith("user", StringComparison.OrdinalIgnoreCase)
                                || token.Equals("this", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            var nested = await BuildExpandNodeAsync(
                                tenantId,
                                storeId,
                                objectRef,
                                token,
                                rules,
                                visited,
                                contextualTuples,
                                cancellationToken);
                            children.Add(new ExpandNodeDto($"{objectRef}#{token}", "computed", nested.Users, nested.Children));
                        }

                        foreach (var excludeClause in term.ExcludeClauses)
                        {
                            foreach (var token in excludeClause)
                            {
                                if (token.Contains('#')
                                    || token.StartsWith("user", StringComparison.OrdinalIgnoreCase)
                                    || token.Equals("this", StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                var nested = await BuildExpandNodeAsync(
                                    tenantId,
                                    storeId,
                                    objectRef,
                                    token,
                                    rules,
                                    visited,
                                    contextualTuples,
                                    cancellationToken);
                                children.Add(new ExpandNodeDto($"{objectRef}#{token}", "exclude", nested.Users, nested.Children));
                            }
                        }
                    }
                }

                return new ExpandNodeDto(
                    $"{objectRef}#{relation}",
                    "relation",
                    users.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(),
                    children);
            }
            finally
            {
                visited.Remove(visitedKey);
            }
        }
    }
}
