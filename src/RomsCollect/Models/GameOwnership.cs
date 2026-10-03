// SPDX-License-Identifier: GPL-3.0-or-later
namespace RomsCollect.Models;

/// <summary>
/// One owned (or wanted/sold/on-loan) copy of a <see cref="Game"/>. A game
/// can have several ownership rows to track duplicates and re-editions.
/// <see cref="CurrentValue"/> is always a manual entry — RomsCollect never
/// queries an online pricing service.
/// </summary>
public sealed class GameOwnership
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public string? Condition { get; set; }
    public string? Owner { get; set; }
    public string? PurchaseDate { get; set; }
    public string? PurchaseStore { get; set; }
    public string? StorageDevice { get; set; }
    public string? StorageSlot { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? CurrentValue { get; set; }
    public bool Completed { get; set; }
    public string? CompletedDate { get; set; }
    public string? Notes { get; set; }
    public int? Rating { get; set; }
    public string? CollectionStatus { get; set; }
    public int SortIndex { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Location { get; set; }
    public string? Completeness { get; set; }
    public bool HasBox { get; set; }
    public bool HasManual { get; set; }
}
