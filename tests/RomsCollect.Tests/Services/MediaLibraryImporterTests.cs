// SPDX-License-Identifier: GPL-3.0-or-later
using RomsCollect.Models;
using RomsCollect.Services.Database;
using RomsCollect.Services.Import;

namespace RomsCollect.Tests.Services;

public class MediaLibraryImporterTests : SqliteRepositoryTestBase
{
    private readonly SystemRepository _systems;
    private readonly GameRepository _games;
    private readonly string _tempDirectory;
    private readonly int _systemId;
    private bool _tempDirectoryDeleted;

    private const string SamplePlatformXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <Platform>
          <Game>
            <Title>Chrono Trigger</Title>
            <ApplicationPath>C:\Roms\SNES\Chrono Trigger.sfc</ApplicationPath>
            <Notes>A classic time-travel RPG.</Notes>
            <Developer>Square</Developer>
            <Publisher>Square</Publisher>
            <ReleaseDate>1995-03-11T00:00:00</ReleaseDate>
            <Genre>RPG;Adventure</Genre>
          </Game>
          <Game>
            <Title>Untitled Prototype</Title>
          </Game>
        </Platform>
        """;

    public MediaLibraryImporterTests()
    {
        _systems = new SystemRepository(ConnectionFactory);
        _games = new GameRepository(ConnectionFactory);
        _tempDirectory = Path.Combine(Path.GetTempPath(), "RomsCollectMediaLibraryTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);

        _systemId = _systems.Add(new GameSystem { Key = "snes", Name = "SNES" });
    }

    public override void Dispose()
    {
        if (!_tempDirectoryDeleted && Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
            _tempDirectoryDeleted = true;
        }

        base.Dispose();
    }

    private string WritePlatformXml()
    {
        var path = Path.Combine(_tempDirectory, "Super Nintendo Entertainment System.xml");
        File.WriteAllText(path, SamplePlatformXml);
        return path;
    }

    [Fact]
    public void ParsePlatformXml_ReadsTextMetadata_AndSkipsEntryWithNoApplicationPath()
    {
        var entries = MediaLibraryImporter.ParsePlatformXml(WritePlatformXml());

        Assert.Equal(2, entries.Count);
        var chronoTrigger = entries.Single(e => e.Title == "Chrono Trigger");
        Assert.Equal("Chrono Trigger.sfc", chronoTrigger.RomFileName);
        Assert.Equal("A classic time-travel RPG.", chronoTrigger.Description);
        Assert.Equal("Square", chronoTrigger.Developer);
        Assert.Equal(["RPG", "Adventure"], chronoTrigger.Genres);

        var untitled = entries.Single(e => e.Title == "Untitled Prototype");
        Assert.Null(untitled.RomFileName);
    }

    [Fact]
    public void ApplyMetadata_UpdatesMatchingGame_ByRomFileName()
    {
        var gameId = _games.Add(new Game
        {
            SystemId = _systemId,
            Title = "Chrono Trigger",
            SortTitle = "Chrono Trigger",
            RomPath = "Chrono Trigger.sfc",
            DateAdded = DateTime.UtcNow,
            DateModified = DateTime.UtcNow,
        });

        var entries = MediaLibraryImporter.ParsePlatformXml(WritePlatformXml());
        var matchedCount = new MediaLibraryImporter(_games).ApplyMetadata(_systemId, entries);

        Assert.Equal(1, matchedCount);
        var updatedGame = _games.GetById(gameId)!;
        Assert.Equal("A classic time-travel RPG.", updatedGame.Description);
        Assert.Equal(["Square"], _games.GetDeveloperNames(gameId));
        Assert.Equal(["Adventure", "RPG"], _games.GetGenreNames(gameId));
    }

    [Fact]
    public void ApplyMetadata_DoesNotThrow_WhenNoGameMatches()
    {
        var entries = MediaLibraryImporter.ParsePlatformXml(WritePlatformXml());

        var matchedCount = new MediaLibraryImporter(_games).ApplyMetadata(_systemId, entries);

        Assert.Equal(0, matchedCount);
    }
}
