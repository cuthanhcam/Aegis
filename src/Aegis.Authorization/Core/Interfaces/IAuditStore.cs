using Aegis.Authorization.Core.Models;

namespace Aegis.Authorization.Core.Interfaces
{
    /// <summary>
    /// Persistence contract for authorization audit events.
    /// </summary>
    public interface IAuditStore
    {
        /// <summary>
        /// Writes one immutable audit event.
        /// </summary>
        Task WriteAsync(
            AuditEvent auditEvent,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Queries audit events by tenant and optional action/decision filters.
        /// </summary>
        Task<IReadOnlyList<AuditEvent>> QueryAsync(
            string tenantId,
            string? action,
            string? decision,
            CancellationToken cancellationToken = default);
    }
}
