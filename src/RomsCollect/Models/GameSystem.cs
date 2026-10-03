// SPDX-License-Identifier: GPL-3.0-or-later
namespace RomsCollect.Models;

/// <summary>
/// A game platform (e.g. SNES, Mega Drive). Directory names in
/// <c>data/Roms/&lt;Key&gt;/</c> follow a standard per-system folder
/// naming convention, shared with several existing ROM collections.
/// </summary>
public sealed class GameSystem
{
    public int Id { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public string RomExtensions { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    /// <summary>
    /// Path to a locally imported system icon/logo, relative to
    /// <c>data/Media/&lt;Key&gt;/</c>. Never populated from a network
    /// source — imported from a local file only, like every other media
    /// asset in RomsCollect. Null when no icon has been set.
    /// </summary>
    public string? IconPath { get; set; }

    /// <summary>
    /// Absolute path to the system's icon/logo image, resolved against
    /// <c>data/Media/</c> by the view model that loads this system (e.g.
    /// <see cref="RomsCollect.ViewModels.SettingsViewModel"/>). Not a
    /// database column — Dapper's convention-based mapping ignores it
    /// since no matching column exists. Null until resolved, or when no
    /// icon has been set.
    /// </summary>
    public string? IconFullPath { get; set; }
}
