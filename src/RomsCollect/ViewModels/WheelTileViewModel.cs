// SPDX-License-Identifier: GPL-3.0-or-later
namespace RomsCollect.ViewModels;

/// <summary>One tile shown in the wheel/carousel layout.</summary>
public sealed class WheelTileViewModel
{
    public required GameCardViewModel Game { get; init; }
    public bool IsCenterTile { get; init; }
}
