// SPDX-License-Identifier: GPL-3.0-or-later
using Dapper;
using RomsCollect.Models;

namespace RomsCollect.Services.Database;

/// <summary>
/// CRUD access to the GameDownloadLinks table. RomsCollect never validates
/// or fetches these URLs; they are plain user-entered text.
/// </summary>
public sealed class GameDownloadLinkRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public GameDownloadLinkRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public int Add(GameDownloadLink link)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            INSERT INTO GameDownloadLinks (GameId, Label, Url, SortOrder)
            VALUES (@GameId, @Label, @Url, @SortOrder);
            SELECT last_insert_rowid();
            """;
        return connection.ExecuteScalar<int>(sql, link);
    }

    public GameDownloadLink? GetById(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT * FROM GameDownloadLinks WHERE Id = @Id;";
        return connection.QuerySingleOrDefault<GameDownloadLink>(sql, new { Id = id });
    }

    public IReadOnlyList<GameDownloadLink> GetByGame(int gameId)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT * FROM GameDownloadLinks WHERE GameId = @GameId ORDER BY SortOrder;";
        return connection.Query<GameDownloadLink>(sql, new { GameId = gameId }).ToList();
    }

    public void Update(GameDownloadLink link)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            UPDATE GameDownloadLinks
            SET Label = @Label, Url = @Url, SortOrder = @SortOrder
            WHERE Id = @Id;
            """;
        connection.Execute(sql, link);
    }

    public void Delete(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "DELETE FROM GameDownloadLinks WHERE Id = @Id;";
        connection.Execute(sql, new { Id = id });
    }
}
