namespace Aegis.Api.Middlewares
{
    internal static class ErrorEnvelopePathClassifier
    {
        public static bool IsCompatibilityPath(PathString path)
        {
            var pathValue = path.Value ?? string.Empty;

            if (!pathValue.StartsWith("/api/v1/stores/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return pathValue.Contains("/compat", StringComparison.OrdinalIgnoreCase);
        }
    }
}
