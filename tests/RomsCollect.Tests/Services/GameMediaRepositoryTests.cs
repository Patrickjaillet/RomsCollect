// SPDX-License-Identifier: GPL-3.0-or-later
using RomsCollect.Models;
using RomsCollect.Services.Database;

namespace RomsCollect.Tests.Services;

public class GameMediaRepositoryTests : SqliteRepositoryTestBase
{
    private readonly GameMediaRepository _media;
    private readonly int _gameId;

    public GameMediaRepositoryTests()
    {
        var systems = new SystemRepository(ConnectionFactory);
        var games = new GameRepository(ConnectionFactory);
        var systemId = systems.Add(new GameSystem { Key = "snes", Name = "SNES" });
        _gameId = games.Add(new Game
        {
            SystemId = systemId,
            Title = "Super Metroid",
            SortTitle = "Super Metroid",
            DateAdded = DateTime.UtcNow,
            DateModified = DateTime.UtcNow,
        });

        _media = new GameMediaRepository(ConnectionFactory);
    }

    [Fact]
    public void Add_ThenGetById_ReturnsTheSameMediaAsset()
    {
        var id = _media.Add(new GameMedia { GameId = _gameId, MediaType = "Box - Front", RelativePath = "super-metroid.png" });

        var asset = _media.GetById(id);

        Assert.NotNull(asset);
        Assert.Equal("Box - Front", asset!.MediaType);
    }

    [Fact]
    public void GetByGameAndType_ReturnsOnlyMatchingMediaType()
    {
        _media.Add(new GameMedia { GameId = _gameId, MediaType = "Box - Front", RelativePath = "front.png" });
        _media.Add(new GameMedia { GameId = _gameId, MediaType = "Screenshot - Gameplay", RelativePath = "gameplay1.png" });
        _media.Add(new GameMedia { GameId = _gameId, MediaType = "Screenshot - Gameplay", RelativePath = "gameplay2.png", SortOrder = 1 });

        var screenshots = _media.GetByGameAndType(_gameId, "Screenshot - Gameplay");

        Assert.Equal(2, screenshots.Count);
        Assert.All(screenshots, m => Assert.Equal("Screenshot - Gameplay", m.MediaType));
    }

    [Fact]
    public void Update_PersistsChanges()
    {
        var id = _media.Add(new GameMedia { GameId = _gameId, MediaType = "Video", RelativePath = "old.mp4" });
        var asset = _media.GetById(id)!;
        asset.RelativePath = "new.mp4";

        _media.Update(asset);

        Assert.Equal("new.mp4", _media.GetById(id)!.RelativePath);
    }

    [Fact]
    public void Delete_RemovesTheMediaAsset()
    {
        var id = _media.Add(new GameMedia { GameId = _gameId, MediaType = "Clear Logo", RelativePath = "logo.png" });

        _media.Delete(id);

        Assert.Null(_media.GetById(id));
    }
}
