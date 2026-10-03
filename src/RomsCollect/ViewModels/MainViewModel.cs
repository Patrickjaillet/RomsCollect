// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RomsCollect.Helpers;
using RomsCollect.Models;
using RomsCollect.Services;
using RomsCollect.Services.Database;

namespace RomsCollect.ViewModels;

/// <summary>
/// Root view model for the main window: system sidebar, search, sort/layout
/// state, and the filtered game collection shown in the central area.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class MainViewModel : ObservableObject
{
    private readonly SystemRepository _systems;
    private readonly GameRepository _games;
    private readonly GameMediaRepository _media;
    private readonly PlayHistoryRepository _playHistory;
    private readonly GameOwnershipRepository _ownership;
    private readonly SettingsRepository _settings;
    private readonly CollectionRepository _collections;
    private readonly PortableDataPaths _dataPaths;

    public CatalogViewModel Catalog { get; }
    public AdvancedFilterViewModel Filters { get; } = new();

    private const string BoxFrontMediaType = "Box - Front";
    private const string ClearLogoMediaType = "Clear Logo";
    private const string FanartBackgroundMediaType = "Fanart - Background";
    private const string VideoMediaType = "Video";
    private static readonly string[] FilmstripMediaTypes =
    [
        "Screenshot - Gameplay", "Screenshot - Game Title", "Fanart - Background",
    ];

    private const int AllSystemsEntryId = -1;
    private const int FavoritesEntryId = -2;
    private const int RecentlyAddedEntryId = -3;
    private const int RecentlyPlayedEntryId = -4;

    /// <summary>
    /// Synthetic sidebar entry IDs for user-defined collections are derived
    /// as <c>CollectionEntryIdOffset - collection.Id</c>, landing comfortably
    /// below the fixed entries above regardless of how many real systems or
    /// collections exist.
    /// </summary>
    private const int CollectionEntryIdOffset = -1000;

    /// <summary>Shared with <see cref="SettingsViewModel"/>, which owns the UI for changing it.</summary>
    private const string ShowSidebarGameCountKey = "showSidebarGameCount";

    [ObservableProperty]
    private bool _showSidebarGameCount = true;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private GameLayoutMode _layoutMode = GameLayoutMode.Grid;

    [ObservableProperty]
    private GameSortMode _sortMode = GameSortMode.Title;

    [ObservableProperty]
    private SystemListItemViewModel? _selectedSystem;

    [ObservableProperty]
    private GameCardViewModel? _selectedGame;

    public ObservableCollection<SystemListItemViewModel> Systems { get; } = [];
    public ObservableCollection<GameCardViewModel> Games { get; } = [];

    [ObservableProperty]
    private string _newCollectionName = string.Empty;

    public string StatusText => string.Format(
        App.Localization["Status.DisplayingCount"], Games.Count, _totalGameCount);

    private int _totalGameCount;

    [ObservableProperty]
    private ScanSummary? _lastScanSummary;

    public string? LastScanSummaryText => LastScanSummary is { } summary
        ? string.Format(
            App.Localization["Scan.NotificationSummary"],
            summary.AddedCount, summary.UpdatedCount, summary.ErrorsCount)
        : null;

    partial void OnLastScanSummaryChanged(ScanSummary? value) => OnPropertyChanged(nameof(LastScanSummaryText));

    /// <summary>
    /// Records the result of a scan the user just committed from the scan
    /// window, so it can be shown as a notification in the status area
    /// until the next scan replaces it or the session ends (never
    /// persisted — this is a transient, in-session notification only).
    /// </summary>
    public void SetLastScanSummary(ScanSummary summary) => LastScanSummary = summary;

    public MainViewModel(
        SystemRepository systems,
        GameRepository games,
        GameMediaRepository media,
        PlayHistoryRepository playHistory,
        GameOwnershipRepository ownership,
        SettingsRepository settings,
        CollectionRepository collections,
        PortableDataPaths dataPaths)
    {
        _systems = systems;
        _games = games;
        _media = media;
        _playHistory = playHistory;
        _ownership = ownership;
        _settings = settings;
        _collections = collections;
        _dataPaths = dataPaths;

        ShowSidebarGameCount = _settings.Get(ShowSidebarGameCountKey) != "false";

        Catalog = new CatalogViewModel(games, ownership, systems, settings);

        Filters.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not (nameof(AdvancedFilterViewModel.IsOpen) or nameof(AdvancedFilterViewModel.HasActiveFilters)))
            {
                ApplyFilter();
            }
        };
        LoadAvailableFilterValues();

        LoadSystems();
        ApplyFilter();
    }

    private void LoadAvailableFilterValues()
    {
        Filters.AvailableGenres.Clear();
        foreach (var genre in GameEditViewModel.AvailableGenres
            .Concat(_games.GetAllUsedGenreNames())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g, StringComparer.OrdinalIgnoreCase))
        {
            Filters.AvailableGenres.Add(genre);
        }

        Filters.AvailableYears.Clear();
        foreach (var year in _games.GetAll()
            .Select(g => ExtractYear(g.ReleaseDate))
            .Where(y => y is not null)
            .Select(y => y!.Value)
            .Distinct()
            .OrderDescending())
        {
            Filters.AvailableYears.Add(year);
        }

        Filters.AvailableCompletenessValues.Clear();
        foreach (var completeness in _ownership.GetFirstCompletenessByGame().Values.Distinct().OrderBy(c => c))
        {
            Filters.AvailableCompletenessValues.Add(completeness);
        }
    }

    private static int? ExtractYear(string? releaseDate)
        => !string.IsNullOrWhiteSpace(releaseDate) && releaseDate.Length >= 4
            && int.TryParse(releaseDate.AsSpan(0, 4), out var year)
            ? year
            : null;

    private IEnumerable<Game> ApplyAdvancedFilters(IEnumerable<Game> candidates)
    {
        if (!Filters.HasActiveFilters)
        {
            return candidates;
        }

        var materialized = candidates.ToList();

        if (Filters.FavoritesOnly)
        {
            materialized = materialized.Where(g => g.IsFavorite).ToList();
        }

        if (Filters.CompletedOnly)
        {
            materialized = materialized.Where(g => g.IsCompleted).ToList();
        }

        if (Filters.MinimumRating > 0)
        {
            materialized = materialized.Where(g => (g.Rating ?? 0) >= Filters.MinimumRating).ToList();
        }

        if (Filters.SelectedYear is { } selectedYear)
        {
            materialized = materialized.Where(g => ExtractYear(g.ReleaseDate) == selectedYear).ToList();
        }

        if (Filters.SelectedGenre is { } selectedGenre)
        {
            var genresByGame = _games.GetGenreNamesByGame();
            materialized = materialized
                .Where(g => genresByGame.TryGetValue(g.Id, out var genres) && genres.Contains(selectedGenre, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        if (Filters.SelectedCompleteness is { } selectedCompleteness)
        {
            var completenessByGame = _ownership.GetFirstCompletenessByGame();
            materialized = materialized
                .Where(g => completenessByGame.TryGetValue(g.Id, out var completeness) && string.Equals(completeness, selectedCompleteness, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return materialized;
    }

    [RelayCommand]
    private void AddCollection()
    {
        if (string.IsNullOrWhiteSpace(NewCollectionName))
        {
            return;
        }

        _collections.Add(new Collection { Name = NewCollectionName.Trim() });
        NewCollectionName = string.Empty;
        LoadSystems();
    }

    [RelayCommand]
    private void RemoveCollection(SystemListItemViewModel? entry)
    {
        if (entry?.SystemId is not { } entryId || entryId > CollectionEntryIdOffset)
        {
            return;
        }

        _collections.Delete(CollectionEntryIdOffset - entryId);
        LoadSystems();
    }

    /// <summary>
    /// Adds or removes a game from a user-defined collection (sidebar entry
    /// IDs at or below <see cref="CollectionEntryIdOffset"/>), from the
    /// game card's right-click context menu.
    /// </summary>
    [RelayCommand]
    private void ToggleGameInCollection(GameCollectionMembershipToggle? toggle)
    {
        if (toggle is null)
        {
            return;
        }

        if (toggle.IsCurrentlyInCollection)
        {
            _collections.RemoveGame(toggle.CollectionId, toggle.GameId);
        }
        else
        {
            _collections.AddGame(toggle.CollectionId, toggle.GameId);
        }

        LoadSystems();
    }

    /// <summary>
    /// Builds the "Add to Collection" / "Remove from Collection" context
    /// menu entries for one game, one per user-defined collection with its
    /// current membership state.
    /// </summary>
    public IReadOnlyList<GameCollectionMembershipToggle> GetCollectionMembershipToggles(int gameId)
    {
        var membership = _collections.GetAll()
            .Select(c => new GameCollectionMembershipToggle(
                c.Id,
                gameId,
                c.Name,
                _collections.GetGames(c.Id).Any(g => g.Id == gameId)))
            .ToList();
        return membership;
    }

    /// <summary>Reloads the sidebar display preference after it may have changed in Settings.</summary>
    public void ReloadDisplayPreferences()
    {
        ShowSidebarGameCount = _settings.Get(ShowSidebarGameCountKey) != "false";
        ApplyShowGameCountToSidebar();
    }

    public void LoadSystems()
    {
        Systems.Clear();

        var allGames = _games.GetAll();
        var allCount = allGames.Count;
        var favoritesCount = allGames.Count(g => g.IsFavorite);
        var recentlyAddedCount = Math.Min(allCount, 50);
        var recentlyPlayedCount = Math.Min(allCount, 50);

        Systems.Add(new SystemListItemViewModel(AllSystemsEntryId, "all", App.Localization["Sidebar.All"], allCount));
        Systems.Add(new SystemListItemViewModel(FavoritesEntryId, "favorites", App.Localization["Sidebar.Favorites"], favoritesCount));
        Systems.Add(new SystemListItemViewModel(RecentlyAddedEntryId, "recently-added", App.Localization["Sidebar.RecentlyAdded"], recentlyAddedCount));
        Systems.Add(new SystemListItemViewModel(RecentlyPlayedEntryId, "recently-played", App.Localization["Sidebar.RecentlyPlayed"], recentlyPlayedCount));

        foreach (var collection in _collections.GetAll())
        {
            var entryId = CollectionEntryIdOffset - collection.Id;
            var gameCount = _collections.GetGames(collection.Id).Count;
            Systems.Add(new SystemListItemViewModel(entryId, "collection", collection.Name, gameCount, isUserCollection: true));
        }

        foreach (var system in _systems.GetAll())
        {
            var gameCount = _games.GetBySystem(system.Id).Count;
            var iconFullPath = system.IconPath is null ? null : _dataPaths.ResolveMediaPath(system.IconPath);
            Systems.Add(new SystemListItemViewModel(system.Id, system.Key, system.Name, gameCount, iconFullPath));
        }

        ApplyShowGameCountToSidebar();

        SelectedSystem = Systems.FirstOrDefault();
    }

    private void ApplyShowGameCountToSidebar()
    {
        foreach (var system in Systems)
        {
            system.ShowGameCount = ShowSidebarGameCount;
        }
    }

    partial void OnSelectedSystemChanged(SystemListItemViewModel? value) => ApplyFilter();

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSortModeChanged(GameSortMode value) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<Game> candidates = SelectedSystem?.SystemId switch
        {
            null or AllSystemsEntryId => _games.GetAll(),
            FavoritesEntryId => _games.GetAll().Where(g => g.IsFavorite),
            RecentlyAddedEntryId => _games.GetAll().OrderByDescending(g => g.DateAdded).Take(50),
            RecentlyPlayedEntryId => _games.GetAll().OrderByDescending(g => g.DateModified).Take(50),
            { } entryId and <= CollectionEntryIdOffset => _collections.GetGames(CollectionEntryIdOffset - entryId),
            { } systemId => _games.GetBySystem(systemId),
        };

        _totalGameCount = _games.GetAll().Count;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            candidates = candidates.Where(g => g.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        candidates = ApplyAdvancedFilters(candidates);

        candidates = SortMode switch
        {
            GameSortMode.Title => candidates.OrderBy(g => g.SortTitle),
            GameSortMode.ReleaseDate => candidates.OrderBy(g => g.ReleaseDate),
            GameSortMode.Rating => candidates.OrderByDescending(g => g.Rating ?? -1),
            GameSortMode.LastPlayed => candidates.OrderByDescending(g => g.DateModified),
            GameSortMode.PlayCount => candidates.OrderBy(g => g.SortTitle),
            _ => candidates,
        };

        var systemsById = _systems.GetAll().ToDictionary(s => s.Id);

        Games.Clear();
        foreach (var game in candidates)
        {
            var systemName = systemsById.TryGetValue(game.SystemId, out var system) ? system.Name : string.Empty;
            var developers = _games.GetDeveloperNames(game.Id);
            var publishers = _games.GetPublisherNames(game.Id);
            var genres = _games.GetGenreNames(game.Id);
            var card = new GameCardViewModel(game, systemName, developers.FirstOrDefault() ?? string.Empty)
            {
                PrimaryPublisherName = publishers.FirstOrDefault() ?? string.Empty,
                PrimaryGenreName = genres.FirstOrDefault() ?? string.Empty,
                TotalPlayTimeDisplay = FormatTotalPlayTime(game.Id),
            };
            PopulateMedia(card);
            Games.Add(card);
        }

        OnPropertyChanged(nameof(StatusText));
    }

    private string FormatTotalPlayTime(int gameId)
    {
        var totalSeconds = _playHistory.GetByGame(gameId).Sum(entry => entry.DurationSeconds ?? 0);
        var duration = TimeSpan.FromSeconds(totalSeconds);
        return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}";
    }

    private void PopulateMedia(GameCardViewModel card)
    {
        card.BoxFrontImagePath = ResolveFirstMediaPath(card.GameId, BoxFrontMediaType);
        card.ClearLogoImagePath = ResolveFirstMediaPath(card.GameId, ClearLogoMediaType);
        card.FanartBackgroundImagePath = ResolveFirstMediaPath(card.GameId, FanartBackgroundMediaType);
        card.VideoPath = ResolveFirstMediaPath(card.GameId, VideoMediaType);

        card.MediaThumbnails.Clear();
        foreach (var mediaType in FilmstripMediaTypes)
        {
            foreach (var asset in _media.GetByGameAndType(card.GameId, mediaType))
            {
                card.MediaThumbnails.Add(_dataPaths.ResolveMediaPath(asset.RelativePath));
            }
        }
    }

    private string? ResolveFirstMediaPath(int gameId, string mediaType)
    {
        var asset = _media.GetByGameAndType(gameId, mediaType).FirstOrDefault();
        return asset is null ? null : _dataPaths.ResolveMediaPath(asset.RelativePath);
    }

    [RelayCommand]
    private void ToggleFavorite(GameCardViewModel? card)
    {
        if (card is null)
        {
            return;
        }

        card.IsFavorite = !card.IsFavorite;
        card.Game.DateModified = DateTime.UtcNow;
        _games.Update(card.Game);
    }

    [RelayCommand]
    private void ToggleCompleted(GameCardViewModel? card)
    {
        if (card is null)
        {
            return;
        }

        card.IsCompleted = !card.IsCompleted;
        card.Game.DateModified = DateTime.UtcNow;
        _games.Update(card.Game);
    }

    [RelayCommand]
    private void DeleteGame(GameCardViewModel? card)
    {
        if (card is null)
        {
            return;
        }

        _games.Delete(card.GameId);
        if (SelectedGame == card)
        {
            SelectedGame = null;
        }

        ApplyFilter();
    }

    [RelayCommand]
    private void OpenRomFolder(GameCardViewModel? card)
    {
        if (card is null || card.Game.RomPath is null)
        {
            return;
        }

        var system = _systems.GetById(card.Game.SystemId);
        if (system is null)
        {
            return;
        }

        var romFullPath = _dataPaths.ResolveRomPath(system.Key, card.Game.RomPath);
        var romDirectory = Path.GetDirectoryName(romFullPath);
        if (romDirectory is not null && Directory.Exists(romDirectory))
        {
            Process.Start(new ProcessStartInfo(romDirectory) { UseShellExecute = true });
        }
    }

    [RelayCommand]
    private void SetLayoutMode(GameLayoutMode mode) => LayoutMode = mode;

    [RelayCommand]
    private void SetSortMode(GameSortMode mode) => SortMode = mode;

    [RelayCommand]
    private void ToggleGridWheelLayout()
        => LayoutMode = LayoutMode == GameLayoutMode.Wheel ? GameLayoutMode.Grid : GameLayoutMode.Wheel;

    /// <summary>
    /// Reloads the game collection from the database and restores the
    /// selection to the same game (by ID), if it is still present. Used
    /// after an external edit (e.g. the game-edit dialog) to reflect
    /// changes without losing the current selection.
    /// </summary>
    public void RefreshSelectedGame()
    {
        var selectedGameId = SelectedGame?.GameId;
        ApplyFilter();
        SelectedGame = selectedGameId is null ? null : Games.FirstOrDefault(g => g.GameId == selectedGameId);
    }
}

/// <summary>One "Add to Collection" / "Remove from Collection" context menu entry for a game.</summary>
public sealed record GameCollectionMembershipToggle(int CollectionId, int GameId, string CollectionName, bool IsCurrentlyInCollection);
