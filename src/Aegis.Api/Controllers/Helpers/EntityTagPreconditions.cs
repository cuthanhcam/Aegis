using Aegis.Application.Contracts;

namespace Aegis.Api.Controllers.Helpers;

internal static class EntityTagPreconditions
{
    public static string Format(long revision) => $"\"{revision}\"";

    public static long RequireRevision(string? ifMatch)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            throw new PreconditionRequiredException("If-Match is required for this mutation.");
        }

        var value = ifMatch.Trim();
        if (value.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("If-Match must contain a strong entity tag.");
        }

        if (value.Length < 3 || value[0] != '"' || value[^1] != '"'
            || !long.TryParse(value[1..^1], out var revision) || revision <= 0)
        {
            throw new ArgumentException("If-Match must be a quoted positive revision, for example \"3\".");
        }

        return revision;
    }
}
