using Aegis.Application.DomainEvents;
using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Authorization.Core.Models;
using Aegis.Contracts.Compatibility;
using Aegis.Contracts.Relationships;
using Aegis.Domain.Entities;
using Aegis.Domain.Enums;
using Aegis.Domain.Repositories;
using Aegis.Domain.ValueObjects;

namespace Aegis.Application.Services
{
    public sealed class RelationshipAppService : IRelationshipService
    {
        private const int DefaultChangesPageSize = 50;
        private const int MaxChangesPageSize = 100;
        private const int DefaultReadPageSize = 50;
        private const int MaxReadPageSize = 100;
        private readonly IRelationshipStore _relationshipStore;
        private readonly IRelationshipRepository? _relationshipRepository;
        private readonly IDomainEventDispatcher? _domainEventDispatcher;

        public RelationshipAppService(IRelationshipStore relationshipStore)
        {
            _relationshipStore = relationshipStore;
            _relationshipRepository = relationshipStore as IRelationshipRepository;
            _domainEventDispatcher = null;
        }

        public RelationshipAppService(
            IRelationshipStore relationshipStore,
            IRelationshipRepository relationshipRepository,
            IDomainEventDispatcher domainEventDispatcher)
            : this(relationshipStore)
        {
            _relationshipRepository = relationshipRepository;
            _domainEventDispatcher = domainEventDispatcher;
        }

        public async Task<IReadOnlyList<RelationshipTupleDto>> QueryAsync(
            string tenantId,
            string? subject,
            string? relation,
            string? objectRef,
            string? effect,
            CancellationToken cancellationToken = default)
        {
            if (subject is not null && !SubjectId.TryCreate(subject, out _))
            {
                throw new ArgumentException("Invalid subject format.");
            }

            if (objectRef is not null && !ObjectId.TryCreate(objectRef, out _))
            {
                throw new ArgumentException("Invalid object format.");
            }

            if (_relationshipRepository is not null)
            {
                var relationships = await _relationshipRepository.QueryAsync(
                    tenantId,
                    subject,
                    relation,
                    objectRef,
                    ParseDomainEffectNullable(effect),
                    cancellationToken);

                return relationships
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(ToDto)
                    .ToList();
            }

            var tuples = await _relationshipStore.QueryAsync(
                tenantId,
                subject is null ? null : new Subject(subject),
                relation,
                objectRef is null ? null : new ObjectRef(objectRef),
                ParseEffectNullable(effect),
                cancellationToken);

            return tuples
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new RelationshipTupleDto(
                    x.Subject.Value,
                    x.Relation,
                    x.Object.Value,
                    x.Effect == RelationshipEffect.Allow ? "allow" : "deny",
                    x.CreatedAt))
                .ToList();
        }

        public async Task UpsertAsync(string tenantId, RelationshipWriteRequestDto request, CancellationToken cancellationToken = default)
        {
            ValidateTuple(request.Subject, request.Relation, request.Object);

            if (_relationshipRepository is not null)
            {
                var relationship = Relationship.Create(
                    tenantId,
                    request.Subject,
                    request.Relation,
                    request.Object,
                    ParseDomainEffect(request.Effect),
                    DateTimeOffset.UtcNow);

                await _relationshipRepository.UpsertAsync(relationship, cancellationToken);
                await _domainEventDispatcher.DispatchAndClearAsync(relationship, cancellationToken);
                return;
            }

            await _relationshipStore.UpsertAsync(
                tenantId,
                new RelationshipTuple(
                    new Subject(request.Subject),
                    request.Relation,
                    new ObjectRef(request.Object),
                    ParseEffect(request.Effect),
                    DateTimeOffset.UtcNow),
                cancellationToken);
        }

        public async Task<bool> DeleteAsync(string tenantId, RelationshipDeleteRequestDto request, CancellationToken cancellationToken = default)
        {
            ValidateTuple(request.Subject, request.Relation, request.Object);

            if (_relationshipRepository is not null)
            {
                var existing = await _relationshipRepository.QueryAsync(
                    tenantId,
                    request.Subject,
                    request.Relation,
                    request.Object,
                    null,
                    cancellationToken);

                var relationship = existing.FirstOrDefault();
                if (relationship is null)
                {
                    return false;
                }

                relationship.MarkDeleted();
                var deleted = await _relationshipRepository.DeleteAsync(
                    tenantId,
                    request.Subject,
                    request.Relation,
                    request.Object,
                    cancellationToken);

                if (deleted)
                {
                    await _domainEventDispatcher.DispatchAndClearAsync(relationship, cancellationToken);
                }

                return deleted;
            }

            return await _relationshipStore.DeleteAsync(
                tenantId,
                new Subject(request.Subject),
                request.Relation,
                new ObjectRef(request.Object),
                cancellationToken);
        }

