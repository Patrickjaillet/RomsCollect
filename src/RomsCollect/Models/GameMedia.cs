// SPDX-License-Identifier: GPL-3.0-or-later
namespace RomsCollect.Models;

/// <summary>
/// A media asset attached to a game (box art, screenshot, video, manual...).
/// <see cref="MediaType"/> follows RomsCollect's own media-folder naming
/// convention (e.g. "Box - Front", "Screenshot - Gameplay").
/// </summary>
public sealed class GameMedia
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public required string MediaType { get; set; }
    public required string RelativePath { get; set; }
    public int SortOrder { get; set; }
}
