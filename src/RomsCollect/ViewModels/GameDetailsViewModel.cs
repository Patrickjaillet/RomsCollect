// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RomsCollect.Models;
using RomsCollect.Services;
using RomsCollect.Services.Database;

namespace RomsCollect.ViewModels;

/// <summary>
/// Backing view model for the full game details page: download links,
/// manual, and play history, alongside the read-only metadata already
/// shown in the quick detail panel.
/// </summary>
public sealed partial class GameDetailsViewModel : ObservableObject
{
    private readonly GameRepository _games;
    private readonly GameDownloadLinkRepository _downloadLinks;
    private readonly GameMediaRepository _media;
    private readonly PlayHistoryRepository _playHistory;
    private readonly SystemRepository _systems;
    private readonly PortableDataPaths _dataPaths;

    private const string ManualMediaType = "Manual";
    private const string BoxFrontMediaType = "Box - Front";
    private const string ClearLogoMediaType = "Clear Logo";
    private const string FanartBackgroundMediaType = "Fanart - Background";
    private const string VideoMediaType = "Video";
    private static readonly string[] GalleryMediaTypes =
    [
        "Box - Front", "Screenshot - Gameplay", "Screenshot - Game Title", "Fanart - Background", "Cart - Front",
    ];

    public Game Game { get; }
    public string SystemName { get; }
    public string SystemKey { get; }

    public ObservableCollection<GameDownloadLink> DownloadLinks { get; } = [];
    public ObservableCollection<PlayHistoryEntry> PlayHistory { get; } = [];
    public ObservableCollection<string> GalleryImages { get; } = [];

    [ObservableProperty]
    private string _newLinkLabel = string.Empty;

    [ObservableProperty]
    private string _newLinkUrl = string.Empty;

    [ObservableProperty]
    private string? _manualRelativePath;

    [ObservableProperty]
    private string? _clearLogoImagePath;

    [ObservableProperty]
    private string? _fanartBackgroundImagePath;

    [ObservableProperty]
    private string? _videoPath;

    /// <summary>The image currently shown in the gallery's main preview — defaults to the first gallery image, switchable by clicking a thumbnail.</summary>
    [ObservableProperty]
    private string? _selectedGalleryImagePath;

    /// <summary>
    /// The personal rating (0-10), editable directly from this page without
    /// reopening the edit dialog. Persisted immediately on change.
    /// </summary>
    [ObservableProperty]
    private int _rating;

    private bool _isLoaded;

    partial void OnRatingChanged(int value)
    {
        if (!_isLoaded)
        {
            return;
        }

        Game.Rating = value;
        Game.DateModified = DateTime.UtcNow;
        _games.Update(Game);
    }

