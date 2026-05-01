using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;

namespace Aegis.Application.Features.Query
{
    public sealed class QueryAllowTuplesUseCase
    {
        private readonly IRelationshipStore _relationshipStore;

        public QueryAllowTuplesUseCase(IRelationshipStore relationshipStore)
        {
            _relationshipStore = relationshipStore ?? throw new ArgumentNullException(nameof(relationshipStore));
        }

        public async Task<IReadOnlyList<RelationshipTuple>> ExecuteAsync(
            string storeId,
            Subject? subject,
            string? relation,
            ObjectRef? @object,
            IReadOnlyList<RelationshipTuple>? contextualTuples,
            CancellationToken cancellationToken)
        {
            var persisted = await _relationshipStore.QueryAsync(storeId, subject, relation, @object, RelationshipEffect.Allow, cancellationToken);
            if (contextualTuples is null || contextualTuples.Count == 0)
            {
                return persisted;
            }

            var contextualMatches = contextualTuples.Where(tuple =>
                (subject is null || string.Equals(tuple.Subject.Value, subject.Value, StringComparison.OrdinalIgnoreCase)) &&
                (relation is null || string.Equals(tuple.Relation, relation, StringComparison.OrdinalIgnoreCase)) &&
                (@object is null || string.Equals(tuple.Object.Value, @object.Value, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (contextualMatches.Count == 0)
            {
                return persisted;
            }

            static string KeyOf(RelationshipTuple tuple)
                => $"{tuple.Subject.Value}|{tuple.Relation}|{tuple.Object.Value}".ToUpperInvariant();

            var denyKeys = contextualMatches
                .Where(tuple => tuple.Effect == RelationshipEffect.Deny)
                .Select(KeyOf)
                .ToHashSet(StringComparer.Ordinal);

            var merged = new List<RelationshipTuple>(persisted.Count + contextualMatches.Count);
            merged.AddRange(persisted);
            merged.AddRange(contextualMatches.Where(tuple => tuple.Effect == RelationshipEffect.Allow));

            return merged
                .Where(tuple => !denyKeys.Contains(KeyOf(tuple)))
                .GroupBy(KeyOf, StringComparer.Ordinal)
                .Select(group => group.OrderByDescending(x => x.CreatedAt).First())
                .ToList();
        }
    }
}
