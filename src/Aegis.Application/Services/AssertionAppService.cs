using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Contracts.Compatibility;
using Aegis.Domain.ValueObjects;
using System.Collections.Concurrent;

namespace Aegis.Application.Services
{
    public sealed class AssertionAppService : IAssertionAppService
    {
        private static readonly ConcurrentDictionary<string, IReadOnlyList<AegisCompatAssertionDto>> AssertionsByModel = new(StringComparer.OrdinalIgnoreCase);
        private const int MaxAssertionsPerModel = 100;
        private readonly IStoreRegistry _storeRegistry;
        private readonly IAuthorizationModelRegistry _authorizationModelRegistry;

        public AssertionAppService(IStoreRegistry storeRegistry, IAuthorizationModelRegistry authorizationModelRegistry)
        {
            _storeRegistry = storeRegistry;
            _authorizationModelRegistry = authorizationModelRegistry;
        }

        public async Task<AegisCompatReadAssertionsResponseDto> ReadAsync(
            string storeId,
            string authorizationModelId,
            CancellationToken cancellationToken = default)
        {
            await EnsureStoreExists(storeId, cancellationToken);
            if (string.IsNullOrWhiteSpace(authorizationModelId))
            {
                throw new CompatibilityApiException(400, "validation_error", "authorization_model_id is required.");
            }

            await EnsureModelExists(storeId, authorizationModelId, cancellationToken);

            var key = BuildKey(storeId, authorizationModelId);
            AssertionsByModel.TryGetValue(key, out var assertions);
            return new AegisCompatReadAssertionsResponseDto(authorizationModelId, assertions ?? []);
        }

        public async Task WriteAsync(
            string storeId,
            string authorizationModelId,
            AegisCompatWriteAssertionsRequestDto request,
            CancellationToken cancellationToken = default)
        {
            await EnsureStoreExists(storeId, cancellationToken);
            if (string.IsNullOrWhiteSpace(authorizationModelId))
            {
                throw new CompatibilityApiException(400, "validation_error", "authorization_model_id is required.");
            }

            var model = await EnsureModelExists(storeId, authorizationModelId, cancellationToken);

            if (request.Assertions is null)
            {
                throw new CompatibilityApiException(400, "validation_error", "assertions are required.");
            }

            if (request.Assertions.Count > MaxAssertionsPerModel)
            {
                throw new CompatibilityApiException(
                    400,
                    "assertions_too_many_items",
                    $"assertions exceeds max allowed items of {MaxAssertionsPerModel}.");
            }

            var relationIndex = BuildRelationIndex(model.Model);

            foreach (var assertion in request.Assertions)
            {
                ValidateAssertion(assertion, relationIndex);
            }

            AssertionsByModel[BuildKey(storeId, authorizationModelId)] = request.Assertions.ToList();
        }

        public Task PurgeStoreAsync(string storeId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(storeId))
            {
                throw new ArgumentException("storeId is required.");
            }

            foreach (var key in AssertionsByModel.Keys
                         .Where(x => x.StartsWith($"{storeId}:", StringComparison.OrdinalIgnoreCase))
                         .ToArray())
            {
                AssertionsByModel.TryRemove(key, out _);
            }

            return Task.CompletedTask;
        }

        private async Task EnsureStoreExists(string storeId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(storeId))
            {
                throw new CompatibilityApiException(400, "validation_error", "store_id is required.");
            }

            var store = await _storeRegistry.GetAsync(storeId, cancellationToken);
            if (store is null)
            {
                throw new CompatibilityApiException(404, "store_id_not_found", "Store ID not found.");
            }
        }

        private static void ValidateAssertion(
            AegisCompatAssertionDto assertion,
            IReadOnlyDictionary<string, HashSet<string>> relationIndex)
        {
            if (!SubjectId.TryCreate(assertion.TupleKey.User, out _)
                || !RelationName.TryCreate(assertion.TupleKey.Relation, out _)
                || !ObjectId.TryCreate(assertion.TupleKey.Object, out _))
            {
                throw new CompatibilityApiException(400, "validation_error", "Invalid assertion tuple_key format.");
            }

            ValidateTypeAndRelation(assertion.TupleKey.Object, assertion.TupleKey.Relation, relationIndex);

            var contextual = assertion.ContextualTuples?.TupleKeys;
            if (contextual is null)
            {
                return;
            }

            foreach (var tuple in contextual)
            {
                if (!SubjectId.TryCreate(tuple.User, out _)
                    || !RelationName.TryCreate(tuple.Relation, out _)
                    || !ObjectId.TryCreate(tuple.Object, out _))
                {
                    throw new CompatibilityApiException(400, "validation_error", "Invalid assertion contextual tuple format.");
                }

                ValidateTypeAndRelation(tuple.Object, tuple.Relation, relationIndex);
            }
        }

        private async Task<AuthorizationModelDto> EnsureModelExists(string storeId, string authorizationModelId, CancellationToken cancellationToken)
        {
            var model = await _authorizationModelRegistry.GetByIdAsync(storeId, authorizationModelId, cancellationToken);
            if (model is null)
            {
                throw new CompatibilityApiException(
                    400,
                    "authorization_model_not_found",
                    $"Authorization Model '{authorizationModelId}' not found");
            }

            return model;
        }

        private static IReadOnlyDictionary<string, HashSet<string>> BuildRelationIndex(string model)
        {
            var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var lines = model.Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string? currentType = null;

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.StartsWith("type ", StringComparison.OrdinalIgnoreCase))
                {
                    currentType = line[5..].Trim();
                    if (!result.ContainsKey(currentType))
                    {
                        result[currentType] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    }

                    continue;
                }

                if (currentType is null || !line.StartsWith("define ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var def = line[7..];
                var separatorIndex = def.IndexOf(':');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                result[currentType].Add(def[..separatorIndex].Trim());
            }

            return result;
        }

        private static void ValidateTypeAndRelation(
            string objectRef,
            string relation,
            IReadOnlyDictionary<string, HashSet<string>> relationIndex)
        {
            var typeSeparator = objectRef.IndexOf(':');
            var typeName = typeSeparator > 0 ? objectRef[..typeSeparator] : objectRef;

            if (!relationIndex.TryGetValue(typeName, out var relations))
            {
                throw new CompatibilityApiException(400, "type_not_found", $"type '{typeName}' not found");
            }

            if (!relations.Contains(relation))
            {
                throw new CompatibilityApiException(
                    400,
                    "relation_not_found",
                    $"relation '{typeName}#{relation}' not found");
            }
        }

        private static string BuildKey(string storeId, string authorizationModelId)
            => $"{storeId}:{authorizationModelId}";
    }
}
