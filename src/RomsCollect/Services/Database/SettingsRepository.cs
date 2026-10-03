// SPDX-License-Identifier: GPL-3.0-or-later
using Dapper;

namespace RomsCollect.Services.Database;

/// <summary>CRUD access to the Settings key/value table.</summary>
public sealed class SettingsRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SettingsRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public string? Get(string key)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT Value FROM Settings WHERE Key = @Key;";
        return connection.QuerySingleOrDefault<string>(sql, new { Key = key });
    }

    public IReadOnlyDictionary<string, string> GetAll()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT Key, Value FROM Settings;";
        return connection.Query(sql)
            .ToDictionary(row => (string)row.Key, row => (string)row.Value);
    }

    public void Set(string key, string value)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            INSERT INTO Settings (Key, Value) VALUES (@Key, @Value)
            ON CONFLICT (Key) DO UPDATE SET Value = @Value;
            """;
        connection.Execute(sql, new { Key = key, Value = value });
    }

    public void Delete(string key)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "DELETE FROM Settings WHERE Key = @Key;";
        connection.Execute(sql, new { Key = key });
    }
}
