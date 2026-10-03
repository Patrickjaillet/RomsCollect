// SPDX-License-Identifier: GPL-3.0-or-later
using Dapper;
using RomsCollect.Models;

namespace RomsCollect.Services.Database;

/// <summary>CRUD access to the PlayHistory table.</summary>
public sealed class PlayHistoryRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public PlayHistoryRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public int Add(PlayHistoryEntry entry)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            INSERT INTO PlayHistory (GameId, StartedAt, EndedAt, DurationSeconds)
            VALUES (@GameId, @StartedAt, @EndedAt, @DurationSeconds);
            SELECT last_insert_rowid();
            """;
        return connection.ExecuteScalar<int>(sql, entry);
    }

    public PlayHistoryEntry? GetById(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT * FROM PlayHistory WHERE Id = @Id;";
        return connection.QuerySingleOrDefault<PlayHistoryEntry>(sql, new { Id = id });
    }

    public IReadOnlyList<PlayHistoryEntry> GetByGame(int gameId)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT * FROM PlayHistory WHERE GameId = @GameId ORDER BY StartedAt DESC;";
        return connection.Query<PlayHistoryEntry>(sql, new { GameId = gameId }).ToList();
    }

    public void Update(PlayHistoryEntry entry)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            UPDATE PlayHistory
            SET EndedAt = @EndedAt, DurationSeconds = @DurationSeconds
            WHERE Id = @Id;
            """;
        connection.Execute(sql, entry);
    }

    public void Delete(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "DELETE FROM PlayHistory WHERE Id = @Id;";
        connection.Execute(sql, new { Id = id });
    }
}
