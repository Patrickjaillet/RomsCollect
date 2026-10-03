// SPDX-License-Identifier: GPL-3.0-or-later
namespace RomsCollect.Models;

/// <summary>
/// A launch configuration for one emulator on one system. RomsCollect never
/// bundles or downloads any emulator; this only stores a command-line
/// template pointing at an executable the user already has installed.
/// </summary>
public sealed class EmulatorProfile
{
    public int Id { get; set; }
    public int SystemId { get; set; }
    public required string Name { get; set; }
    public required string ExecutablePath { get; set; }
    public string CommandLineTemplate { get; set; } = "\"{RomPath}\"";
    public string? WorkingDirectory { get; set; }
    public bool IsDefault { get; set; }
}
