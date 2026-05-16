using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using System.Collections.Concurrent;

namespace Aegis.Infrastructure.Authorization
{
    public sealed class InMemoryAuditStore : IAuditStore
    {
        private readonly ConcurrentQueue<AuditEvent> _events = new();

        public Task WriteAsync(
            AuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            _events.Enqueue(auditEvent);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditEvent>> QueryAsync(
            string tenantId,
            string? action,
            string? decision,
            CancellationToken cancellationToken = default)
        {
            var data = _events
                .Where(x => string.Equals(x.TenantId, tenantId, StringComparison.OrdinalIgnoreCase))
                .Where(x => action is null || string.Equals(x.Action, action, StringComparison.OrdinalIgnoreCase))
                .Where(x => decision is null || string.Equals(x.Decision, decision, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            return Task.FromResult<IReadOnlyList<AuditEvent>>(data);
        }
    }
}
