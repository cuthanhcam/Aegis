namespace Aegis.Authorization.Core.Engine
{
    /// <summary>
    /// Options for `AuthorizationEngine` configuration.
    /// </summary>
    public sealed class AuthorizationEngineOptions
    {
        /// <summary>
        /// Maximum recursion depth for rewrite evaluation. Protects against runaway rewrites/cycles.
        /// Default: 8
        /// </summary>
        public int MaxDepth { get; set; } = 8;
        /// <summary>
        /// Parsed authorization model cache time-to-live in seconds. Default: 300 (5 minutes).
        /// </summary>
        public int ParsedModelCacheTtlSeconds { get; set; } = 300;

        /// <summary>
        /// Approximate maximum number of parsed models to retain in the cache. Default: 1024.
        /// </summary>
        public int ParsedModelCacheSizeLimit { get; set; } = 1024;
    }
}
