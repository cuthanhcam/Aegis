using Aegis.Contracts.Compatibility;
using System.Text.Json;

namespace Aegis.Application.Features.Query
{
    internal static partial class AuthorizationQueryHelper
    {
        public static bool TryParseTupleToUsersetToken(string token, out string computedRelation, out string tuplesetRelation)
        {
            computedRelation = string.Empty;
            tuplesetRelation = string.Empty;

            var marker = token.IndexOf(" from ", StringComparison.OrdinalIgnoreCase);
            if (marker <= 0 || marker == token.Length - 6)
            {
                return false;
            }

            computedRelation = token[..marker].Trim();
            tuplesetRelation = token[(marker + 6)..].Trim();
            return !string.IsNullOrWhiteSpace(computedRelation) && !string.IsNullOrWhiteSpace(tuplesetRelation);
        }

        public static bool TryParseUsersetToken(string token, out string? typeName, out string relation)
        {
            typeName = null;
            relation = string.Empty;

            var markerIndex = token.IndexOf('#');
            if (markerIndex <= 0 || markerIndex == token.Length - 1)
            {
                return false;
            }

            var left = token[..markerIndex].Trim();
            relation = token[(markerIndex + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(relation))
            {
                return false;
            }

            if (!string.Equals(left, "*", StringComparison.Ordinal))
            {
                typeName = left;
            }

            return true;
        }

        public static bool TryParseUserset(string value, out string objectRef, out string relation)
        {
            objectRef = string.Empty;
            relation = string.Empty;

            var marker = value.IndexOf('#');
            if (marker <= 0 || marker == value.Length - 1)
            {
                return false;
            }

            var obj = value[..marker];
            var rel = value[(marker + 1)..];
            if (!Aegis.Domain.ValueObjects.ObjectId.TryCreate(obj, out _) || !Aegis.Domain.ValueObjects.RelationName.TryCreate(rel, out _))
            {
                return false;
            }

            objectRef = obj;
            relation = rel;
            return true;
        }

        public static string GetTypeName(string objectRef)
        {
            var separatorIndex = objectRef.IndexOf(':');
            if (separatorIndex <= 0)
            {
                return objectRef;
            }

            return objectRef[..separatorIndex];
        }

        public static bool TryParseConditionedToken(string token, out string baseToken, out string conditionName)
        {
            baseToken = token;
            conditionName = string.Empty;

            var marker = token.LastIndexOf(" with ", StringComparison.OrdinalIgnoreCase);
            if (marker <= 0 || marker >= token.Length - 6)
            {
                return false;
            }

            baseToken = token[..marker].Trim();
            conditionName = token[(marker + 6)..].Trim();
            return !string.IsNullOrWhiteSpace(baseToken) && !string.IsNullOrWhiteSpace(conditionName);
        }

        public static bool EvaluateCondition(string conditionName, IReadOnlyDictionary<string, JsonElement>? context)
        {
            if (context is null)
            {
                return false;
            }

            if (!context.TryGetValue(conditionName, out var value))
            {
                return false;
            }

            if (value.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (value.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                var conditionValue = value.GetString();
                if (bool.TryParse(conditionValue, out var parsedBoolean))
                {
                    return parsedBoolean;
                }
            }

            return false;
        }

        public static AegisCompatObjectRefDto ParseAegisCompatObject(string value)
        {
            if (TryParseUserset(value, out var objectRef, out var relation))
            {
                var idxUserset = objectRef.IndexOf(':');
                if (idxUserset > 0 && idxUserset < objectRef.Length - 1)
                {
                    return new AegisCompatObjectRefDto(objectRef[..idxUserset], objectRef[(idxUserset + 1)..], relation);
                }

                return new AegisCompatObjectRefDto("user", objectRef, relation);
            }

            var idx = value.IndexOf(':');
            if (idx <= 0 || idx == value.Length - 1)
            {
                return new AegisCompatObjectRefDto("user", value);
            }

            return new AegisCompatObjectRefDto(value[..idx], value[(idx + 1)..]);
        }
    }
}
