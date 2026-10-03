// SPDX-License-Identifier: GPL-3.0-or-later
using System.Diagnostics;
using System.IO;
using RomsCollect.Models;
using RomsCollect.Services.Database;

namespace RomsCollect.Services.Emulation;

/// <summary>
/// Launches a game's ROM through its system's configured emulator profile,
/// waits for the emulator process to exit, and records a
/// <see cref="PlayHistoryEntry"/> for the session. Every failure path
/// (missing profile, missing emulator executable, missing ROM file) is
/// reported explicitly through <see cref="GameLaunchResult"/> — this never
/// fails silently.
/// </summary>
public sealed class GameLauncher
{
    private readonly EmulatorProfileRepository _emulatorProfiles;
    private readonly PlayHistoryRepository _playHistory;
    private readonly PortableDataPaths _dataPaths;

    public GameLauncher(EmulatorProfileRepository emulatorProfiles, PlayHistoryRepository playHistory, PortableDataPaths dataPaths)
    {
        _emulatorProfiles = emulatorProfiles;
        _playHistory = playHistory;
        _dataPaths = dataPaths;
    }

    /// <summary>
    /// Launches <paramref name="game"/> using the default emulator profile
    /// configured for <paramref name="systemKey"/>, and asynchronously waits
    /// for the emulator to exit to record play history. The returned result
    /// reflects only whether the process was started; history recording
    /// happens in the background.
    /// </summary>
    public GameLaunchResult Launch(Game game, string systemKey)
    {
        if (game.RomPath is null)
        {
            return new GameLaunchResult(GameLaunchOutcome.RomFileNotFound, "This game has no associated ROM file.");
        }

        var profile = _emulatorProfiles.GetDefaultForSystem(game.SystemId);
        if (profile is null)
        {
            return new GameLaunchResult(
                GameLaunchOutcome.NoEmulatorProfileConfigured,
                "No default emulator is configured for this system. Configure one in Settings > Emulators.");
        }

        if (!File.Exists(profile.ExecutablePath))
        {
            return new GameLaunchResult(
                GameLaunchOutcome.EmulatorExecutableNotFound,
                $"The configured emulator executable was not found:\n{profile.ExecutablePath}\n\nCheck the path in Settings > Emulators.");
        }

        var romFullPath = _dataPaths.ResolveRomPath(systemKey, game.RomPath);
        if (!File.Exists(romFullPath))
        {
            return new GameLaunchResult(GameLaunchOutcome.RomFileNotFound, $"The ROM file was not found:\n{romFullPath}");
        }

        var arguments = EmulatorCommandLineBuilder.Build(profile.CommandLineTemplate, romFullPath);

        Process process;
        try
        {
            process = Process.Start(new ProcessStartInfo(profile.ExecutablePath, arguments)
            {
                UseShellExecute = false,
                WorkingDirectory = string.IsNullOrWhiteSpace(profile.WorkingDirectory)
                    ? Path.GetDirectoryName(profile.ExecutablePath)
                    : profile.WorkingDirectory,
            }) ?? throw new InvalidOperationException("Process.Start returned null.");
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new GameLaunchResult(GameLaunchOutcome.FailedToStart, ex.Message);
        }

        var startedAt = DateTime.UtcNow;
        var historyEntryId = _playHistory.Add(new PlayHistoryEntry { GameId = game.Id, StartedAt = startedAt });

        _ = TrackSessionEndAsync(process, historyEntryId, startedAt);

        return new GameLaunchResult(GameLaunchOutcome.Started);
    }

    private async Task TrackSessionEndAsync(Process process, int historyEntryId, DateTime startedAt)
    {
        await process.WaitForExitAsync();

        var endedAt = DateTime.UtcNow;
        var entry = new PlayHistoryEntry
        {
            Id = historyEntryId,
            StartedAt = startedAt,
            EndedAt = endedAt,
            DurationSeconds = (int)(endedAt - startedAt).TotalSeconds,
        };
        _playHistory.Update(entry);
    }
}
