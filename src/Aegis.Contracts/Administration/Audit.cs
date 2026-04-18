namespace Aegis.Contracts.Administration
{
    /// <summary>
    /// Audit event payload exposed by administration endpoints.
    /// </summary>
    public sealed record AuditEventDto(
        string Action,
        string Subject,
        string Relation,
        string Object,
        string Decision,
        string ReasonCode,
        DateTimeOffset CreatedAt);
}
