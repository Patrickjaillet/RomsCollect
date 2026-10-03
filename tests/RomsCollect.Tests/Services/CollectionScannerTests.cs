// SPDX-License-Identifier: GPL-3.0-or-later
using RomsCollect.Models;
using RomsCollect.Services.Database;
using RomsCollect.Services.Scanning;

namespace RomsCollect.Tests.Services;

public class CollectionScannerTests : SqliteRepositoryTestBase
{
    private readonly SystemRepository _systems;
    private readonly GameRepository _games;
    private readonly string _romsRootDirectory;
    private readonly int _systemId;
    private bool _romsRootDeleted;

    public CollectionScannerTests()
    {
        _systems = new SystemRepository(ConnectionFactory);
        _games = new GameRepository(ConnectionFactory);
        _romsRootDirectory = Path.Combine(Path.GetTempPath(), "RomsCollectScannerTests_" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(_romsRootDirectory, "snes"));

        _systemId = _systems.Add(new GameSystem { Key = "snes", Name = "SNES", RomExtensions = ".sfc,.smc" });
    }

    public override void Dispose()
    {
        if (!_romsRootDeleted && Directory.Exists(_romsRootDirectory))
        {
            Directory.Delete(_romsRootDirectory, recursive: true);
            _romsRootDeleted = true;
        }

        base.Dispose();
    }

    private CollectionScanner CreateScanner() => new(_systems, _games, _romsRootDirectory);

    private void WriteRom(string relativePath, string content)
    {
        var fullPath = Path.Combine(_romsRootDirectory, "snes", relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    [Fact]
    public void Scan_ReportsNewRomAsAdded_AndIgnoresUnknownExtensions()
    {
        WriteRom("Chrono Trigger.sfc", "rom bytes");
        WriteRom("readme.txt", "not a rom");

        var report = CreateScanner().Scan();

        Assert.Single(report.Added);
        Assert.Equal("Chrono Trigger.sfc", report.Added[0].RelativeRomPath);
    }

    [Fact]
    public void Commit_InsertsAddedGames_TitledFromFileName()
    {
        WriteRom("Super Metroid.sfc", "rom bytes");
        var scanner = CreateScanner();
        var report = scanner.Scan();

        scanner.Commit(report);

        var games = _games.GetBySystem(_systemId);
        Assert.Single(games);
        Assert.Equal("Super Metroid", games[0].Title);
        Assert.Equal("Super Metroid.sfc", games[0].RomPath);
        Assert.NotNull(games[0].RomHash);
    }

    [Fact]
    public void Scan_ReportsUnchanged_WhenRomHashHasNotChangedSinceLastCommit()
    {
        WriteRom("Secret of Mana.sfc", "original bytes");
        var scanner = CreateScanner();
        scanner.Commit(scanner.Scan());

        var secondReport = scanner.Scan();

        Assert.Empty(secondReport.Added);
        Assert.Empty(secondReport.Updated);
        Assert.Single(secondReport.Unchanged);
    }

    [Fact]
    public void Scan_ReportsUpdated_WhenRomContentChangesAfterCommit()
    {
        WriteRom("Earthbound.sfc", "original bytes");
        var scanner = CreateScanner();
        scanner.Commit(scanner.Scan());

        WriteRom("Earthbound.sfc", "patched bytes");
        var secondReport = scanner.Scan();

        Assert.Single(secondReport.Updated);
        scanner.Commit(secondReport);

        var updatedGame = _games.GetBySystemAndRomPath(_systemId, "Earthbound.sfc");
        Assert.NotNull(updatedGame);
    }

    [Fact]
    public void Scan_NeverWritesToTheDatabase_UntilCommitIsCalled()
    {
        WriteRom("Not Yet Imported.sfc", "rom bytes");

        CreateScanner().Scan();

        Assert.Empty(_games.GetBySystem(_systemId));
    }

    [Fact]
    public void Scan_DetectsRenamedDuplicate_ByIdenticalHash_AsSeparateAddedEntry()
    {
        WriteRom("Original.sfc", "identical content");
        var scanner = CreateScanner();
        scanner.Commit(scanner.Scan());

        WriteRom("Renamed Copy.sfc", "identical content");
        var report = scanner.Scan();

        Assert.Single(report.Added);
        Assert.Equal(
            _games.GetBySystemAndRomPath(_systemId, "Original.sfc")!.RomHash,
            report.Added[0].RomHash);
    }

    [Fact]
    public void Scan_WithSystemKey_OnlyScansThatSystem_IgnoringOthers()
    {
        Directory.CreateDirectory(Path.Combine(_romsRootDirectory, "nes"));
        _systems.Add(new GameSystem { Key = "nes", Name = "NES", RomExtensions = ".nes" });

        WriteRom("Chrono Trigger.sfc", "snes rom");
        File.WriteAllText(Path.Combine(_romsRootDirectory, "nes", "Zelda.nes"), "nes rom");

        var report = CreateScanner().Scan(systemKey: "nes");

        Assert.Single(report.Added);
        Assert.Equal("nes", report.Added[0].SystemKey);
    }

    [Fact]
    public void Scan_ReportsErrors_WhenAFileCannotBeReadDuringHashing()
    {
        WriteRom("Locked.sfc", "rom bytes");
        var lockedPath = Path.Combine(_romsRootDirectory, "snes", "Locked.sfc");

        using var exclusiveHandle = new FileStream(lockedPath, FileMode.Open, FileAccess.Read, FileShare.None);
        var report = CreateScanner().Scan();

        Assert.Empty(report.Added);
        Assert.Single(report.Errors);
        Assert.Equal("Locked.sfc", report.Errors[0].RelativeRomPath);
        Assert.False(string.IsNullOrEmpty(report.Errors[0].Message));
    }

    [Fact]
    public void Scan_ReportsProgress_ForEachSystemScanned()
    {
        WriteRom("Chrono Trigger.sfc", "rom bytes");
        var reportedKeys = new List<string>();
        var progress = new Progress<string>(key => reportedKeys.Add(key));

        CreateScanner().Scan(progress: progress);

        // Progress callbacks run on the SynchronizationContext captured by
        // Progress<T>; without one (as in this synchronous test), they run
        // synchronously, so the list is already populated here.
        Assert.Contains("snes", reportedKeys);
    }
}
