// SPDX-License-Identifier: GPL-3.0-or-later
using RomsCollect.Models;
using RomsCollect.Services.Database;

namespace RomsCollect.Tests.Services;

public class CollectionRepositoryTests : SqliteRepositoryTestBase
{
    private readonly CollectionRepository _collections;
    private readonly GameRepository _games;
    private readonly int _systemId;

    public CollectionRepositoryTests()
    {
        _collections = new CollectionRepository(ConnectionFactory);
        _games = new GameRepository(ConnectionFactory);
        var systems = new SystemRepository(ConnectionFactory);
        _systemId = systems.Add(new GameSystem { Key = "snes", Name = "SNES" });
    }

    private int AddGame(string title) => _games.Add(new Game
    {
        SystemId = _systemId,
        Title = title,
        SortTitle = title,
        DateAdded = DateTime.UtcNow,
        DateModified = DateTime.UtcNow,
    });

    [Fact]
    public void Add_ThenGetById_ReturnsTheSameCollection()
    {
        var id = _collections.Add(new Collection { Name = "Favorites" });

        var collection = _collections.GetById(id);

        Assert.NotNull(collection);
        Assert.Equal("Favorites", collection!.Name);
    }

    [Fact]
    public void AddGame_ThenGetGames_ReturnsLinkedGamesInSortOrder()
    {
        var collectionId = _collections.Add(new Collection { Name = "Speedrun list" });
        var gameA = AddGame("Game A");
        var gameB = AddGame("Game B");

        _collections.AddGame(collectionId, gameB, sortOrder: 0);
        _collections.AddGame(collectionId, gameA, sortOrder: 1);

        var games = _collections.GetGames(collectionId);

        Assert.Equal(["Game B", "Game A"], games.Select(g => g.Title));
    }

    [Fact]
    public void RemoveGame_UnlinksTheGameFromTheCollection()
    {
        var collectionId = _collections.Add(new Collection { Name = "Wishlist" });
        var gameId = AddGame("Game A");
        _collections.AddGame(collectionId, gameId);

        _collections.RemoveGame(collectionId, gameId);

        Assert.Empty(_collections.GetGames(collectionId));
    }

    [Fact]
    public void Update_PersistsChanges()
    {
        var id = _collections.Add(new Collection { Name = "Old name" });
        var collection = _collections.GetById(id)!;
        collection.Name = "New name";

        _collections.Update(collection);

        Assert.Equal("New name", _collections.GetById(id)!.Name);
    }

    [Fact]
    public void Delete_RemovesTheCollection_AndItsGameLinks()
    {
        var id = _collections.Add(new Collection { Name = "Temp" });
        var gameId = AddGame("Game A");
        _collections.AddGame(id, gameId);

        _collections.Delete(id);

        Assert.Null(_collections.GetById(id));
    }
}
