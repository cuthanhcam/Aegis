namespace Aegis.Authorization.Core.Engine.Rewrite
{
    /// <summary>
    /// Normalized rewrite term used by the rewrite evaluator.
    /// </summary>
    /// <param name="Includes">Tokens that must all evaluate to true.</param>
    /// <param name="ExcludeClauses">Clauses that negate the term when any clause fully matches.</param>
    internal sealed record RewriteTerm(
        IReadOnlyList<string> Includes,
        IReadOnlyList<IReadOnlyList<string>> ExcludeClauses);
}
