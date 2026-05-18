using Aegis.Application.Interfaces;
using Aegis.Contracts.Administration;
using Aegis.Domain.Entities;
using Aegis.Domain.Repositories;
using Npgsql;

namespace Aegis.Infrastructure.Persistence
{
    public sealed class PostgresStoreRegistry : IStoreRegistry, IAuthorizationModelRegistry, IStoreRepository, IAuthorizationModelRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        public PostgresStoreRegistry(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<StoreDto> CreateAsync(string name, CancellationToken cancellationToken = default)
        {
            var store = Store.Create(name);
            await ((IStoreRepository)this).AddAsync(store, cancellationToken);
            return new StoreDto(store.Id, store.Name, store.CreatedAt, store.UpdatedAt);
        }

        public async Task<IReadOnlyList<StoreDto>> ListAsync(CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = "SELECT id, name, created_at, updated_at FROM stores ORDER BY created_at DESC;";

            var items = new List<StoreDto>();
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new StoreDto(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetFieldValue<DateTimeOffset>(2),
                    reader.GetFieldValue<DateTimeOffset>(3)));
            }

            return items;
        }

        public async Task<StoreDto?> GetAsync(string storeId, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = "SELECT id, name, created_at, updated_at FROM stores WHERE id = @id;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", storeId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new StoreDto(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetFieldValue<DateTimeOffset>(2),
                reader.GetFieldValue<DateTimeOffset>(3));
        }

        public async Task<bool> DeleteAsync(string storeId, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = "DELETE FROM stores WHERE id = @id;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", storeId);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }

        Task IStoreRepository.AddAsync(Store store, CancellationToken cancellationToken)
        {
            return ExecuteAsync(async connection =>
            {
                const string sql = @"INSERT INTO stores (id, name, created_at, updated_at)
                                     VALUES (@id, @name, @created_at, @updated_at)
                                     ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, updated_at = EXCLUDED.updated_at;";
                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("id", store.Id);
                command.Parameters.AddWithValue("name", store.Name);
                command.Parameters.AddWithValue("created_at", store.CreatedAt);
                command.Parameters.AddWithValue("updated_at", store.UpdatedAt);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }, cancellationToken);
        }

        async Task<Store?> IStoreRepository.GetByIdAsync(string storeId, CancellationToken cancellationToken)
        {
            var dto = await GetAsync(storeId, cancellationToken);
            return dto is null ? null : Store.Rehydrate(dto.Id, dto.Name, dto.CreatedAt, dto.UpdatedAt);
        }

        async Task<IReadOnlyList<Store>> IStoreRepository.ListAsync(CancellationToken cancellationToken)
        {
            var items = await ListAsync(cancellationToken);
            return items.Select(x => Store.Rehydrate(x.Id, x.Name, x.CreatedAt, x.UpdatedAt)).ToList();
        }

        Task<bool> IStoreRepository.DeleteAsync(Store store, CancellationToken cancellationToken)
        {
            return DeleteAsync(store.Id, cancellationToken);
        }

        public async Task<AuthorizationModelDto> CreateAsync(string storeId, string schemaVersion, string model, CancellationToken cancellationToken = default)
        {
            var authorizationModel = AuthorizationModel.Create(storeId, schemaVersion, model);
            await ((IAuthorizationModelRepository)this).AddAsync(authorizationModel, cancellationToken);
            return new AuthorizationModelDto(authorizationModel.Id, authorizationModel.StoreId, authorizationModel.SchemaVersion, authorizationModel.Model, authorizationModel.CreatedAt);
        }

        public async Task<IReadOnlyList<AuthorizationModelDto>> ListAsync(string storeId, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = "SELECT id, store_id, schema_version, model, created_at FROM authorization_models WHERE store_id = @store_id ORDER BY created_at DESC;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("store_id", storeId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<AuthorizationModelDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new AuthorizationModelDto(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetFieldValue<DateTimeOffset>(4)));
            }

            return items;
        }

        public async Task<AuthorizationModelDto?> GetLatestAsync(string storeId, CancellationToken cancellationToken = default)
        {
            var items = await ListAsync(storeId, cancellationToken);
            return items.FirstOrDefault();
        }

        public async Task<AuthorizationModelDto?> GetByIdAsync(string storeId, string authorizationModelId, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = "SELECT id, store_id, schema_version, model, created_at FROM authorization_models WHERE store_id = @store_id AND id = @id;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("store_id", storeId);
            command.Parameters.AddWithValue("id", authorizationModelId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new AuthorizationModelDto(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetFieldValue<DateTimeOffset>(4));
        }

        public async Task<AuthorizationModelDto?> UpdateAsync(string storeId, string authorizationModelId, string schemaVersion, string model, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = @"UPDATE authorization_models
                                 SET schema_version = @schema_version, model = @model
                                 WHERE store_id = @store_id AND id = @id
                                 RETURNING id, store_id, schema_version, model, created_at;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("store_id", storeId);
            command.Parameters.AddWithValue("id", authorizationModelId);
            command.Parameters.AddWithValue("schema_version", schemaVersion);
            command.Parameters.AddWithValue("model", model);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new AuthorizationModelDto(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetFieldValue<DateTimeOffset>(4));
        }

        public async Task<bool> DeleteAsync(string storeId, string authorizationModelId, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = "DELETE FROM authorization_models WHERE store_id = @store_id AND id = @id;";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("store_id", storeId);
            command.Parameters.AddWithValue("id", authorizationModelId);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }

        Task IAuthorizationModelRepository.AddAsync(AuthorizationModel authorizationModel, CancellationToken cancellationToken)
        {
            return ExecuteAsync(async connection =>
            {
                const string sql = @"INSERT INTO authorization_models (id, store_id, schema_version, model, created_at)
                                     VALUES (@id, @store_id, @schema_version, @model, @created_at)
                                     ON CONFLICT (id) DO UPDATE SET schema_version = EXCLUDED.schema_version, model = EXCLUDED.model;";
                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("id", authorizationModel.Id);
                command.Parameters.AddWithValue("store_id", authorizationModel.StoreId);
                command.Parameters.AddWithValue("schema_version", authorizationModel.SchemaVersion);
                command.Parameters.AddWithValue("model", authorizationModel.Model);
                command.Parameters.AddWithValue("created_at", authorizationModel.CreatedAt);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }, cancellationToken);
        }

        async Task<IReadOnlyList<AuthorizationModel>> IAuthorizationModelRepository.ListByStoreAsync(string storeId, CancellationToken cancellationToken)
        {
            var items = await ListAsync(storeId, cancellationToken);
            return items.Select(x => AuthorizationModel.Rehydrate(x.Id, x.StoreId, x.SchemaVersion, x.Model, x.CreatedAt)).ToList();
        }

        async Task<AuthorizationModel?> IAuthorizationModelRepository.GetLatestByStoreAsync(string storeId, CancellationToken cancellationToken)
        {
            var dto = await GetLatestAsync(storeId, cancellationToken);
            return dto is null ? null : AuthorizationModel.Rehydrate(dto.Id, dto.StoreId, dto.SchemaVersion, dto.Model, dto.CreatedAt);
        }

        async Task<AuthorizationModel?> IAuthorizationModelRepository.GetByIdAsync(string storeId, string authorizationModelId, CancellationToken cancellationToken)
        {
            var dto = await GetByIdAsync(storeId, authorizationModelId, cancellationToken);
            return dto is null ? null : AuthorizationModel.Rehydrate(dto.Id, dto.StoreId, dto.SchemaVersion, dto.Model, dto.CreatedAt);
        }

        async Task<AuthorizationModel?> IAuthorizationModelRepository.UpdateAsync(AuthorizationModel authorizationModel, CancellationToken cancellationToken)
        {
            var dto = await UpdateAsync(authorizationModel.StoreId, authorizationModel.Id, authorizationModel.SchemaVersion, authorizationModel.Model, cancellationToken);
            return dto is null ? null : AuthorizationModel.Rehydrate(dto.Id, dto.StoreId, dto.SchemaVersion, dto.Model, dto.CreatedAt);
        }

        Task<bool> IAuthorizationModelRepository.DeleteAsync(AuthorizationModel authorizationModel, CancellationToken cancellationToken)
        {
            return DeleteAsync(authorizationModel.StoreId, authorizationModel.Id, cancellationToken);
        }

        private Task ExecuteAsync(Func<NpgsqlConnection, Task> action, CancellationToken cancellationToken)
        {
            return ExecuteAsyncInternal(action, cancellationToken);
        }

        private async Task ExecuteAsyncInternal(Func<NpgsqlConnection, Task> action, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await action(connection);
        }
    }
}
