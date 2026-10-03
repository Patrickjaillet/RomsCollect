// SPDX-License-Identifier: GPL-3.0-or-later
namespace RomsCollect.Models;

/// <summary>A reusable developer name, linked to games via GameDevelopers.</summary>
public sealed class Developer
{
    public int Id { get; set; }
    public required string Name { get; set; }
}
