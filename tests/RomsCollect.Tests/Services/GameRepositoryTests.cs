// SPDX-License-Identifier: GPL-3.0-or-later
using RomsCollect.Models;
using RomsCollect.Services.Database;

namespace RomsCollect.Tests.Services;

public class GameRepositoryTests : SqliteRepositoryTestBase
{
    private readonly GameRepository _games;
    private readonly SystemRepository _systems;
    private readonly int _systemId;

    public GameRepositoryTests()
    {
        _games = new GameRepository(ConnectionFactory);
        _systems = new SystemRepository(ConnectionFactory);
        _systemId = _systems.Add(new GameSystem { Key = "snes", Name = "Super Nintendo Entertainment System" });
    }

    private Game NewGame(string title = "Chrono Trigger", string? romPath = "Chrono Trigger.sfc") => new()
    {
        SystemId = _systemId,
        Title = title,
        SortTitle = title,
        RomPath = romPath,
        DateAdded = DateTime.UtcNow,
        DateModified = DateTime.UtcNow,
    };

    [Fact]
    public void Add_ThenGetById_ReturnsTheSameGame()
    {
        var id = _games.Add(NewGame());

        var game = _games.GetById(id);

        Assert.NotNull(game);
        Assert.Equal("Chrono Trigger", game!.Title);
        Assert.Equal(_systemId, game.SystemId);
        Assert.Equal("Chrono Trigger.sfc", game.RomPath);
    }

    [Fact]
    public void GetBySystem_ReturnsOnlyGamesOfThatSystem()
    {
        var otherSystemId = _systems.Add(new GameSystem { Key = "nes", Name = "NES" });
        _games.Add(NewGame("Super Mario World", "smw.sfc"));
        _games.Add(new Game { SystemId = otherSystemId, Title = "Zelda", SortTitle = "Zelda", DateAdded = DateTime.UtcNow, DateModified = DateTime.UtcNow });

        var snesGames = _games.GetBySystem(_systemId);

        Assert.Single(snesGames);
        Assert.Equal("Super Mario World", snesGames[0].Title);
    }

    [Fact]
    public void GetBySystemAndRomPath_FindsExistingEntry_ForDuplicateDetection()
    {
        _games.Add(NewGame(romPath: "Secret of Mana.sfc"));

        var found = _games.GetBySystemAndRomPath(_systemId, "Secret of Mana.sfc");
        var notFound = _games.GetBySystemAndRomPath(_systemId, "Unknown.sfc");

        Assert.NotNull(found);
        Assert.Null(notFound);
    }

    [Fact]
    public void Update_PersistsChanges()
    {
        var id = _games.Add(NewGame());
        var game = _games.GetById(id)!;
        game.Rating = 9;
        game.IsFavorite = true;
        game.DateModified = DateTime.UtcNow;

        _games.Update(game);

        var updated = _games.GetById(id)!;
        Assert.Equal(9, updated.Rating);
        Assert.True(updated.IsFavorite);
    }

    [Fact]
    public void Add_ThenUpdate_PersistsPlayModeAndIsPortable()
    {
        var id = _games.Add(NewGame());
        var game = _games.GetById(id)!;
        Assert.Null(game.PlayMode);
        Assert.False(game.IsPortable);

        game.PlayMode = "Multiplayer";
        game.IsPortable = true;
        game.DateModified = DateTime.UtcNow;
        _games.Update(game);

        var updated = _games.GetById(id)!;
        Assert.Equal("Multiplayer", updated.PlayMode);
        Assert.True(updated.IsPortable);
    }

    [Fact]
    public void Delete_RemovesTheGame()
    {
        var id = _games.Add(NewGame());

        _games.Delete(id);

        Assert.Null(_games.GetById(id));
    }

    [Fact]
    public void SetDevelopers_ThenGetDeveloperNames_RoundTrips_AndReusesExistingLookupRows()
    {
        var firstGameId = _games.Add(NewGame("Game A", "a.sfc"));
        var secondGameId = _games.Add(NewGame("Game B", "b.sfc"));

        _games.SetDevelopers(firstGameId, ["Square", "Nintendo"]);
        _games.SetDevelopers(secondGameId, ["Square"]);

        Assert.Equal(["Nintendo", "Square"], _games.GetDeveloperNames(firstGameId));
        Assert.Equal(["Square"], _games.GetDeveloperNames(secondGameId));
    }

    [Fact]
    public void SetGenres_ReplacesPreviousLinks()
    {
        var gameId = _games.Add(NewGame());
        _games.SetGenres(gameId, ["RPG", "Adventure"]);

        _games.SetGenres(gameId, ["Platformer"]);

        Assert.Equal(["Platformer"], _games.GetGenreNames(gameId));
    }

    [Fact]
    public void GetAllUsedGenreNames_ReturnsDistinctNamesAcrossAllGames_SortedAlphabetically()
    {
        var firstGameId = _games.Add(NewGame("Chrono Trigger", "ct.sfc"));
        var secondGameId = _games.Add(NewGame("Super Metroid", "sm.sfc"));
        _games.SetGenres(firstGameId, ["RPG", "Adventure"]);
        _games.SetGenres(secondGameId, ["Adventure", "Platformer"]);

        Assert.Equal(["Adventure", "Platformer", "RPG"], _games.GetAllUsedGenreNames());
    }

    [Fact]
    public void GetGenreNamesByGame_ReturnsEachGamesGenres_InASingleBatch()
    {
        var firstGameId = _games.Add(NewGame("Chrono Trigger", "ct.sfc"));
        var secondGameId = _games.Add(NewGame("Super Metroid", "sm.sfc"));
        _games.SetGenres(firstGameId, ["RPG"]);
        _games.SetGenres(secondGameId, ["Platformer", "Action"]);

        var genresByGame = _games.GetGenreNamesByGame();

        Assert.Equal(["RPG"], genresByGame[firstGameId]);
        Assert.Equal(["Action", "Platformer"], genresByGame[secondGameId].OrderBy(g => g));
    }

    [Fact]
    public void SetTags_IgnoresBlankAndDuplicateEntries()
    {
        var gameId = _games.Add(NewGame());

        _games.SetTags(gameId, ["Favorite", "  ", "favorite", "Multiplayer"]);

        Assert.Equal(["Favorite", "Multiplayer"], _games.GetTagNames(gameId));
    }
}
