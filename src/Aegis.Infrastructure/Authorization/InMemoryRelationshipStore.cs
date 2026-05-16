using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Domain.Repositories;
using System.Collections.Concurrent;
using DomainRelationship = Aegis.Domain.Entities.Relationship;
using DomainRelationshipChange = Aegis.Domain.Entities.RelationshipChangeEntry;
using DomainRelationshipPermissionEffect = Aegis.Domain.Enums.RelationshipPermissionEffect;

namespace Aegis.Infrastructure.Authorization
{
    public sealed class InMemoryRelationshipStore : IRelationshipStore, IRelationshipRepository
    {
        private readonly ConcurrentDictionary<string, (string TenantId, RelationshipTuple Tuple)> _tuples = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentQueue<RelationshipChange> _changes = new();

        public Task<IReadOnlyList<RelationshipTuple>> QueryAsync(
            string tenantId,
            Subject? subject,
            string? relation,
            ObjectRef? obj,
            RelationshipEffect? effect,
            CancellationToken cancellationToken = default)
        {
            var result = _tuples.Values
                .Where(x => string.Equals(x.TenantId, tenantId, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Tuple)
                .Where(x => x.Subject == (subject ?? x.Subject))
                .Where(x => relation is null || x.Relation.Equals(relation, StringComparison.OrdinalIgnoreCase))
                .Where(x => x.Object == (obj ?? x.Object))
                .Where(x => effect is null || x.Effect == effect)
                .ToList();

            return Task.FromResult<IReadOnlyList<RelationshipTuple>>(result);
        }

        public Task UpsertAsync(
            string tenantId,
            RelationshipTuple tuple,
            CancellationToken cancellationToken = default)
        {
            _tuples[Key(tenantId, tuple.Subject.Value, tuple.Relation, tuple.Object.Value)] = (tenantId, tuple);
            _changes.Enqueue(new RelationshipChange(tenantId, tuple.Subject, tuple.Relation, tuple.Object, "upsert", tuple.CreatedAt));
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(
            string tenantId,
            Subject subject,
            string relation,
            ObjectRef obj,
            CancellationToken cancellationToken = default)
        {
            var removed = _tuples.TryRemove(Key(tenantId, subject.Value, relation, obj.Value), out _);
            if (removed)
            {
                _changes.Enqueue(new RelationshipChange(tenantId, subject, relation, obj, "delete", DateTimeOffset.UtcNow));
            }

            return Task.FromResult(removed);
        }

        public Task<IReadOnlyList<RelationshipChange>> ReadChangesAsync(
            string tenantId,
            int offset,
            int limit,
            CancellationToken cancellationToken = default)
        {
            var data = _changes
                .Where(x => string.Equals(x.TenantId, tenantId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .ToList();

            return Task.FromResult<IReadOnlyList<RelationshipChange>>(data);
        }

        public Task PurgeTenantAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            foreach (var entry in _tuples.ToArray())
            {
                if (string.Equals(entry.Value.TenantId, tenantId, StringComparison.OrdinalIgnoreCase))
                {
                    _tuples.TryRemove(entry.Key, out _);
                }
            }

            if (_changes.IsEmpty)
            {
                return Task.CompletedTask;
            }

            var retained = _changes.Where(x => !string.Equals(x.TenantId, tenantId, StringComparison.OrdinalIgnoreCase)).ToArray();
            while (_changes.TryDequeue(out _))
            {
            }

            foreach (var change in retained)
            {
                _changes.Enqueue(change);
            }

            return Task.CompletedTask;
        }

        Task<IReadOnlyList<DomainRelationship>> IRelationshipRepository.QueryAsync(
            string tenantId,
            string? subject,
            string? relation,
            string? obj,
            DomainRelationshipPermissionEffect? effect,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<DomainRelationship> result = _tuples.Values
                .Where(x => string.Equals(x.TenantId, tenantId, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Tuple)
                .Where(x => subject is null || x.Subject.Value.Equals(subject, StringComparison.OrdinalIgnoreCase))
                .Where(x => relation is null || x.Relation.Equals(relation, StringComparison.OrdinalIgnoreCase))
                .Where(x => obj is null || x.Object.Value.Equals(obj, StringComparison.OrdinalIgnoreCase))
                .Where(x => effect is null || (x.Effect == RelationshipEffect.Deny ? DomainRelationshipPermissionEffect.Deny : DomainRelationshipPermissionEffect.Allow) == effect)
                .Select(x => DomainRelationship.Rehydrate(Guid.NewGuid(), tenantId, x.Subject.Value, x.Relation, x.Object.Value, x.Effect == RelationshipEffect.Deny ? DomainRelationshipPermissionEffect.Deny : DomainRelationshipPermissionEffect.Allow, x.CreatedAt, x.CreatedAt))
                .ToList();

            return Task.FromResult(result);
        }

        Task IRelationshipRepository.UpsertAsync(
            DomainRelationship relationship,
            CancellationToken cancellationToken)
        {
            var tuple = new RelationshipTuple(new Subject(relationship.Subject.Value), relationship.Relation.Value, new ObjectRef(relationship.Object.Value), relationship.Effect == DomainRelationshipPermissionEffect.Deny ? RelationshipEffect.Deny : RelationshipEffect.Allow, relationship.CreatedAt);
            return UpsertAsync(relationship.TenantId, tuple, cancellationToken);
        }

        Task<bool> IRelationshipRepository.DeleteAsync(
            string tenantId,
            string subject,
            string relation,
            string obj,
            CancellationToken cancellationToken)
        {
            return DeleteAsync(tenantId, new Subject(subject), relation, new ObjectRef(obj), cancellationToken);
        }

        Task<IReadOnlyList<DomainRelationshipChange>> IRelationshipRepository.ReadChangesAsync(
            string tenantId,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<DomainRelationshipChange> data = _changes
                .Where(x => string.Equals(x.TenantId, tenantId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .Select(x => DomainRelationshipChange.Rehydrate(Guid.NewGuid(), x.TenantId, x.Subject.Value, x.Relation, x.Object.Value, x.Operation, x.CreatedAt))
                .ToList();

            return Task.FromResult(data);
        }

        Task IRelationshipRepository.PurgeTenantAsync(
            string tenantId,
            CancellationToken cancellationToken)
        {
            return PurgeTenantAsync(tenantId, cancellationToken);
        }

        private static string Key(
            string tenantId,
            string subject,
            string relation,
            string obj)
        {
            return $"{tenantId}:{subject}:{relation}:{obj}";
        }
    }
}
