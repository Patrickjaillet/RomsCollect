// SPDX-License-Identifier: GPL-3.0-or-later
namespace RomsCollect.Models;

/// <summary>
/// A manually entered download link shown on a game's page. RomsCollect
/// never performs any network request against this URL; it is only ever
/// opened in the system's default browser on explicit user action.
/// </summary>
public sealed class GameDownloadLink
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public required string Label { get; set; }
    public required string Url { get; set; }
    public int SortOrder { get; set; }
}
