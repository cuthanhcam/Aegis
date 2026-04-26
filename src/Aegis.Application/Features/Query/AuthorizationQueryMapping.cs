using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;

namespace Aegis.Application.Features.Query
{
    internal static partial class AuthorizationQueryHelper
    {
        public static IReadOnlyList<ContextualTupleDto>? ToContextualTuples(AegisCompatContextualTuplesDto? contextualTuples)
        {
            if (contextualTuples?.TupleKeys is null || contextualTuples.TupleKeys.Count == 0)
            {
                return null;
            }

            return contextualTuples.TupleKeys
                .Select(x => new ContextualTupleDto(x.User, x.Relation, x.Object, "allow"))
                .ToList();
        }

        public static List<AegisCompatUserEntryDto> ApplyUserFilters(
            List<AegisCompatUserEntryDto> users,
            IReadOnlyList<AegisCompatUserFilterDto>? userFilters)
        {
            if (userFilters is null)
            {
                return users;
            }

            if (userFilters.Count == 0)
            {
                return [];
            }

            var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var filter in userFilters)
            {
                if (string.IsNullOrWhiteSpace(filter.Type))
                {
                    throw new ArgumentException("user_filters.type is required.");
                }

                if (string.IsNullOrWhiteSpace(filter.Relation))
                {
                    allowedTypes.Add(filter.Type);
                }
            }

            if (allowedTypes.Count == 0)
            {
                return [];
            }

            return users
                .Where(x => x.Object.Relation is null && allowedTypes.Contains(x.Object.Type))
                .ToList();
        }

        public static string ToObjectRef(AegisCompatObjectRefDto objectRef)
        {
            var typeName = objectRef.Type;
            var objectId = objectRef.Id;

            return $"{typeName}:{objectId}";
        }
    }
}
