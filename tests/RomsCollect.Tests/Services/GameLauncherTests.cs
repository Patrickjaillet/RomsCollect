// SPDX-License-Identifier: GPL-3.0-or-later
using RomsCollect.Models;
using RomsCollect.Services;
using RomsCollect.Services.Database;
using RomsCollect.Services.Emulation;

namespace RomsCollect.Tests.Services;

public class GameLauncherTests : SqliteRepositoryTestBase
{
    private readonly SystemRepository _systems;
    private readonly GameRepository _games;
    private readonly EmulatorProfileRepository _emulatorProfiles;
    private readonly PlayHistoryRepository _playHistory;
    private readonly GameLauncher _launcher;
    private readonly int _systemId;
    private readonly string _romsRootDirectory;
    private bool _romsRootDeleted;

    public GameLauncherTests()
    {
        _systems = new SystemRepository(ConnectionFactory);
        _games = new GameRepository(ConnectionFactory);
        _emulatorProfiles = new EmulatorProfileRepository(ConnectionFactory);
        _playHistory = new PlayHistoryRepository(ConnectionFactory);

        _romsRootDirectory = Path.Combine(Path.GetTempPath(), "RomsCollectLauncherTests_" + Guid.NewGuid());
        var dataPaths = new PortableDataPaths(_romsRootDirectory);
        Directory.CreateDirectory(Path.Combine(dataPaths.RomsRoot, "snes"));

        _launcher = new GameLauncher(_emulatorProfiles, _playHistory, dataPaths);

        _systemId = _systems.Add(new GameSystem { Key = "snes", Name = "SNES", RomExtensions = ".sfc" });
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

    private Game AddGame(string? romPath = "game.sfc")
    {
        var id = _games.Add(new Game
        {
            SystemId = _systemId,
            Title = "Test Game",
            SortTitle = "Test Game",
            RomPath = romPath,
            DateAdded = DateTime.UtcNow,
            DateModified = DateTime.UtcNow,
        });
        return _games.GetById(id)!;
    }

    [Fact]
    public void Launch_ReturnsRomFileNotFound_WhenGameHasNoRomPath()
    {
        var game = AddGame(romPath: null);

        var result = _launcher.Launch(game, "snes");

        Assert.Equal(GameLaunchOutcome.RomFileNotFound, result.Outcome);
        Assert.False(result.Success);
    }

    [Fact]
    public void Launch_ReturnsNoEmulatorProfileConfigured_WhenNoDefaultProfileExists()
    {
        var game = AddGame();

        var result = _launcher.Launch(game, "snes");

        Assert.Equal(GameLaunchOutcome.NoEmulatorProfileConfigured, result.Outcome);
        Assert.False(result.Success);
        Assert.Contains("Settings", result.Detail);
    }

    [Fact]
    public void Launch_ReturnsEmulatorExecutableNotFound_WhenConfiguredPathDoesNotExist()
    {
        var game = AddGame();
        _emulatorProfiles.Add(new EmulatorProfile
        {
            SystemId = _systemId,
            Name = "Missing Emulator",
            ExecutablePath = @"C:\DoesNotExist\emulator.exe",
            IsDefault = true,
        });

        var result = _launcher.Launch(game, "snes");

        Assert.Equal(GameLaunchOutcome.EmulatorExecutableNotFound, result.Outcome);
        Assert.False(result.Success);
        Assert.Contains(@"C:\DoesNotExist\emulator.exe", result.Detail);
    }

    [Fact]
    public void Launch_ReturnsRomFileNotFound_WhenRomFileIsMissingFromDisk()
    {
        var game = AddGame(romPath: "does-not-exist.sfc");
        _emulatorProfiles.Add(new EmulatorProfile
        {
            SystemId = _systemId,
            Name = "cmd",
            ExecutablePath = Environment.ProcessPath ?? @"C:\Windows\System32\cmd.exe",
            IsDefault = true,
        });

        var result = _launcher.Launch(game, "snes");

        Assert.Equal(GameLaunchOutcome.RomFileNotFound, result.Outcome);
        Assert.False(result.Success);
    }

    /// <summary>
    /// End-to-end check for all 4 pre-filled presets (RetroArch, Dolphin,
    /// PCSX2, DuckStation): each one's exact command-line template expands
    /// without throwing and actually starts a process, without requiring any
    /// of those emulators to be installed on the test machine. The current
    /// test process itself (Environment.ProcessPath) stands in as the
    /// "emulator executable" — Process.Start succeeding is what's being
    /// verified, not that the stand-in does anything useful with the
    /// (nonsensical, to it) emulator arguments it's handed.
    /// </summary>
    [Theory]
    [InlineData("RetroArch", "-L \"{CorePath}\" \"{RomPath}\"")]
    [InlineData("Dolphin", "-b -e \"{RomPath}\"")]
    [InlineData("PCSX2", "-- \"{RomPath}\"")]
    [InlineData("DuckStation", "-batch \"{RomPath}\"")]
    public void Launch_StartsAProcess_ForEachPreFilledEmulatorPreset(string presetName, string commandLineTemplate)
    {
        var fakeEmulatorExecutable = Environment.ProcessPath
            ?? throw new InvalidOperationException("Environment.ProcessPath is required for this test to stand in as a fake emulator executable.");

        File.WriteAllText(Path.Combine(_romsRootDirectory, "data", "Roms", "snes", "game.sfc"), "rom bytes");
        var game = AddGame();
        _emulatorProfiles.Add(new EmulatorProfile
        {
            SystemId = _systemId,
            Name = presetName,
            ExecutablePath = fakeEmulatorExecutable,
            CommandLineTemplate = commandLineTemplate,
            IsDefault = true,
        });

        var result = _launcher.Launch(game, "snes");

        Assert.Equal(GameLaunchOutcome.Started, result.Outcome);
        Assert.True(result.Success);
    }
}
