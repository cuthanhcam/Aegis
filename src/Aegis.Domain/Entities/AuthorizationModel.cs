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

        private AuthorizationModel()
        {
            // For serializers/ORM tools.
        }

        private AuthorizationModel(
            string id,
            string storeId,
            string schemaVersion,
            string model,
            DateTimeOffset createdAt)
            : base(id)
        {
            StoreId = NormalizeStoreId(storeId);
            SchemaVersion = NormalizeSchemaVersion(schemaVersion);
            Model = NormalizeModel(model);
            CreatedAt = createdAt;
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
            DateTimeOffset createdAt)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("authorizationModelId is required.", nameof(id));
            }

            return new AuthorizationModel(id.Trim(), storeId, schemaVersion, model, createdAt);
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

        private static string NewUlidLikeId()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", string.Empty).Replace("+", "A").Replace("/", "B");
        }
    }
}
