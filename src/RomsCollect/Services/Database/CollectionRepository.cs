// SPDX-License-Identifier: GPL-3.0-or-later
using Dapper;
using RomsCollect.Models;

namespace RomsCollect.Services.Database;

/// <summary>CRUD access to the Collections table and its game membership links.</summary>
public sealed class CollectionRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public CollectionRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public int Add(Collection collection)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            INSERT INTO Collections (Name, SortOrder) VALUES (@Name, @SortOrder);
            SELECT last_insert_rowid();
            """;
        return connection.ExecuteScalar<int>(sql, collection);
    }

    public Collection? GetById(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT * FROM Collections WHERE Id = @Id;";
        return connection.QuerySingleOrDefault<Collection>(sql, new { Id = id });
    }

    public IReadOnlyList<Collection> GetAll()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT * FROM Collections ORDER BY SortOrder, Name;";
        return connection.Query<Collection>(sql).ToList();
    }

    public void Update(Collection collection)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "UPDATE Collections SET Name = @Name, SortOrder = @SortOrder WHERE Id = @Id;";
        connection.Execute(sql, collection);
    }

    public void Delete(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "DELETE FROM Collections WHERE Id = @Id;";
        connection.Execute(sql, new { Id = id });
    }

    public void AddGame(int collectionId, int gameId, int sortOrder = 0)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            INSERT INTO CollectionGames (CollectionId, GameId, SortOrder)
            VALUES (@CollectionId, @GameId, @SortOrder);
            """;
        connection.Execute(sql, new { CollectionId = collectionId, GameId = gameId, SortOrder = sortOrder });
    }

    public void RemoveGame(int collectionId, int gameId)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "DELETE FROM CollectionGames WHERE CollectionId = @CollectionId AND GameId = @GameId;";
        connection.Execute(sql, new { CollectionId = collectionId, GameId = gameId });
    }

    public IReadOnlyList<Game> GetGames(int collectionId)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            SELECT g.*
            FROM CollectionGames cg
            JOIN Games g ON g.Id = cg.GameId
            WHERE cg.CollectionId = @CollectionId
            ORDER BY cg.SortOrder;
            """;
        return connection.Query<Game>(sql, new { CollectionId = collectionId }).ToList();
    }
}
