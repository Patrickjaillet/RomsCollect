// SPDX-License-Identifier: GPL-3.0-or-later
namespace RomsCollect.Models;

/// <summary>A reusable personal tag, linked to games via GameTags.</summary>
public sealed class Tag
{
    public int Id { get; set; }
    public required string Name { get; set; }
}