        public async Task<ReadChangesResponseDto> ReadChangesAsync(string tenantId, ReadChangesRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentException("tenantId is required.");
            }

            var pageSize = request.PageSize.GetValueOrDefault(DefaultChangesPageSize);
            if (request.PageSize is <= 0 || request.PageSize is > MaxChangesPageSize)
            {
                throw new CompatibilityApiException(
                    400,
                    "page_size_invalid",
                    $"invalid page_size: value must be inside range [1, {MaxChangesPageSize}]");
            }

            var offset = 0;
            if (!string.IsNullOrWhiteSpace(request.PageToken) && !int.TryParse(request.PageToken, out offset))
            {
                throw new CompatibilityApiException(400, "invalid_continuation_token", "Invalid continuation token");
            }

            if (offset < 0)
            {
                throw new CompatibilityApiException(400, "invalid_continuation_token", "Invalid continuation token");
            }

            if (_relationshipRepository is not null)
            {
                var changes = await _relationshipRepository.ReadChangesAsync(tenantId, offset, pageSize, cancellationToken);
                if (!string.IsNullOrWhiteSpace(request.Type))
                {
                    changes = changes
                        .Where(x => x.Object.StartsWith($"{request.Type}:", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                var items = changes
                    .Select(x => new RelationshipChangeDto(
                        x.Subject,
                        x.Relation,
                        x.Object,
                        x.Operation,
                        x.CreatedAt))
                    .ToList();

                var continuationToken = items.Count == pageSize ? (offset + pageSize).ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
                return new ReadChangesResponseDto(items, continuationToken);
            }

            var changesFromStore = await _relationshipStore.ReadChangesAsync(tenantId, offset, pageSize, cancellationToken);
            if (!string.IsNullOrWhiteSpace(request.Type))
            {
                changesFromStore = changesFromStore
                    .Where(x => x.Object.Value.StartsWith($"{request.Type}:", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var itemsFromStore = changesFromStore
                .Select(x => new RelationshipChangeDto(
                    x.Subject.Value,
                    x.Relation,
                    x.Object.Value,
                    x.Operation,
                    x.CreatedAt))
                .ToList();

            var continuationFromStore = itemsFromStore.Count == pageSize ? (offset + pageSize).ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
            return new ReadChangesResponseDto(itemsFromStore, continuationFromStore);
        }

        public async Task<AegisCompatReadResponseDto> ReadAegisCompatAsync(
            string tenantId,
            AegisCompatReadRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentException("tenantId is required.");
            }

            var tupleKey = request.TupleKey;
            var subject = tupleKey?.User;
            var relation = tupleKey?.Relation;
            var objectRef = tupleKey?.Object;

            if (!string.IsNullOrWhiteSpace(subject) && !SubjectId.TryCreate(subject, out _))
            {
                throw new ArgumentException("Invalid tuple_key.user format.");
            }

            var objectPrefixQuery = !string.IsNullOrWhiteSpace(objectRef) && objectRef.EndsWith(':');
            if (!string.IsNullOrWhiteSpace(objectRef) && !objectPrefixQuery && !ObjectId.TryCreate(objectRef, out _))
            {
                throw new ArgumentException("Invalid tuple_key.object format.");
            }

            var pageSize = request.PageSize.GetValueOrDefault(DefaultReadPageSize);
            if (request.PageSize is <= 0 || request.PageSize is > MaxReadPageSize)
            {
                throw new CompatibilityApiException(
                    400,
                    "page_size_invalid",
                    $"invalid page_size: value must be inside range [1, {MaxReadPageSize}]");
            }

            var offset = 0;
            if (!string.IsNullOrWhiteSpace(request.ContinuationToken) && !int.TryParse(request.ContinuationToken, out offset))
            {
                throw new CompatibilityApiException(400, "invalid_continuation_token", "Invalid continuation token");
            }

            if (offset < 0)
            {
                throw new CompatibilityApiException(400, "invalid_continuation_token", "Invalid continuation token");
            }

            var tuples = await _relationshipStore.QueryAsync(
                tenantId,
                string.IsNullOrWhiteSpace(subject) ? null : new Subject(subject),
                string.IsNullOrWhiteSpace(relation) ? null : relation,
                string.IsNullOrWhiteSpace(objectRef) || objectPrefixQuery ? null : new ObjectRef(objectRef),
                null,
                cancellationToken);

            if (objectPrefixQuery)
            {
                tuples = tuples
                    .Where(x => x.Object.Value.StartsWith(objectRef!, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var page = tuples
                .OrderByDescending(x => x.CreatedAt)
                .Skip(offset)
                .Take(pageSize)
                .Select(x => new AegisCompatTupleDto(
                    new AegisCompatTupleKeyDto(x.Subject.Value, x.Relation, x.Object.Value),
                    x.CreatedAt))
                .ToList();

            var continuationToken = page.Count == pageSize
                ? (offset + pageSize).ToString(System.Globalization.CultureInfo.InvariantCulture)
                : null;

            return new AegisCompatReadResponseDto(page, continuationToken);
        }

        public async Task WriteAegisCompatAsync(
            string tenantId,
            AegisCompatWriteRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentException("tenantId is required.");
            }

            var writeTuples = request.Writes?.TupleKeys ?? [];
            var deleteTuples = request.Deletes?.TupleKeys ?? [];
            if (writeTuples.Count == 0 && deleteTuples.Count == 0)
            {
                throw new CompatibilityApiException(400, "invalid_write_input", "Invalid input. Make sure you provide at least one write, or at least one delete");
            }

            var onDuplicate = request.Writes?.OnDuplicate;
            var ignoreDuplicate = string.Equals(onDuplicate, "ignore", StringComparison.OrdinalIgnoreCase);

            foreach (var tuple in writeTuples)
            {
                ValidateTuple(tuple.User, tuple.Relation, tuple.Object);

                if (!ignoreDuplicate)
                {
                    var existing = await _relationshipStore.QueryAsync(
                        tenantId,
                        new Subject(tuple.User),
                        tuple.Relation,
                        new ObjectRef(tuple.Object),
                        RelationshipEffect.Allow,
                        cancellationToken);

                    if (existing.Count > 0)
                    {
                        throw new CompatibilityApiException(
                            400,
                            "write_failed_due_to_invalid_input",
                            $"cannot write a tuple which already exists: user: '{tuple.User}', relation: '{tuple.Relation}', object: '{tuple.Object}'");
                    }
                }

                await _relationshipStore.UpsertAsync(
                    tenantId,
                    new RelationshipTuple(
                        new Subject(tuple.User),
                        tuple.Relation,
                        new ObjectRef(tuple.Object),
                        RelationshipEffect.Allow,
                        DateTimeOffset.UtcNow),
                    cancellationToken);
            }

            var ignoreMissing = string.Equals(request.Deletes?.OnMissing, "ignore", StringComparison.OrdinalIgnoreCase);
            foreach (var tuple in deleteTuples)
            {
                ValidateTuple(tuple.User, tuple.Relation, tuple.Object);
                var deleted = await _relationshipStore.DeleteAsync(
                    tenantId,
                    new Subject(tuple.User),
                    tuple.Relation,
                    new ObjectRef(tuple.Object),
                    cancellationToken);

                if (!deleted && !ignoreMissing)
                {
                    throw new CompatibilityApiException(
                        400,
                        "write_failed_due_to_invalid_input",
                        $"cannot delete a tuple which does not exist: user: '{tuple.User}', relation: '{tuple.Relation}', object: '{tuple.Object}'");
                }
            }
        }

        private static void ValidateTuple(string subject, string relation, string objectRef)
        {
            if (!RelationName.TryCreate(relation, out _) || !SubjectId.TryCreate(subject, out _) || !ObjectId.TryCreate(objectRef, out _))
            {
                throw new ArgumentException("Invalid tuple format. Expected subject/object as <type>:<id> and non-empty relation.");
            }
        }

        private static RelationshipEffect ParseEffect(string effect)
        {
            return effect.Equals("deny", StringComparison.OrdinalIgnoreCase) ? RelationshipEffect.Deny : RelationshipEffect.Allow;
        }

        private static RelationshipPermissionEffect ParseDomainEffect(string effect)
        {
            return effect.Equals("deny", StringComparison.OrdinalIgnoreCase)
                ? RelationshipPermissionEffect.Deny
                : RelationshipPermissionEffect.Allow;
        }

        private static RelationshipEffect? ParseEffectNullable(string? effect)
        {
            if (effect is null)
            {
                return null;
            }

            if (effect.Equals("allow", StringComparison.OrdinalIgnoreCase))
            {
                return RelationshipEffect.Allow;
            }

            if (effect.Equals("deny", StringComparison.OrdinalIgnoreCase))
            {
                return RelationshipEffect.Deny;
            }

            throw new ArgumentException("effect must be allow or deny");
        }

        private static RelationshipPermissionEffect? ParseDomainEffectNullable(string? effect)
        {
            if (effect is null)
            {
                return null;
            }

            if (effect.Equals("allow", StringComparison.OrdinalIgnoreCase))
            {
                return RelationshipPermissionEffect.Allow;
            }

            if (effect.Equals("deny", StringComparison.OrdinalIgnoreCase))
            {
                return RelationshipPermissionEffect.Deny;
            }

            throw new ArgumentException("effect must be allow or deny");
        }

        private static RelationshipTupleDto ToDto(Relationship relationship)
        {
            return new RelationshipTupleDto(
                relationship.Subject.Value,
                relationship.Relation.Value,
                relationship.Object.Value,
                relationship.Effect == RelationshipPermissionEffect.Deny ? "deny" : "allow",
                relationship.CreatedAt);
        }
    }
}
