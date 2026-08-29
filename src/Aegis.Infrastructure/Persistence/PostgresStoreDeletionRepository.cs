using Aegis.Application.Interfaces;
using Npgsql;

namespace Aegis.Infrastructure.Persistence;

public sealed class PostgresStoreDeletionRepository : IStoreDeletionRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresStoreDeletionRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<bool> DeleteAsync(
        string tenantId,
        string storeId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM stores WHERE tenant_id = @tenant_id AND id = @store_id;";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("store_id", storeId);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }
}
