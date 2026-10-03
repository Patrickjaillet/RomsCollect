// SPDX-License-Identifier: GPL-3.0-or-later
namespace RomsCollect.Models;

/// <summary>A reusable genre name, linked to games via GameGenres.</summary>
public sealed class Genre
{
    public int Id { get; set; }
    public required string Name { get; set; }
}
