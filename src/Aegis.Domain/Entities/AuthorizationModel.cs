using Aegis.Domain.Events;
using Aegis.SharedKernel;

namespace Aegis.Domain.Entities
{
    /// <summary>
    /// Aggregate root representing a versioned authorization model for a store.
    /// </summary>
    public sealed class AuthorizationModel : AggregateRoot<string>
    {
        public string StoreId { get; private init; } = string.Empty;

        public string SchemaVersion { get; private set; } = string.Empty;

        public string Model { get; private set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; private init; }

        public string State { get; private set; } = DraftState;

        public DateTimeOffset? PublishedAt { get; private set; }

        public DateTimeOffset? ArchivedAt { get; private set; }

        public string? SupersededBy { get; private set; }

        private AuthorizationModel()
        {
            // For serializers/ORM tools.
        }

        private AuthorizationModel(
            string id,
            string storeId,
            string schemaVersion,
            string model,
            DateTimeOffset createdAt,
            string state = DraftState,
            DateTimeOffset? publishedAt = null,
            DateTimeOffset? archivedAt = null,
            string? supersededBy = null)
            : base(id)
        {
            StoreId = NormalizeStoreId(storeId);
            SchemaVersion = NormalizeSchemaVersion(schemaVersion);
            Model = NormalizeModel(model);
            CreatedAt = createdAt;
            State = NormalizeState(state);
            PublishedAt = publishedAt;
            ArchivedAt = archivedAt;
            SupersededBy = string.IsNullOrWhiteSpace(supersededBy) ? null : supersededBy.Trim();
        }

        /// <summary>
        /// Creates a new model instance and raises a creation domain event.
        /// </summary>
        public static AuthorizationModel Create(string storeId, string schemaVersion, string model)
        {
            var authorizationModel = new AuthorizationModel(NewUlidLikeId(), storeId, schemaVersion, model, DateTimeOffset.UtcNow);
            authorizationModel.RaiseDomainEvent(new AuthorizationModelCreatedDomainEvent(
                authorizationModel.Id,
                authorizationModel.StoreId,
                authorizationModel.SchemaVersion,
                authorizationModel.CreatedAt));
            return authorizationModel;
        }

        /// <summary>
        /// Rebuilds an existing model from persistence state without emitting domain events.
        /// </summary>
        public static AuthorizationModel Rehydrate(
            string id,
            string storeId,
            string schemaVersion,
            string model,
            DateTimeOffset createdAt,
            string state = DraftState,
            DateTimeOffset? publishedAt = null,
            DateTimeOffset? archivedAt = null,
            string? supersededBy = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("authorizationModelId is required.", nameof(id));
            }

            return new AuthorizationModel(id.Trim(), storeId, schemaVersion, model, createdAt, state, publishedAt, archivedAt, supersededBy);
        }

        /// <summary>
        /// Updates schema and model definition, then raises an update domain event.
        /// </summary>
        public void UpdateDefinition(string schemaVersion, string model)
        {
            SchemaVersion = NormalizeSchemaVersion(schemaVersion);
            Model = NormalizeModel(model);
            RaiseDomainEvent(new AuthorizationModelUpdatedDomainEvent(Id, StoreId, SchemaVersion, DateTimeOffset.UtcNow));
        }

        public void MarkValidated()
        {
            State = ValidatedState;
        }

        public void MarkPublished()
        {
            State = PublishedState;
            PublishedAt = DateTimeOffset.UtcNow;
            ArchivedAt = null;
            SupersededBy = null;
        }

        public void MarkArchived(string supersededBy)
        {
            if (string.IsNullOrWhiteSpace(supersededBy))
            {
                throw new ArgumentException("supersededBy is required.", nameof(supersededBy));
            }

            State = ArchivedState;
            ArchivedAt = DateTimeOffset.UtcNow;
            SupersededBy = supersededBy.Trim();
        }

        /// <summary>
        /// Marks the aggregate as deleted by raising a domain event.
        /// </summary>
        public void MarkDeleted()
        {
            RaiseDomainEvent(new AuthorizationModelDeletedDomainEvent(Id, StoreId, DateTimeOffset.UtcNow));
        }

        private static string NormalizeStoreId(string storeId)
        {
            if (string.IsNullOrWhiteSpace(storeId))
            {
                throw new ArgumentException("storeId is required.", nameof(storeId));
            }

            return storeId.Trim();
        }

        private static string NormalizeSchemaVersion(string schemaVersion)
        {
            if (string.IsNullOrWhiteSpace(schemaVersion))
            {
                throw new ArgumentException("schemaVersion is required.", nameof(schemaVersion));
            }

            var normalized = schemaVersion.Trim();
            if (normalized.Length > 32)
            {
                throw new ArgumentException("schemaVersion cannot exceed 32 characters.", nameof(schemaVersion));
            }

            return normalized;
        }

        private static string NormalizeModel(string model)
        {
            if (string.IsNullOrWhiteSpace(model))
            {
                throw new ArgumentException("model is required.", nameof(model));
            }

            return model.Trim();
        }

        private static string NormalizeState(string state)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                return DraftState;
            }

            var normalized = state.Trim();
            var allowed = new[]
            {
                DraftState,
                ValidatedState,
                PublishedState,
                ArchivedState,
                DeprecatedState,
            };

            return allowed.FirstOrDefault(x => x.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"Unsupported authorization model state '{state}'.", nameof(state));
        }

        private static string NewUlidLikeId()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", string.Empty).Replace("+", "A").Replace("/", "B");
        }

        private const string DraftState = "Draft";
        private const string ValidatedState = "Validated";
        private const string PublishedState = "Published";
        private const string ArchivedState = "Archived";
        private const string DeprecatedState = "Deprecated";
    }
}
