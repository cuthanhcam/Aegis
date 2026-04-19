namespace Aegis.Authorization.Core.Parsing
{
    /// <summary>
    /// Parses rewrite expressions into normalized include/exclude terms used by authorization evaluation.
    /// </summary>
    public static class RewriteExpressionParser
    {
        private const string OrKeyword = "or";
        private const string AndKeyword = "and";
        private const string ButNotKeyword = "but not";

        /// <summary>
        /// Parses a rewrite expression into a list of deterministic terms.
        /// </summary>
        /// <remarks>
        /// The parser supports top-level <c>or</c>, conjunctions via <c>and</c>,
        /// subtraction via <c>but not</c>, grouped clauses in <c>(...)</c>, and
        /// option lists in <c>[a, b, c]</c>.
        /// </remarks>
        public static IReadOnlyList<RewriteExpressionTerm> Parse(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return [];
            }

            var terms = new List<RewriteExpressionTerm>();
            foreach (var orPart in SplitTopLevel(expression, OrKeyword))
            {
                var clause = orPart.Trim();
                if (string.IsNullOrWhiteSpace(clause))
                {
                    continue;
                }

                var splitIndex = IndexOfTopLevel(clause, ButNotKeyword);
                var includeExpression = splitIndex >= 0 ? clause[..splitIndex].Trim() : clause;
                var excludeExpression = splitIndex >= 0 ? clause[(splitIndex + ButNotKeyword.Length)..].Trim() : string.Empty;

                var includeOptions = ParseConjunctionOptions(includeExpression);
                if (includeOptions.Count == 0)
                {
                    continue;
                }

                var excludeClauses = string.IsNullOrWhiteSpace(excludeExpression)
                    ? []
                    : ParseConjunctionOptions(excludeExpression).Select(option => (IReadOnlyList<string>)option).ToList();

                foreach (var includes in includeOptions)
                {
                    terms.Add(new RewriteExpressionTerm(includes, excludeClauses));
                }
            }

            return terms;
        }

        private static List<List<string>> ParseConjunctionOptions(string expression)
        {
            var andParts = SplitTopLevel(expression, AndKeyword);
            var options = new List<List<string>> { new List<string>() };

            foreach (var part in andParts)
            {
                var factorOptions = ParseFactorOptions(part.Trim());
                if (factorOptions.Count == 0)
                {
                    return [];
                }

                var next = new List<List<string>>();
                foreach (var option in options)
                {
                    foreach (var factorOption in factorOptions)
                    {
                        var combined = new List<string>(option.Count + factorOption.Count);
                        combined.AddRange(option);
                        combined.AddRange(factorOption);
                        next.Add(combined);
                    }
                }

                options = next;
            }

            return options;
        }

        private static List<List<string>> ParseFactorOptions(string factor)
        {
            if (string.IsNullOrWhiteSpace(factor))
            {
                return [];
            }

            if (IsWrapped(factor, '(', ')'))
            {
                return ParseExpressionOptions(factor[1..^1]);
            }

            if (IsWrapped(factor, '[', ']'))
            {
                var items = SplitTopLevel(factor[1..^1], ",", requireWordBoundaries: false);
                var options = new List<List<string>>();
                foreach (var item in items)
                {
                    var token = item.Trim();
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        options.Add([token]);
                    }
                }

                return options;
            }

            return [[factor]];
        }

        private static List<List<string>> ParseExpressionOptions(string expression)
        {
            var options = new List<List<string>>();
            foreach (var part in SplitTopLevel(expression, OrKeyword))
            {
                options.AddRange(ParseConjunctionOptions(part.Trim()));
            }

            return options;
        }

        private static List<string> SplitTopLevel(string expression, string delimiter, bool requireWordBoundaries = true)
        {
            var parts = new List<string>();
            var start = 0;
            var depthParen = 0;
            var depthBracket = 0;

            for (var index = 0; index < expression.Length; index++)
            {
                var current = expression[index];
                switch (current)
                {
                    case '(':
                        depthParen++;
                        continue;
                    case ')':
                        depthParen = Math.Max(0, depthParen - 1);
                        continue;
                    case '[':
                        depthBracket++;
                        continue;
                    case ']':
                        depthBracket = Math.Max(0, depthBracket - 1);
                        continue;
                }

                if (depthParen != 0 || depthBracket != 0)
                {
                    continue;
                }

                if (!IsDelimiterAt(expression, index, delimiter, requireWordBoundaries))
                {
                    continue;
                }

                parts.Add(expression[start..index]);
                index += delimiter.Length - 1;
                start = index + 1;
            }

            parts.Add(expression[start..]);
            return parts;
        }

        private static int IndexOfTopLevel(string expression, string delimiter)
        {
            var depthParen = 0;
            var depthBracket = 0;

            for (var index = 0; index <= expression.Length - delimiter.Length; index++)
            {
                var current = expression[index];
                switch (current)
                {
                    case '(':
                        depthParen++;
                        continue;
                    case ')':
                        depthParen = Math.Max(0, depthParen - 1);
                        continue;
                    case '[':
                        depthBracket++;
                        continue;
                    case ']':
                        depthBracket = Math.Max(0, depthBracket - 1);
                        continue;
                }

                if (depthParen != 0 || depthBracket != 0)
                {
                    continue;
                }

                if (IsDelimiterAt(expression, index, delimiter, requireWordBoundaries: true))
                {
                    return index;
                }
            }

            return -1;
        }

        private static bool IsDelimiterAt(string expression, int index, string delimiter, bool requireWordBoundaries)
        {
            if (index < 0 || index + delimiter.Length > expression.Length)
            {
                return false;
            }

            if (!expression.AsSpan(index, delimiter.Length).Equals(delimiter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!requireWordBoundaries)
            {
                return true;
            }

            var beforeOk = index == 0 || char.IsWhiteSpace(expression[index - 1]);
            var afterIndex = index + delimiter.Length;
            var afterOk = afterIndex >= expression.Length || char.IsWhiteSpace(expression[afterIndex]);
            return beforeOk && afterOk;
        }

        private static bool IsWrapped(string expression, char open, char close)
        {
            if (expression.Length < 2 || expression[0] != open || expression[^1] != close)
            {
                return false;
            }

            var depth = 0;
            for (var index = 0; index < expression.Length; index++)
            {
                if (expression[index] == open)
                {
                    depth++;
                }
                else if (expression[index] == close)
                {
                    depth--;
                    if (depth == 0 && index < expression.Length - 1)
                    {
                        return false;
                    }
                }

                if (depth < 0)
                {
                    return false;
                }
            }

            return depth == 0;
        }
    }

    /// <summary>
    /// One normalized rewrite expression term.
    /// </summary>
    /// <param name="Includes">Tokens that must all match.</param>
    /// <param name="ExcludeClauses">Clauses that deny the term when matched.</param>
    public sealed record RewriteExpressionTerm(
        IReadOnlyList<string> Includes,
        IReadOnlyList<IReadOnlyList<string>> ExcludeClauses);
}
