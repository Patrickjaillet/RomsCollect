// SPDX-License-Identifier: GPL-3.0-or-later
using RomsCollect.Models;
using RomsCollect.Services.Database;

namespace RomsCollect.Tests.Services;

public class GameDownloadLinkRepositoryTests : SqliteRepositoryTestBase
{
    private readonly GameDownloadLinkRepository _links;
    private readonly int _gameId;

    public GameDownloadLinkRepositoryTests()
    {
        var systems = new SystemRepository(ConnectionFactory);
        var games = new GameRepository(ConnectionFactory);
        var systemId = systems.Add(new GameSystem { Key = "amiga1200", Name = "Commodore Amiga" });
        _gameId = games.Add(new Game
        {
            SystemId = systemId,
            Title = "Lemmings",
            SortTitle = "Lemmings",
            DateAdded = DateTime.UtcNow,
            DateModified = DateTime.UtcNow,
        });

        _links = new GameDownloadLinkRepository(ConnectionFactory);
    }

    [Fact]
    public void Add_ThenGetById_ReturnsTheSameLink_AsPlainStaticText()
    {
        var id = _links.Add(new GameDownloadLink { GameId = _gameId, Label = "Archive.org mirror", Url = "https://example.invalid/lemmings.zip" });

        var link = _links.GetById(id);

        Assert.NotNull(link);
        Assert.Equal("Archive.org mirror", link!.Label);
        Assert.Equal("https://example.invalid/lemmings.zip", link.Url);
    }

    [Fact]
    public void GetByGame_ReturnsLinksOrderedBySortOrder()
    {
        _links.Add(new GameDownloadLink { GameId = _gameId, Label = "Mirror B", Url = "https://example.invalid/b", SortOrder = 1 });
        _links.Add(new GameDownloadLink { GameId = _gameId, Label = "Mirror A", Url = "https://example.invalid/a", SortOrder = 0 });

        var links = _links.GetByGame(_gameId);

        Assert.Equal(["Mirror A", "Mirror B"], links.Select(l => l.Label));
    }

    [Fact]
    public void Update_PersistsChanges()
    {
        var id = _links.Add(new GameDownloadLink { GameId = _gameId, Label = "Old label", Url = "https://example.invalid/old" });
        var link = _links.GetById(id)!;
        link.Label = "New label";
        link.Url = "https://example.invalid/new";

        _links.Update(link);

        var updated = _links.GetById(id)!;
        Assert.Equal("New label", updated.Label);
        Assert.Equal("https://example.invalid/new", updated.Url);
    }

    [Fact]
    public void Delete_RemovesTheLink()
    {
        var id = _links.Add(new GameDownloadLink { GameId = _gameId, Label = "Mirror", Url = "https://example.invalid/x" });

        _links.Delete(id);

        Assert.Null(_links.GetById(id));
    }
}