    public string TotalPlayTimeDisplay
    {
        get
        {
            var totalSeconds = PlayHistory.Sum(entry => entry.DurationSeconds ?? 0);
            var duration = TimeSpan.FromSeconds(totalSeconds);
            return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
    }

    public int SessionCount => PlayHistory.Count;

    /// <summary>
    /// Raised when the user confirms opening a download link or a manual
    /// file. The view owns the actual <c>Process.Start</c> call, per the
    /// offline-boundary constraint requiring explicit confirmation.
    /// </summary>
    public event EventHandler<string>? OpenExternalRequested;

    /// <summary>Raised when the user clicks Play on this page.</summary>
    public event EventHandler? PlayRequested;

    [RelayCommand]
    private void Play() => PlayRequested?.Invoke(this, EventArgs.Empty);

    public GameDetailsViewModel(
        GameRepository games,
        GameDownloadLinkRepository downloadLinks,
        GameMediaRepository media,
        PlayHistoryRepository playHistory,
        SystemRepository systems,
        PortableDataPaths dataPaths,
        int gameId)
    {
        _games = games;
        _downloadLinks = downloadLinks;
        _media = media;
        _playHistory = playHistory;
        _systems = systems;
        _dataPaths = dataPaths;

        Game = _games.GetById(gameId) ?? throw new InvalidOperationException($"Game {gameId} not found.");
        var system = _systems.GetById(Game.SystemId);
        SystemName = system?.Name ?? string.Empty;
        SystemKey = system?.Key ?? string.Empty;

        Reload();
    }

    private void Reload()
    {
        DownloadLinks.Clear();
        foreach (var link in _downloadLinks.GetByGame(Game.Id))
        {
            DownloadLinks.Add(link);
        }

        PlayHistory.Clear();
        foreach (var entry in _playHistory.GetByGame(Game.Id))
        {
            PlayHistory.Add(entry);
        }

        var manualAsset = _media.GetByGameAndType(Game.Id, ManualMediaType).FirstOrDefault();
        ManualRelativePath = manualAsset is null ? null : _dataPaths.ResolveMediaPath(manualAsset.RelativePath);

        ClearLogoImagePath = ResolveFirstMediaPath(ClearLogoMediaType);
        FanartBackgroundImagePath = ResolveFirstMediaPath(FanartBackgroundMediaType);
        VideoPath = ResolveFirstMediaPath(VideoMediaType);

        GalleryImages.Clear();
        foreach (var mediaType in GalleryMediaTypes)
        {
            foreach (var asset in _media.GetByGameAndType(Game.Id, mediaType))
            {
                GalleryImages.Add(_dataPaths.ResolveMediaPath(asset.RelativePath));
            }
        }

        SelectedGalleryImagePath = GalleryImages.FirstOrDefault();

        OnPropertyChanged(nameof(TotalPlayTimeDisplay));
        OnPropertyChanged(nameof(SessionCount));

        Rating = Game.Rating ?? 0;
        _isLoaded = true;
    }

    private string? ResolveFirstMediaPath(string mediaType)
    {
        var asset = _media.GetByGameAndType(Game.Id, mediaType).FirstOrDefault();
        return asset is null ? null : _dataPaths.ResolveMediaPath(asset.RelativePath);
    }

    /// <summary>Switches the gallery's main preview to a clicked thumbnail.</summary>
    public void SelectGalleryImage(string imagePath) => SelectedGalleryImagePath = imagePath;

    [RelayCommand]
    private void AddDownloadLink()
    {
        if (string.IsNullOrWhiteSpace(NewLinkLabel) || string.IsNullOrWhiteSpace(NewLinkUrl))
        {
            return;
        }

        _downloadLinks.Add(new GameDownloadLink
        {
            GameId = Game.Id,
            Label = NewLinkLabel.Trim(),
            Url = NewLinkUrl.Trim(),
            SortOrder = DownloadLinks.Count,
        });

        NewLinkLabel = string.Empty;
        NewLinkUrl = string.Empty;
        Reload();
    }

    [RelayCommand]
    private void RemoveDownloadLink(GameDownloadLink? link)
    {
        if (link is null)
        {
            return;
        }

        _downloadLinks.Delete(link.Id);
        Reload();
    }

    [RelayCommand]
    private void OpenDownloadLink(GameDownloadLink? link)
    {
        if (link is not null)
        {
            OpenExternalRequested?.Invoke(this, link.Url);
        }
    }

    [RelayCommand]
    private void OpenManual()
    {
        if (ManualRelativePath is not null)
        {
            OpenExternalRequested?.Invoke(this, ManualRelativePath);
        }
    }

    /// <summary>
    /// Imports a manual file from the local file system into
    /// <c>data/Media/&lt;system&gt;/Manual/</c>, replacing any existing one.
    /// </summary>
    public void ImportManualFromFile(string sourceFilePath)
    {
        var extension = Path.GetExtension(sourceFilePath);
        var relativePath = Path.Combine(SystemKey, ManualMediaType, $"{Game.Title}{extension}");
        var destinationPath = _dataPaths.ResolveMediaPath(relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Copy(sourceFilePath, destinationPath, overwrite: true);

        var existingAsset = _media.GetByGameAndType(Game.Id, ManualMediaType).FirstOrDefault();
        if (existingAsset is not null)
        {
            existingAsset.RelativePath = relativePath;
            _media.Update(existingAsset);
        }
        else
        {
            _media.Add(new GameMedia { GameId = Game.Id, MediaType = ManualMediaType, RelativePath = relativePath });
        }

        ManualRelativePath = destinationPath;
    }
}
