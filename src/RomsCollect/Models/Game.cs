// SPDX-License-Identifier: GPL-3.0-or-later
namespace RomsCollect.Models;

/// <summary>
/// A game catalog entry. Combines the playable-ROM facet (RomPath, RomHash)
/// with the collector catalog facet (Developers, Publishers, Genres, ...).
/// Per-copy ownership data lives in <see cref="GameOwnership"/>.
/// </summary>
public sealed class Game
{
    public int Id { get; set; }
    public int SystemId { get; set; }
    public required string Title { get; set; }
    public required string SortTitle { get; set; }
    public string? RomPath { get; set; }
    public string? RomHash { get; set; }
    public string? ReleaseDate { get; set; }
    public string? Series { get; set; }
    public string? AudienceRating { get; set; }
    public string? Barcode { get; set; }
    public string? Format { get; set; }
    public string? Edition { get; set; }
    public string? Region { get; set; }
    public string? Description { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsCompleted { get; set; }
    public int? Rating { get; set; }
    public string? PlayMode { get; set; }
    public bool IsPortable { get; set; }
    public DateTime DateAdded { get; set; }
    public DateTime DateModified { get; set; }
}
