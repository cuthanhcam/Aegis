namespace Aegis.Domain.Enums
{
    /// <summary>
    /// Effect applied to a relationship tuple during permission evaluation.
    /// </summary>
    public enum RelationshipPermissionEffect
    {
        /// <summary>
        /// Allows access.
        /// </summary>
        Allow = 0,

        /// <summary>
        /// Explicitly denies access.
        /// </summary>
        Deny = 1,
    }
}
