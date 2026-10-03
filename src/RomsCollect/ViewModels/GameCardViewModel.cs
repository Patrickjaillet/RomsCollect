// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;
using System.Runtime.Versioning;
using CommunityToolkit.Mvvm.ComponentModel;
using RomsCollect.Models;

namespace RomsCollect.ViewModels;

/// <summary>One tile in the game grid/wheel, or the row backing the detail panel.</summary>
[SupportedOSPlatform("windows")]
public sealed partial class GameCardViewModel : ObservableObject
{
    public Game Game { get; }

    [ObservableProperty]
    private string _systemName;

    [ObservableProperty]
    private string? _boxFrontImagePath;

    /// <summary>
    /// The image currently shown in the quick detail panel's central
    /// preview — defaults to <see cref="BoxFrontImagePath"/>, but can be
    /// switched to any filmstrip thumbnail by clicking it.
    /// </summary>
    [ObservableProperty]
    private string? _selectedPreviewImagePath;

    partial void OnBoxFrontImagePathChanged(string? value) => SelectedPreviewImagePath = value;

    [ObservableProperty]
    private string? _clearLogoImagePath;

    [ObservableProperty]
    private string? _fanartBackgroundImagePath;

    /// <summary>Absolute path to the game's gameplay video, or null when no "Video" media asset is set.</summary>
    [ObservableProperty]
    private string? _videoPath;

    [ObservableProperty]
    private string _primaryDeveloperName;

    [ObservableProperty]
    private string _primaryPublisherName = string.Empty;

    [ObservableProperty]
    private string _primaryGenreName = string.Empty;

    [ObservableProperty]
    private string _totalPlayTimeDisplay = "00:00";

    public ObservableCollection<string> MediaThumbnails { get; } = [];

    public int GameId => Game.Id;
    public string Title => Game.Title;
    public int? Rating => Game.Rating;

    /// <summary>True when this game was picked up by a collection scan, rather than added manually without a ROM file.</summary>
    public bool IsImportedRom => Game.RomPath is not null;

    public string ImportedRomText => App.Localization[IsImportedRom ? "Detail.Portable.Yes" : "Detail.Portable.No"];
    public string PortableText => App.Localization[Game.IsPortable ? "Detail.Portable.Yes" : "Detail.Portable.No"];
    public string StatusText => App.Localization[Game.IsCompleted ? "Detail.Status.Completed" : "Detail.Status.InProgress"];

    public bool IsFavorite
    {
        get => Game.IsFavorite;
        set
        {
            if (Game.IsFavorite != value)
            {
                Game.IsFavorite = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsCompleted
    {
        get => Game.IsCompleted;
        set
        {
            if (Game.IsCompleted != value)
            {
                Game.IsCompleted = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public GameCardViewModel(Game game, string systemName, string primaryDeveloperName = "")
    {
        Game = game;
        _systemName = systemName;
        _primaryDeveloperName = primaryDeveloperName;
    }
}
