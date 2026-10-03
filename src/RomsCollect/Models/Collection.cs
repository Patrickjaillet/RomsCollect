// SPDX-License-Identifier: GPL-3.0-or-later
namespace RomsCollect.Models;

/// <summary>A user-defined collection/playlist of games.</summary>
public sealed class Collection
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int SortOrder { get; set; }
}
