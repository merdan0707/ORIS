using MiniHttpServer.ORM.Builders;
using MiniHttpServer.ORM.Mapping;
using Npgsql;

namespace MiniHttpServer.ORM;

public class OrmContext : IOrmContext
{
    private readonly string _connectionString;

    public OrmContext(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Строка подключения пуста.", nameof(connectionString));

        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<T>> GetAllAsync<T>(string tableName, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        var query = SqlQueryBuilder.BuildSelectAll(tableName);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(query.Query, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        return EntityMapper.MapAll<T>(reader);
    }

    public async Task<T?> GetByIdAsync<T>(string tableName, int id, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        var query = SqlQueryBuilder.BuildSelectById(tableName, id);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(query.Query, connection);
        foreach (var param in query.Parameters)
        {
            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        return EntityMapper.TryMap<T>(reader, out T? entity) ? entity : null;
    }

    public async Task<int> InsertAsync<T>(string tableName, T entity, CancellationToken cancellationToken = default)
        where T : class
    {
        var query = SqlQueryBuilder.BuildInsert(tableName, entity);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(query.Query, connection);

        foreach (var param in query.Parameters)
        {
            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
        }

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> UpdateAsync<T>(string tableName, int id, T entity,
        CancellationToken cancellationToken = default) where T : class
    {
        var query = SqlQueryBuilder.BuildUpdate(tableName, id, entity);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(query.Query, connection);

        foreach (var param in query.Parameters)
        {
            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
        }

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> DeleteAsync(string tableName, int id, CancellationToken cancellationToken = default)
    {
        var query = SqlQueryBuilder.BuildDelete(tableName, id);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(query.Query, connection);

        foreach (var param in query.Parameters)
        {
            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
        }

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}