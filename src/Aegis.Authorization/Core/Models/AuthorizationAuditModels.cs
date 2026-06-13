namespace Aegis.Authorization.Core.Models
{
    /// <summary>
    /// Immutable audit event payload recorded for authorization decisions.
    /// </summary>
    public sealed record AuditEvent(
        string TenantId,
        string Action,
        string Subject,
        string Relation,
        string Object,
        string Decision,
        string ReasonCode,
        DateTimeOffset CreatedAt,
        string? StoreId = null);
}
