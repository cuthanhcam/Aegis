using Aegis.Authorization.Core.Interfaces;
using Aegis.Contracts.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aegis.Application.Features.Permissions
{
    public sealed class QueryAuditUseCase
    {
        private readonly IAuditStore _auditStore;

        public QueryAuditUseCase(IAuditStore auditStore)
        {
            _auditStore = auditStore;
        }

        public async Task<IReadOnlyList<AuditEventDto>> ExecuteAsync(
            string tenantId,
            string? action,
            string? decision,
            CancellationToken cancellationToken = default)
        {
            var events = await _auditStore.QueryAsync(tenantId, action, decision, cancellationToken);
            return events
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new AuditEventDto(
                    x.Action,
                    x.Subject,
                    x.Relation,
                    x.Object,
                    x.Decision,
                    x.ReasonCode,
                    x.CreatedAt,
                    x.StoreId))
                .ToList();
        }
    }
}
