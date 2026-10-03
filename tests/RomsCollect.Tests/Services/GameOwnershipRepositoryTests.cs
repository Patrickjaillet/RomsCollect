// SPDX-License-Identifier: GPL-3.0-or-later
using RomsCollect.Models;
using RomsCollect.Services.Database;

namespace RomsCollect.Tests.Services;

public class GameOwnershipRepositoryTests : SqliteRepositoryTestBase
{
    private readonly GameOwnershipRepository _ownership;
    private readonly int _gameId;

    public GameOwnershipRepositoryTests()
    {
        var systems = new SystemRepository(ConnectionFactory);
        var games = new GameRepository(ConnectionFactory);
        var systemId = systems.Add(new GameSystem { Key = "psx", Name = "PlayStation" });
        _gameId = games.Add(new Game
        {
            SystemId = systemId,
            Title = "Final Fantasy VII",
            SortTitle = "Final Fantasy VII",
            DateAdded = DateTime.UtcNow,
            DateModified = DateTime.UtcNow,
        });

        _ownership = new GameOwnershipRepository(ConnectionFactory);
    }

    private GameOwnership NewOwnership() => new()
    {
        GameId = _gameId,
        Condition = "Good",
        Completeness = "CIB",
        HasBox = true,
        HasManual = true,
        CollectionStatus = "In Collection",
        Quantity = 1,
        CurrentValue = 45.50m,
    };

    [Fact]
    public void Add_ThenGetById_ReturnsTheSameOwnershipRecord()
    {
        var id = _ownership.Add(NewOwnership());

        var record = _ownership.GetById(id);

        Assert.NotNull(record);
        Assert.Equal(_gameId, record!.GameId);
        Assert.Equal("CIB", record.Completeness);
        Assert.True(record.HasBox);
        Assert.Equal(45.50m, record.CurrentValue);
    }

    [Fact]
    public void GetByGame_SupportsMultipleCopiesOfTheSameGame()
    {
        _ownership.Add(NewOwnership());
        var copy1 = NewOwnership();
        copy1.SortIndex = 0;
        var copy2 = NewOwnership();
        copy2.SortIndex = 1;
        copy2.Completeness = "Loose";

        _ownership.Add(copy1);
        _ownership.Add(copy2);

        var records = _ownership.GetByGame(_gameId);

        Assert.Equal(3, records.Count);
    }

    [Fact]
    public void Update_PersistsChanges()
    {
        var id = _ownership.Add(NewOwnership());
        var record = _ownership.GetById(id)!;
        record.CurrentValue = 60m;
        record.Completed = true;

        _ownership.Update(record);

        var updated = _ownership.GetById(id)!;
        Assert.Equal(60m, updated.CurrentValue);
        Assert.True(updated.Completed);
    }

    [Fact]
    public void Delete_RemovesTheOwnershipRecord()
    {
        var id = _ownership.Add(NewOwnership());

        _ownership.Delete(id);

        Assert.Null(_ownership.GetById(id));
    }

    [Fact]
    public void GetFirstCompletenessByGame_ReturnsTheFirstRecordBySortIndex_WhenAGameHasSeveralCopies()
    {
        var firstCopy = NewOwnership();
        firstCopy.SortIndex = 0;
        firstCopy.Completeness = "Loose";
        var secondCopy = NewOwnership();
        secondCopy.SortIndex = 1;
        secondCopy.Completeness = "CIB";

        _ownership.Add(secondCopy);
        _ownership.Add(firstCopy);

        var completenessByGame = _ownership.GetFirstCompletenessByGame();

        Assert.Equal("Loose", completenessByGame[_gameId]);
    }

    [Fact]
    public void GetFirstCompletenessByGame_OmitsGamesWithNoOwnershipRecord_OrABlankCompleteness()
    {
        var ownershipWithoutCompleteness = NewOwnership();
        ownershipWithoutCompleteness.Completeness = null;
        _ownership.Add(ownershipWithoutCompleteness);

        var completenessByGame = _ownership.GetFirstCompletenessByGame();

        Assert.False(completenessByGame.ContainsKey(_gameId));
    }
}
