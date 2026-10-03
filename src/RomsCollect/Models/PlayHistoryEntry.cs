// SPDX-License-Identifier: GPL-3.0-or-later
namespace RomsCollect.Models;

/// <summary>One recorded play session for a game.</summary>
public sealed class PlayHistoryEntry
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? DurationSeconds { get; set; }
}
