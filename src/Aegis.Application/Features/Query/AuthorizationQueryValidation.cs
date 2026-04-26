using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Common;
using Aegis.Domain.ValueObjects;

namespace Aegis.Application.Features.Query
{
    internal static partial class AuthorizationQueryHelper
    {
        public static bool IsValidUsersetRef(string value)
        {
            var hasValidSubjectId = SubjectId.TryCreate(value, out _);
            var hasUsersetSeparator = value.Contains('#', StringComparison.Ordinal);

            return hasValidSubjectId && hasUsersetSeparator;
        }

        public static void ValidateListObjectsInput(string user, string relation, string type)
        {
            if (!ObjectId.TryCreate(user, out _) || !RelationName.TryCreate(relation, out _) || !ResourceTypeName.TryCreate(type, out _))
            {
                throw new ArgumentException("Invalid input. user must be <type>:<id>, relation and type are required.");
            }
        }

        public static void ValidateObjectAndRelation(string objectRef, string relation)
        {
            if (!RelationName.TryCreate(relation, out _) || !ObjectId.TryCreate(objectRef, out _))
            {
                throw new ArgumentException("Invalid input. object must be <type>:<id> and relation non-empty.");
            }
        }

        public static void ValidateCheckInput(string subject, string relation, string objectRef)
        {
            if (!SubjectId.TryCreate(subject, out _) || !RelationName.TryCreate(relation, out _) || !ObjectId.TryCreate(objectRef, out _))
            {
                throw new ArgumentException("Invalid tuple format. Expected subject/object as <type>:<id> and non-empty relation.");
            }
        }

        public static ConsistencyPreference ParseConsistency(string? consistency)
        {
            if (string.IsNullOrWhiteSpace(consistency))
            {
                return ConsistencyPreference.MinimizeLatency;
            }

            var normalizedConsistency = consistency.Trim().ToUpperInvariant();
            if (string.Equals(normalizedConsistency, "MINIMIZE_LATENCY", StringComparison.Ordinal))
            {
                return ConsistencyPreference.MinimizeLatency;
            }

            if (string.Equals(normalizedConsistency, "HIGHER_CONSISTENCY", StringComparison.Ordinal))
            {
                return ConsistencyPreference.HigherConsistency;
            }

            throw new ArgumentException("consistency must be MINIMIZE_LATENCY or HIGHER_CONSISTENCY.");
        }

        public static IReadOnlyList<RelationshipTuple>? ParseContextualTuples(IReadOnlyList<ContextualTupleDto>? contextualTuples)
        {
            if (contextualTuples is null || contextualTuples.Count == 0)
            {
                return null;
            }

            var parsed = new List<RelationshipTuple>(contextualTuples.Count);
            foreach (var tuple in contextualTuples)
            {
                if (!SubjectId.TryCreate(tuple.Subject, out _)
                    || !RelationName.TryCreate(tuple.Relation, out _)
                    || !ObjectId.TryCreate(tuple.Object, out _))
                {
                    throw new ArgumentException("Invalid contextual tuple format.");
                }

                if (!tuple.Effect.Equals("allow", StringComparison.OrdinalIgnoreCase)
                    && !tuple.Effect.Equals("deny", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("contextual tuple effect must be allow or deny.");
                }

                var effect = tuple.Effect.Equals("deny", StringComparison.OrdinalIgnoreCase)
                    ? RelationshipEffect.Deny
                    : RelationshipEffect.Allow;
                parsed.Add(new RelationshipTuple(new Subject(tuple.Subject), tuple.Relation, new ObjectRef(tuple.Object), effect, DateTimeOffset.UtcNow));
            }

            return parsed;
        }
    }
}
