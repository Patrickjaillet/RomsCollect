// SPDX-License-Identifier: GPL-3.0-or-later
using Dapper;
using RomsCollect.Models;

namespace RomsCollect.Services.Database;

/// <summary>CRUD access to the GameMedia table.</summary>
public sealed class GameMediaRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public GameMediaRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public int Add(GameMedia media)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            INSERT INTO GameMedia (GameId, MediaType, RelativePath, SortOrder)
            VALUES (@GameId, @MediaType, @RelativePath, @SortOrder);
            SELECT last_insert_rowid();
            """;
        return connection.ExecuteScalar<int>(sql, media);
    }

    public GameMedia? GetById(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT * FROM GameMedia WHERE Id = @Id;";
        return connection.QuerySingleOrDefault<GameMedia>(sql, new { Id = id });
    }

    public IReadOnlyList<GameMedia> GetByGame(int gameId)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT * FROM GameMedia WHERE GameId = @GameId ORDER BY MediaType, SortOrder;";
        return connection.Query<GameMedia>(sql, new { GameId = gameId }).ToList();
    }

    public IReadOnlyList<GameMedia> GetByGameAndType(int gameId, string mediaType)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            SELECT * FROM GameMedia
            WHERE GameId = @GameId AND MediaType = @MediaType
            ORDER BY SortOrder;
            """;
        return connection.Query<GameMedia>(sql, new { GameId = gameId, MediaType = mediaType }).ToList();
    }

    public void Update(GameMedia media)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            UPDATE GameMedia
            SET MediaType = @MediaType, RelativePath = @RelativePath, SortOrder = @SortOrder
            WHERE Id = @Id;
            """;
        connection.Execute(sql, media);
    }

    public void Delete(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "DELETE FROM GameMedia WHERE Id = @Id;";
        connection.Execute(sql, new { Id = id });
    }
}
