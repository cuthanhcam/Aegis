using System.Text.Json;

namespace Aegis.Authorization.ABAC
{
    /// <summary>
    /// Evaluates boolean ABAC conditions from request context values.
    /// </summary>
    public static class ContextConditionEvaluator
    {
        /// <summary>
        /// Evaluates whether a named condition resolves to true in context.
        /// </summary>
        public static bool Evaluate(
            string conditionName,
            IReadOnlyDictionary<string, JsonElement>? context)
        {
            if (string.IsNullOrWhiteSpace(conditionName) || context is null)
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
                JsonValueKind.Number when value.TryGetInt64(out var number) => number != 0,
                JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
                _ => false,
            };
        }
    }
}
