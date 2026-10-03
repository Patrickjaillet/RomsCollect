// SPDX-License-Identifier: GPL-3.0-or-later
namespace RomsCollect.Services.Emulation;

/// <summary>Outcome of a <see cref="GameLauncher"/> launch attempt.</summary>
public enum GameLaunchOutcome
{
    Started,
    NoEmulatorProfileConfigured,
    EmulatorExecutableNotFound,
    RomFileNotFound,
    FailedToStart,
}

/// <summary>Result of attempting to launch a game, with enough detail to show a clear, non-silent error.</summary>
public sealed record GameLaunchResult(GameLaunchOutcome Outcome, string? Detail = null)
{
    public bool Success => Outcome == GameLaunchOutcome.Started;
}
