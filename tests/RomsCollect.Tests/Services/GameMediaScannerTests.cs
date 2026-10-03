// SPDX-License-Identifier: GPL-3.0-or-later
using RomsCollect.Models;
using RomsCollect.Services.Database;
using RomsCollect.Services.Media;

namespace RomsCollect.Tests.Services;

public class GameMediaScannerTests : SqliteRepositoryTestBase
{
    private readonly SystemRepository _systems;
    private readonly GameRepository _games;
    private readonly GameMediaRepository _media;
    private readonly string _mediaRootDirectory;
    private readonly int _systemId;
    private bool _mediaRootDeleted;

    public GameMediaScannerTests()
    {
        _systems = new SystemRepository(ConnectionFactory);
        _games = new GameRepository(ConnectionFactory);
        _media = new GameMediaRepository(ConnectionFactory);
        _mediaRootDirectory = Path.Combine(Path.GetTempPath(), "RomsCollectMediaTests_" + Guid.NewGuid());

        _systemId = _systems.Add(new GameSystem { Key = "snes", Name = "SNES" });
    }

    public override void Dispose()
    {
        if (!_mediaRootDeleted && Directory.Exists(_mediaRootDirectory))
        {
            Directory.Delete(_mediaRootDirectory, recursive: true);
            _mediaRootDeleted = true;
        }

        base.Dispose();
    }

    private int AddGame(string title, string romPath) => _games.Add(new Game
    {
        SystemId = _systemId,
        Title = title,
        SortTitle = title,
        RomPath = romPath,
        DateAdded = DateTime.UtcNow,
        DateModified = DateTime.UtcNow,
    });

    private void WriteMediaFile(string mediaType, string fileName)
    {
        var directory = Path.Combine(_mediaRootDirectory, "snes", mediaType);
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, fileName), []);
    }

    private GameMediaScanner CreateScanner() => new(_systems, _games, _media, _mediaRootDirectory);

    [Fact]
    public void ScanAndLink_LinksMediaFile_ToGameWithMatchingRomFileName()
    {
        AddGame("Super Metroid", "Super Metroid.sfc");
        WriteMediaFile("Box - Front", "Super Metroid.png");

        var linkedCount = CreateScanner().ScanAndLink();

        Assert.Equal(1, linkedCount);
        var gameId = _games.GetBySystem(_systemId)[0].Id;
        var media = _media.GetByGameAndType(gameId, "Box - Front");
        Assert.Single(media);
    }

    [Fact]
    public void ScanAndLink_SkipsMediaFile_WhenNoGameMatchesTheFileName()
    {
        WriteMediaFile("Box - Front", "Unknown Game.png");

        var linkedCount = CreateScanner().ScanAndLink();

        Assert.Equal(0, linkedCount);
    }

    [Fact]
    public void ScanAndLink_IsIdempotent_WhenCalledTwice()
    {
        AddGame("Chrono Trigger", "Chrono Trigger.sfc");
        WriteMediaFile("Box - Front", "Chrono Trigger.png");
        var scanner = CreateScanner();
        scanner.ScanAndLink();

        var secondRunLinkedCount = scanner.ScanAndLink();

        Assert.Equal(0, secondRunLinkedCount);
        var gameId = _games.GetBySystem(_systemId)[0].Id;
        Assert.Single(_media.GetByGameAndType(gameId, "Box - Front"));
    }

    /// <summary>
    /// Integration-style check across several systems at once, with media
    /// only partially present for some games — representative of a real
    /// imported collection rather than one game in isolation.
    /// </summary>
    [Fact]
    public void ScanAndLink_HandlesMultipleSystems_WithOnlyPartialMediaCoverage()
    {
        var nesSystemId = _systems.Add(new GameSystem { Key = "nes", Name = "NES" });
        var megadriveSystemId = _systems.Add(new GameSystem { Key = "megadrive", Name = "Mega Drive" });

        AddGame("Super Metroid", "Super Metroid.sfc"); // snes, has media
        _games.Add(new Game { SystemId = nesSystemId, Title = "Zelda", SortTitle = "Zelda", RomPath = "Zelda.nes", DateAdded = DateTime.UtcNow, DateModified = DateTime.UtcNow }); // no media
        _games.Add(new Game { SystemId = megadriveSystemId, Title = "Sonic", SortTitle = "Sonic", RomPath = "Sonic.md", DateAdded = DateTime.UtcNow, DateModified = DateTime.UtcNow }); // has media

        WriteMediaFile("Box - Front", "Super Metroid.png");
        var megadriveMediaDirectory = Path.Combine(_mediaRootDirectory, "megadrive", "Box - Front");
        Directory.CreateDirectory(megadriveMediaDirectory);
        File.WriteAllBytes(Path.Combine(megadriveMediaDirectory, "Sonic.png"), []);

        var linkedCount = CreateScanner().ScanAndLink();

        Assert.Equal(2, linkedCount);

        var snesGameId = _games.GetBySystem(_systemId)[0].Id;
        var nesGameId = _games.GetBySystem(nesSystemId)[0].Id;
        var megadriveGameId = _games.GetBySystem(megadriveSystemId)[0].Id;

        Assert.Single(_media.GetByGameAndType(snesGameId, "Box - Front"));
        Assert.Empty(_media.GetByGameAndType(nesGameId, "Box - Front"));
        Assert.Single(_media.GetByGameAndType(megadriveGameId, "Box - Front"));
    }
}
