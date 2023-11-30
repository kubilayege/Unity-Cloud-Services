using Npgsql;

public class PostgreClientConnection
{
    private readonly NpgsqlDataSource _dataSource;
    
    public PostgreClientConnection()
    {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder();
        connectionStringBuilder.Host = "host.adress";
        connectionStringBuilder.Username = "username";
        connectionStringBuilder.Password = "password";
        connectionStringBuilder.Database = "dbName";
        connectionStringBuilder.Port = 33333;
        _dataSource = NpgsqlDataSource.Create(connectionStringBuilder);
    }

    public async Task<string> Query(string query)
    {
        await using var cmd = _dataSource.CreateCommand(query);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            return reader.GetString(0);
        }
        return "";
    }

    public async Task<int> Execute(string query)
    {
        await using var cmd = _dataSource.CreateCommand(query);
        
        return await cmd.ExecuteNonQueryAsync();
    }
}
