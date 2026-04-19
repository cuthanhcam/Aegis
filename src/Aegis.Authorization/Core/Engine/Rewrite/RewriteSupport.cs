using System.Text.Json;

namespace Aegis.Authorization.Core.Engine.Rewrite
{
    /// <summary>
    /// Helper utilities for parsing and evaluating rewrite tokens.
    /// </summary>
    internal static class RewriteSupport
    {
        /// <summary>
        /// Parses a userset token in the form <c>type#relation</c> or <c>*#relation</c>.
        /// </summary>
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

        /// <summary>
        /// Parses a userset subject reference in the form <c>object#relation</c>.
        /// </summary>
        public static bool TryParseUsersetRef(string userset, out string objectRef, out string relation)
        {
            objectRef = string.Empty;
            relation = string.Empty;

            var marker = userset.IndexOf('#');
            if (marker <= 0 || marker == userset.Length - 1)
            {
                return false;
            }

            objectRef = userset[..marker];
            relation = userset[(marker + 1)..];
            return true;
        }

        /// <summary>
        /// Parses tuple-to-userset tokens in the form <c>computed from tupleset</c>.
        /// </summary>
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

        /// <summary>
        /// Parses conditioned tokens in the form <c>token with conditionName</c>.
        /// </summary>
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

        /// <summary>
        /// Evaluates a boolean condition value from check context.
        /// </summary>
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

            return value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
                _ => false,
            };
        }

        /// <summary>
        /// Determines whether the token is a plain type token.
        /// </summary>
        public static bool IsTypeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)
                || token.Contains('#', StringComparison.Ordinal)
                || token.Contains(':', StringComparison.Ordinal)
                || token.Contains(" from ", StringComparison.OrdinalIgnoreCase)
                || token.Equals("this", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether subject reference type matches a type token.
        /// </summary>
        public static bool SubjectMatchesTypeToken(string subjectRef, string token)
        {
            var subjectType = GetTypeName(subjectRef);
            return string.Equals(subjectType, token, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Extracts the type segment from a reference in <c>type:id</c> format.
        /// </summary>
        public static string GetTypeName(string refValue)
        {
            var idx = refValue.IndexOf(':');
            return idx > 0 ? refValue[..idx] : refValue;
        }
    }
}
