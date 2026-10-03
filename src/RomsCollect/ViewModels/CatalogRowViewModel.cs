// SPDX-License-Identifier: GPL-3.0-or-later
using CommunityToolkit.Mvvm.ComponentModel;
using RomsCollect.Models;

namespace RomsCollect.ViewModels;

/// <summary>One row of the tabular catalog view, combining a game with its primary ownership record.</summary>
public sealed partial class CatalogRowViewModel : ObservableObject
{
    public Game Game { get; }
    public GameOwnership? Ownership { get; }

    [ObservableProperty]
    private bool _isChecked;

    public string Title => Game.Title;
    public string PlatformName { get; }
    public string Publisher { get; }
    public string Developer { get; }
    public string Genre { get; }
    public string? ReleaseDate => Game.ReleaseDate;
    public string? Series => Game.Series;
    public string? Format => Game.Format;
    public string? Region => Game.Region;
    public string? Edition => Game.Edition;
    public string? Completeness => Ownership?.Completeness;
    public bool HasBox => Ownership?.HasBox ?? false;
    public bool HasManual => Ownership?.HasManual ?? false;
    public string? CollectionStatus => Ownership?.CollectionStatus;
    public bool Completed => Ownership?.Completed ?? Game.IsCompleted;
    public string? Condition => Ownership?.Condition;
    public int? Rating => Game.Rating;
    public string? Location => Ownership?.Location;

    public CatalogRowViewModel(Game game, GameOwnership? ownership, string platformName, string publisher, string developer, string genre)
    {
        Game = game;
        Ownership = ownership;
        PlatformName = platformName;
        Publisher = publisher;
        Developer = developer;
        Genre = genre;
    }
}
