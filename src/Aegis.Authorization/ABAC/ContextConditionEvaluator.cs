using System.Text.Json;
using Aegis.Authorization.Core.Models;

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

        public static bool Evaluate(
            string expression,
            CheckRequest request)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return false;
            }

            foreach (var orPart in SplitTopLevel(expression, " or "))
            {
                if (EvaluateAndExpression(orPart, request))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool EvaluateAndExpression(string expression, CheckRequest request)
        {
            foreach (var andPart in SplitTopLevel(expression, " and "))
            {
                if (!EvaluatePredicate(andPart.Trim(), request))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool EvaluatePredicate(string predicate, CheckRequest request)
        {
            if (predicate.StartsWith("not ", StringComparison.OrdinalIgnoreCase))
            {
                return !EvaluatePredicate(predicate[4..].Trim(), request);
            }

            var operators = new[] { ">=", "<=", "!=", "==", ">", "<" };
            foreach (var op in operators)
            {
                var index = IndexOfOperator(predicate, op);
                if (index < 0)
                {
                    continue;
                }

                var left = ResolveValue(predicate[..index].Trim(), request);
                var right = ResolveValue(predicate[(index + op.Length)..].Trim(), request);
                return Compare(left, right, op);
            }

            return Evaluate(predicate, request.Context);
        }

        private static object? ResolveValue(string token, CheckRequest request)
        {
            if (token.Length >= 2 && token[0] == '"' && token[^1] == '"')
            {
                return token[1..^1];
            }

            if (bool.TryParse(token, out var boolean))
            {
                return boolean;
            }

            if (decimal.TryParse(token, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var number))
            {
                return number;
            }

            if (token.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return token.ToLowerInvariant() switch
            {
                "subject" => request.Subject.Value,
                "subject.id" => GetId(request.Subject.Value),
                "subject.type" => GetTypeName(request.Subject.Value),
                "relation" => request.Relation,
                "object" => request.Object.Value,
                "object.id" => GetId(request.Object.Value),
                "object.type" => GetTypeName(request.Object.Value),
                "resource" => request.Object.Value,
                "resource.id" => GetId(request.Object.Value),
                "resource.type" => GetTypeName(request.Object.Value),
                _ => ResolveContextValue(token, request.Context),
            };
        }

        private static object? ResolveContextValue(string token, IReadOnlyDictionary<string, JsonElement>? context)
        {
            if (context is null)
            {
                return null;
            }

            var key = token.StartsWith("context.", StringComparison.OrdinalIgnoreCase)
                ? token[8..]
                : token;

            if (!context.TryGetValue(key, out var value))
            {
                return null;
            }

            return value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Null => null,
                _ => value.GetRawText(),
            };
        }

        private static bool Compare(object? left, object? right, string op)
        {
            if (left is decimal leftNumber && right is decimal rightNumber)
            {
                var numberComparison = leftNumber.CompareTo(rightNumber);
                return CompareResult(numberComparison, op);
            }

            if (left is bool leftBool && right is bool rightBool)
            {
                var boolComparison = leftBool.CompareTo(rightBool);
                return op is "==" or "!=" && CompareResult(boolComparison, op);
            }

            var comparison = string.Compare(Convert.ToString(left, System.Globalization.CultureInfo.InvariantCulture), Convert.ToString(right, System.Globalization.CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
            return CompareResult(comparison, op);
        }

        private static bool CompareResult(int comparison, string op)
        {
            return op switch
            {
                "==" => comparison == 0,
                "!=" => comparison != 0,
                ">" => comparison > 0,
                ">=" => comparison >= 0,
                "<" => comparison < 0,
                "<=" => comparison <= 0,
                _ => false,
            };
        }

        private static int IndexOfOperator(string expression, string op)
        {
            var inString = false;
            for (var i = 0; i <= expression.Length - op.Length; i++)
            {
                if (expression[i] == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (!inString && expression.AsSpan(i, op.Length).Equals(op, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private static IReadOnlyList<string> SplitTopLevel(string expression, string delimiter)
        {
            var parts = new List<string>();
            var start = 0;
            var inString = false;
            for (var i = 0; i <= expression.Length - delimiter.Length; i++)
            {
                if (expression[i] == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString || !expression.AsSpan(i, delimiter.Length).Equals(delimiter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                parts.Add(expression[start..i]);
                i += delimiter.Length - 1;
                start = i + 1;
            }

            parts.Add(expression[start..]);
            return parts;
        }

        private static string GetTypeName(string value)
        {
            var separator = value.IndexOf(':');
            return separator > 0 ? value[..separator] : value;
        }

        private static string GetId(string value)
        {
            var separator = value.IndexOf(':');
            return separator > 0 && separator < value.Length - 1 ? value[(separator + 1)..] : value;
        }
    }
}
