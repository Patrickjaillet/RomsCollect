// SPDX-License-Identifier: GPL-3.0-or-later
using RomsCollect.Models;
using RomsCollect.Services.Database;

namespace RomsCollect.Tests.Services;

public class PlayHistoryRepositoryTests : SqliteRepositoryTestBase
{
    private readonly PlayHistoryRepository _history;
    private readonly int _gameId;

    public PlayHistoryRepositoryTests()
    {
        var systems = new SystemRepository(ConnectionFactory);
        var games = new GameRepository(ConnectionFactory);
        var systemId = systems.Add(new GameSystem { Key = "megadrive", Name = "Sega Genesis" });
        _gameId = games.Add(new Game
        {
            SystemId = systemId,
            Title = "Sonic the Hedgehog",
            SortTitle = "Sonic the Hedgehog",
            DateAdded = DateTime.UtcNow,
            DateModified = DateTime.UtcNow,
        });

        _history = new PlayHistoryRepository(ConnectionFactory);
    }

    [Fact]
    public void Add_ThenGetById_ReturnsTheSameEntry()
    {
        var startedAt = DateTime.UtcNow;
        var id = _history.Add(new PlayHistoryEntry { GameId = _gameId, StartedAt = startedAt });

        var entry = _history.GetById(id);

        Assert.NotNull(entry);
        Assert.Equal(_gameId, entry!.GameId);
        Assert.Null(entry.EndedAt);
    }

    [Fact]
    public void Update_RecordsEndOfSession()
    {
        var startedAt = DateTime.UtcNow;
        var id = _history.Add(new PlayHistoryEntry { GameId = _gameId, StartedAt = startedAt });
        var entry = _history.GetById(id)!;
        entry.EndedAt = startedAt.AddMinutes(30);
        entry.DurationSeconds = 1800;

        _history.Update(entry);

        var updated = _history.GetById(id)!;
        Assert.NotNull(updated.EndedAt);
        Assert.Equal(1800, updated.DurationSeconds);
    }

    [Fact]
    public void GetByGame_ReturnsEntriesMostRecentFirst()
    {
        var older = DateTime.UtcNow.AddDays(-1);
        var newer = DateTime.UtcNow;
        _history.Add(new PlayHistoryEntry { GameId = _gameId, StartedAt = older });
        _history.Add(new PlayHistoryEntry { GameId = _gameId, StartedAt = newer });

        var entries = _history.GetByGame(_gameId);

        Assert.Equal(2, entries.Count);
        Assert.True(entries[0].StartedAt >= entries[1].StartedAt);
    }

    [Fact]
    public void Delete_RemovesTheEntry()
    {
        var id = _history.Add(new PlayHistoryEntry { GameId = _gameId, StartedAt = DateTime.UtcNow });

        _history.Delete(id);

        Assert.Null(_history.GetById(id));
    }
}
