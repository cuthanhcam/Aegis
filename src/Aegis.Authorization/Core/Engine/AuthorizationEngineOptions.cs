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
    }
}
