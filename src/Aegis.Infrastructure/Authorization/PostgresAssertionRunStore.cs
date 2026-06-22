using Aegis.Application.Interfaces;
using Aegis.Contracts.Compatibility;
using Npgsql;
using NpgsqlTypes;
using System.Text.Json;

namespace Aegis.Infrastructure.Authorization
{
    public sealed class PostgresAssertionRunStore : IAssertionRunStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly NpgsqlDataSource _dataSource;

        public PostgresAssertionRunStore(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task SaveAsync(AegisAssertionRunRecordDto record, CancellationToken cancellationToken = default)
        {
            const string sql = @"
INSERT INTO assertion_run_records (
    run_id,
    store_id,
    authorization_model_id,
    started_at,
    completed_at,
    total,
    passed,
    failed,
    results_json)
VALUES (
    @run_id,
    @store_id,
    @authorization_model_id,
    @started_at,
    @completed_at,
    @total,
    @passed,
    @failed,
    @results_json)
ON CONFLICT (run_id) DO UPDATE SET
    store_id = EXCLUDED.store_id,
    authorization_model_id = EXCLUDED.authorization_model_id,
    started_at = EXCLUDED.started_at,
    completed_at = EXCLUDED.completed_at,
    total = EXCLUDED.total,
    passed = EXCLUDED.passed,
    failed = EXCLUDED.failed,
    results_json = EXCLUDED.results_json;";

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("run_id", NpgsqlDbType.Text, record.RunId);
            command.Parameters.AddWithValue("store_id", NpgsqlDbType.Text, record.StoreId);
            command.Parameters.AddWithValue("authorization_model_id", NpgsqlDbType.Text, record.AuthorizationModelId);
            command.Parameters.AddWithValue("started_at", NpgsqlDbType.TimestampTz, record.StartedAt);
            command.Parameters.AddWithValue("completed_at", NpgsqlDbType.TimestampTz, record.CompletedAt);
            command.Parameters.AddWithValue("total", NpgsqlDbType.Integer, record.Summary.Total);
            command.Parameters.AddWithValue("passed", NpgsqlDbType.Integer, record.Summary.Passed);
            command.Parameters.AddWithValue("failed", NpgsqlDbType.Integer, record.Summary.Failed);
            command.Parameters.AddWithValue("results_json", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(record.Results, JsonOptions));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<AegisAssertionRunRecordDto>> ListByModelAsync(
            string storeId,
            string authorizationModelId,
            int limit = 25,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT run_id, store_id, authorization_model_id, started_at, completed_at, total, passed, failed, results_json
FROM assertion_run_records
WHERE store_id = @store_id
  AND authorization_model_id = @authorization_model_id
ORDER BY started_at DESC
LIMIT @limit;";

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("store_id", NpgsqlDbType.Text, storeId);
            command.Parameters.AddWithValue("authorization_model_id", NpgsqlDbType.Text, authorizationModelId);
            command.Parameters.AddWithValue("limit", NpgsqlDbType.Integer, limit);

            var records = new List<AegisAssertionRunRecordDto>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                records.Add(ReadRecord(reader));
            }

            return records;
        }

        public async Task<AegisAssertionRunRecordDto?> GetAsync(
            string storeId,
            string runId,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT run_id, store_id, authorization_model_id, started_at, completed_at, total, passed, failed, results_json
FROM assertion_run_records
WHERE store_id = @store_id
  AND run_id = @run_id;";

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("store_id", NpgsqlDbType.Text, storeId);
            command.Parameters.AddWithValue("run_id", NpgsqlDbType.Text, runId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? ReadRecord(reader) : null;
        }

        public async Task PurgeStoreAsync(string storeId, CancellationToken cancellationToken = default)
        {
            const string sql = "DELETE FROM assertion_run_records WHERE store_id = @store_id;";

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("store_id", NpgsqlDbType.Text, storeId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static AegisAssertionRunRecordDto ReadRecord(NpgsqlDataReader reader)
        {
            var results = JsonSerializer.Deserialize<List<AegisAssertionRunResultItemDto>>(reader.GetString(8), JsonOptions)
                ?? [];

            return new AegisAssertionRunRecordDto(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetFieldValue<DateTimeOffset>(3),
                reader.GetFieldValue<DateTimeOffset>(4),
                new AegisAssertionRunSummaryDto(reader.GetInt32(5), reader.GetInt32(6), reader.GetInt32(7)),
                results);
        }
    }
}
