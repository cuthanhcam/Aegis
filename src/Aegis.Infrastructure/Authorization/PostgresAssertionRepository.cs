using Aegis.Application.Interfaces;
using Aegis.Contracts.Compatibility;
using Npgsql;
using NpgsqlTypes;
using System.Text.Json;

namespace Aegis.Infrastructure.Authorization;

public sealed class PostgresAssertionRepository : IAssertionRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly NpgsqlDataSource _dataSource;

    public PostgresAssertionRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<AssertionSetSnapshot> ReadAsync(
        string storeId,
        string authorizationModelId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await ReadAsync(connection, null, storeId, authorizationModelId, forUpdate: false, cancellationToken)
            ?? new AssertionSetSnapshot(storeId, authorizationModelId, 0, []);
    }

    public Task<AssertionSetSnapshot> ReplaceAsync(
        string storeId,
        string authorizationModelId,
        IReadOnlyList<AegisCompatAssertionDto> assertions,
        CancellationToken cancellationToken = default)
    {
        return MutateAsync(storeId, authorizationModelId, _ => assertions.ToList(), maximum: null, cancellationToken);
    }

    public Task<AssertionSetSnapshot> AppendDistinctAsync(
        string storeId,
        string authorizationModelId,
        IReadOnlyList<AegisCompatAssertionDto> assertions,
        int maximum,
        CancellationToken cancellationToken = default)
    {
        return MutateAsync(
            storeId,
            authorizationModelId,
            current => InMemoryAssertionRepository.Distinct(current.Concat(assertions)),
            maximum,
            cancellationToken);
    }

    public async Task PurgeStoreAsync(string storeId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("DELETE FROM assertion_sets WHERE store_id = @store_id;", connection);
        command.Parameters.AddWithValue("store_id", NpgsqlDbType.Text, storeId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<AssertionSetSnapshot> MutateAsync(
        string storeId,
        string authorizationModelId,
        Func<IReadOnlyList<AegisCompatAssertionDto>, IReadOnlyList<AegisCompatAssertionDto>> mutation,
        int? maximum,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await AcquireMutationLockAsync(connection, transaction, storeId, authorizationModelId, cancellationToken);
        var current = await ReadAsync(connection, transaction, storeId, authorizationModelId, forUpdate: true, cancellationToken)
            ?? new AssertionSetSnapshot(storeId, authorizationModelId, 0, []);
        var assertions = mutation(current.Assertions);
        if (maximum is not null && assertions.Count > maximum.Value)
        {
            throw new AssertionSetCapacityExceededException(maximum.Value);
        }

        var snapshot = new AssertionSetSnapshot(storeId, authorizationModelId, current.Revision + 1, assertions);
        const string sql = @"
INSERT INTO assertion_sets (store_id, authorization_model_id, revision, assertions_json, updated_at)
VALUES (@store_id, @authorization_model_id, @revision, @assertions_json, @updated_at)
ON CONFLICT (store_id, authorization_model_id) DO UPDATE SET
    revision = EXCLUDED.revision,
    assertions_json = EXCLUDED.assertions_json,
    updated_at = EXCLUDED.updated_at;";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("store_id", NpgsqlDbType.Text, storeId);
        command.Parameters.AddWithValue("authorization_model_id", NpgsqlDbType.Text, authorizationModelId);
        command.Parameters.AddWithValue("revision", NpgsqlDbType.Bigint, snapshot.Revision);
        command.Parameters.AddWithValue("assertions_json", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(assertions, JsonOptions));
        command.Parameters.AddWithValue("updated_at", NpgsqlDbType.TimestampTz, DateTimeOffset.UtcNow);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return snapshot;
    }

    private static async Task AcquireMutationLockAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string storeId,
        string authorizationModelId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT pg_advisory_xact_lock(hashtextextended(@scope, 0));",
            connection,
            transaction);
        command.Parameters.AddWithValue("scope", NpgsqlDbType.Text, $"assertions:{storeId}:{authorizationModelId}");
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<AssertionSetSnapshot?> ReadAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string storeId,
        string authorizationModelId,
        bool forUpdate,
        CancellationToken cancellationToken)
    {
        var sql = @"
SELECT revision, assertions_json
FROM assertion_sets
WHERE store_id = @store_id AND authorization_model_id = @authorization_model_id" + (forUpdate ? " FOR UPDATE;" : ";");
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("store_id", NpgsqlDbType.Text, storeId);
        command.Parameters.AddWithValue("authorization_model_id", NpgsqlDbType.Text, authorizationModelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var assertions = JsonSerializer.Deserialize<List<AegisCompatAssertionDto>>(reader.GetString(1), JsonOptions) ?? [];
        return new AssertionSetSnapshot(storeId, authorizationModelId, reader.GetInt64(0), assertions);
    }
}
