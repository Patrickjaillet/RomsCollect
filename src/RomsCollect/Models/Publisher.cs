// SPDX-License-Identifier: GPL-3.0-or-later
namespace RomsCollect.Models;

/// <summary>A reusable publisher name, linked to games via GamePublishers.</summary>
public sealed class Publisher
{
    public int Id { get; set; }
    public required string Name { get; set; }
}
