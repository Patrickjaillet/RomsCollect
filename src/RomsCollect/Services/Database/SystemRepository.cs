// SPDX-License-Identifier: GPL-3.0-or-later
using Dapper;
using RomsCollect.Models;

namespace RomsCollect.Services.Database;

/// <summary>CRUD access to the Systems table.</summary>
public sealed class SystemRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SystemRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public int Add(GameSystem system)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            INSERT INTO Systems (Key, Name, RomExtensions, SortOrder, IconPath)
            VALUES (@Key, @Name, @RomExtensions, @SortOrder, @IconPath);
            SELECT last_insert_rowid();
            """;
        return connection.ExecuteScalar<int>(sql, system);
    }

    public GameSystem? GetById(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT * FROM Systems WHERE Id = @Id;";
        return connection.QuerySingleOrDefault<GameSystem>(sql, new { Id = id });
    }

    public GameSystem? GetByKey(string key)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT * FROM Systems WHERE Key = @Key;";
        return connection.QuerySingleOrDefault<GameSystem>(sql, new { Key = key });
    }

    public IReadOnlyList<GameSystem> GetAll()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT * FROM Systems ORDER BY SortOrder, Name;";
        return connection.Query<GameSystem>(sql).ToList();
    }

    public void Update(GameSystem system)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            UPDATE Systems
            SET Key = @Key, Name = @Name, RomExtensions = @RomExtensions, SortOrder = @SortOrder, IconPath = @IconPath
            WHERE Id = @Id;
            """;
        connection.Execute(sql, system);
    }

    /// <summary>
    /// Number of games currently attached to a system. Used to warn the
    /// user, with an exact count, before a deletion that would cascade
    /// (see <c>Games.SystemId ... ON DELETE CASCADE</c>).
    /// </summary>
    public int CountGamesForSystem(int systemId)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT COUNT(*) FROM Games WHERE SystemId = @SystemId;";
        return connection.ExecuteScalar<int>(sql, new { SystemId = systemId });
    }

    public void Delete(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "DELETE FROM Systems WHERE Id = @Id;";
        connection.Execute(sql, new { Id = id });
    }

    /// <summary>
    /// Reassigns every game currently attached to <paramref name="fromSystemId"/>
    /// to <paramref name="toSystemId"/>, then deletes the now-empty source
    /// system. Used as the non-destructive alternative to a cascading
    /// delete when the user chooses to keep the games instead of removing
    /// them.
    /// </summary>
    public void ReassignGamesAndDelete(int fromSystemId, int toSystemId)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        using var transaction = connection.BeginTransaction();

        connection.Execute(
            "UPDATE Games SET SystemId = @ToSystemId WHERE SystemId = @FromSystemId;",
            new { ToSystemId = toSystemId, FromSystemId = fromSystemId },
            transaction);

        connection.Execute(
            "DELETE FROM Systems WHERE Id = @Id;",
            new { Id = fromSystemId },
            transaction);

        transaction.Commit();
    }
}
