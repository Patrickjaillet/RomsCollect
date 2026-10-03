// SPDX-License-Identifier: GPL-3.0-or-later
using Dapper;
using RomsCollect.Models;

namespace RomsCollect.Services.Database;

/// <summary>CRUD access to the GameOwnership table (the "Personal" catalog tab).</summary>
public sealed class GameOwnershipRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public GameOwnershipRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public int Add(GameOwnership ownership)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            INSERT INTO GameOwnership (
                GameId, Condition, Owner, PurchaseDate, PurchaseStore, StorageDevice, StorageSlot,
                PurchasePrice, CurrentValue, Completed, CompletedDate, Notes, Rating,
                CollectionStatus, SortIndex, Quantity, Location, Completeness, HasBox, HasManual
            ) VALUES (
                @GameId, @Condition, @Owner, @PurchaseDate, @PurchaseStore, @StorageDevice, @StorageSlot,
                @PurchasePrice, @CurrentValue, @Completed, @CompletedDate, @Notes, @Rating,
                @CollectionStatus, @SortIndex, @Quantity, @Location, @Completeness, @HasBox, @HasManual
            );
            SELECT last_insert_rowid();
            """;
        return connection.ExecuteScalar<int>(sql, ownership);
    }

    public GameOwnership? GetById(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT * FROM GameOwnership WHERE Id = @Id;";
        return connection.QuerySingleOrDefault<GameOwnership>(sql, new { Id = id });
    }

    public IReadOnlyList<GameOwnership> GetByGame(int gameId)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT * FROM GameOwnership WHERE GameId = @GameId ORDER BY SortIndex;";
        return connection.Query<GameOwnership>(sql, new { GameId = gameId }).ToList();
    }

    /// <summary>
    /// The completeness (Loose/CIB/New) of each game's first ownership
    /// record (by <c>SortIndex</c>), for the whole collection in a single
    /// query — used by the advanced search filter, which would otherwise
    /// need one query per game. A game with several ownership rows (a
    /// duplicate or re-edition) is represented by its first row only; a
    /// game with no ownership row or a null/blank Completeness is omitted.
    /// </summary>
    public IReadOnlyDictionary<int, string> GetFirstCompletenessByGame()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            SELECT GameId, Completeness
            FROM GameOwnership
            WHERE Completeness IS NOT NULL AND Completeness <> ''
            AND Id = (
                SELECT Id FROM GameOwnership AS o2
                WHERE o2.GameId = GameOwnership.GameId
                ORDER BY SortIndex
                LIMIT 1
            );
            """;
        return connection.Query(sql)
            .ToDictionary(row => (int)row.GameId, row => (string)row.Completeness);
    }

    public void Update(GameOwnership ownership)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            UPDATE GameOwnership SET
                Condition = @Condition, Owner = @Owner, PurchaseDate = @PurchaseDate,
                PurchaseStore = @PurchaseStore, StorageDevice = @StorageDevice, StorageSlot = @StorageSlot,
                PurchasePrice = @PurchasePrice, CurrentValue = @CurrentValue, Completed = @Completed,
                CompletedDate = @CompletedDate, Notes = @Notes, Rating = @Rating,
                CollectionStatus = @CollectionStatus, SortIndex = @SortIndex, Quantity = @Quantity,
                Location = @Location, Completeness = @Completeness, HasBox = @HasBox, HasManual = @HasManual
            WHERE Id = @Id;
            """;
        connection.Execute(sql, ownership);
    }

    public void Delete(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "DELETE FROM GameOwnership WHERE Id = @Id;";
        connection.Execute(sql, new { Id = id });
    }
}
