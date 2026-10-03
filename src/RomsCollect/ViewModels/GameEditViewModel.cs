// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using RomsCollect.Models;
using RomsCollect.Services;
using RomsCollect.Services.Database;

namespace RomsCollect.ViewModels;

/// <summary>
/// Backing view model for the tabbed game-edit dialog (Main / Personal /
/// Cover / Description tabs). Supports Previous/Next navigation across a
/// fixed list of game IDs without closing the dialog.
/// </summary>
public sealed partial class GameEditViewModel : ObservableObject
{
    private const string BoxFrontMediaType = "Box - Front";

    private readonly GameRepository _games;
    private readonly GameOwnershipRepository _ownership;
    private readonly SystemRepository _systems;
    private readonly GameMediaRepository _media;
    private readonly PortableDataPaths _dataPaths;
    private readonly List<int> _navigableGameIds;

    private Game _game = null!;
    private GameOwnership? _ownershipRecord;

    // --- Main tab -----------------------------------------------------
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _sortTitle = string.Empty;
    [ObservableProperty] private GameSystem? _selectedSystem;
    [ObservableProperty] private string? _releaseDate;
    [ObservableProperty] private string? _completeness;
    [ObservableProperty] private bool _hasBox;
    [ObservableProperty] private bool _hasManual;
    [ObservableProperty] private string? _barcode;
    [ObservableProperty] private string? _format;
    [ObservableProperty] private string? _edition;
    [ObservableProperty] private string? _region;
    [ObservableProperty] private string? _series;
    [ObservableProperty] private string? _audienceRating;
    [ObservableProperty] private string? _playMode;
    [ObservableProperty] private bool _isPortable;

    /// <summary>
    /// Standard Format values offered in the dropdown, stored in English
    /// like every other free-text catalog field (e.g. Completeness). The
    /// field stays a free-text-capable ComboBox (not a strict enum) so a
    /// value already saved outside this list — from before this list
    /// existed, or from an imported collection — is never silently lost or
    /// overwritten.
    /// </summary>
    public static IReadOnlyList<string> AvailableFormats { get; } =
        ["Cartridge", "CD", "DVD", "Blu-ray", "Digital", "Floppy Disk", "Cassette"];

    /// <summary>
    /// Standard genre values offered when adding a genre to a game, stored
    /// in English like every other free-text catalog field. The field
    /// stays free-text-capable (a user can type any genre, including one
    /// not in this list) so nothing already saved is ever lost.
    /// </summary>
    public static IReadOnlyList<string> AvailableGenres { get; } =
    [
        "Action", "Adventure", "Role-Playing", "Strategy", "Simulation",
        "Sports", "Racing", "Fighting", "Shooter", "Platformer", "Puzzle",
        "Party", "Music/Rhythm", "Card/Board Game", "Educational", "Horror",
        "Visual Novel", "Sandbox", "Stealth", "Survival",
    ];

    public ObservableCollection<string> Developers { get; } = [];
    public ObservableCollection<string> Publishers { get; } = [];
    public ObservableCollection<string> Genres { get; } = [];

    [ObservableProperty] private string _newDeveloperName = string.Empty;
    [ObservableProperty] private string _newPublisherName = string.Empty;
    [ObservableProperty] private string _newGenreName = string.Empty;

    // --- Personal tab ---------------------------------------------------
    [ObservableProperty] private string? _condition;
    [ObservableProperty] private string? _owner;
    [ObservableProperty] private string? _purchaseDate;
    [ObservableProperty] private string? _purchaseStore;
    [ObservableProperty] private string? _storageDevice;
    [ObservableProperty] private string? _storageSlot;
    [ObservableProperty] private decimal? _purchasePrice;
    [ObservableProperty] private decimal? _currentValue;
    [ObservableProperty] private bool _completed;
    [ObservableProperty] private string? _completedDate;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private int? _rating;
    public ObservableCollection<string> Tags { get; } = [];
    [ObservableProperty] private string _newTagName = string.Empty;
    [ObservableProperty] private string? _collectionStatus;
    [ObservableProperty] private int _sortIndex;
    [ObservableProperty] private int _quantity = 1;
    [ObservableProperty] private string? _location;

    // --- Cover tab --------------------------------------------------------
    [ObservableProperty] private string? _coverImagePath;

    // --- Description tab -------------------------------------------------
    [ObservableProperty] private string? _description;

    public ObservableCollection<GameSystem> AvailableSystems { get; } = [];

    public bool CanNavigatePrevious => _navigableGameIds.IndexOf(_game.Id) > 0;
    public bool CanNavigateNext => _navigableGameIds.IndexOf(_game.Id) is var index && index >= 0 && index < _navigableGameIds.Count - 1;

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    public GameEditViewModel(
        GameRepository games,
        GameOwnershipRepository ownership,
        SystemRepository systems,
        GameMediaRepository media,
        PortableDataPaths dataPaths,
        int gameId,
        IReadOnlyList<int> navigableGameIds)
    {
        _games = games;
        _ownership = ownership;
        _systems = systems;
        _media = media;
        _dataPaths = dataPaths;
        _navigableGameIds = [.. navigableGameIds];

        foreach (var system in _systems.GetAll())
        {
            AvailableSystems.Add(system);
        }

        LoadGame(gameId);
    }

    private void LoadGame(int gameId)
    {
        _game = _games.GetById(gameId) ?? throw new InvalidOperationException($"Game {gameId} not found.");
        _ownershipRecord = _ownership.GetByGame(gameId).FirstOrDefault();

        Title = _game.Title;
        SortTitle = _game.SortTitle;
        SelectedSystem = AvailableSystems.FirstOrDefault(s => s.Id == _game.SystemId);
        ReleaseDate = _game.ReleaseDate;
        Barcode = _game.Barcode;
        Format = _game.Format;
        Edition = _game.Edition;
        Region = _game.Region;
        Series = _game.Series;
        AudienceRating = _game.AudienceRating;
        PlayMode = _game.PlayMode;
        IsPortable = _game.IsPortable;
        Description = _game.Description;
        Rating = _game.Rating;

        var coverAsset = _media.GetByGameAndType(gameId, BoxFrontMediaType).FirstOrDefault();
        CoverImagePath = coverAsset is null ? null : _dataPaths.ResolveMediaPath(coverAsset.RelativePath);

        Developers.Clear();
        foreach (var name in _games.GetDeveloperNames(gameId)) Developers.Add(name);
        Publishers.Clear();
        foreach (var name in _games.GetPublisherNames(gameId)) Publishers.Add(name);
        Genres.Clear();
        foreach (var name in _games.GetGenreNames(gameId)) Genres.Add(name);
        Tags.Clear();
        foreach (var name in _games.GetTagNames(gameId)) Tags.Add(name);

        Completeness = _ownershipRecord?.Completeness;
        HasBox = _ownershipRecord?.HasBox ?? false;
        HasManual = _ownershipRecord?.HasManual ?? false;
        Condition = _ownershipRecord?.Condition;
        Owner = _ownershipRecord?.Owner;
        PurchaseDate = _ownershipRecord?.PurchaseDate;
        PurchaseStore = _ownershipRecord?.PurchaseStore;
        StorageDevice = _ownershipRecord?.StorageDevice;
        StorageSlot = _ownershipRecord?.StorageSlot;
        PurchasePrice = _ownershipRecord?.PurchasePrice;
        CurrentValue = _ownershipRecord?.CurrentValue;
        Completed = _ownershipRecord?.Completed ?? _game.IsCompleted;
        CompletedDate = _ownershipRecord?.CompletedDate;
        Notes = _ownershipRecord?.Notes;
        CollectionStatus = _ownershipRecord?.CollectionStatus;
        SortIndex = _ownershipRecord?.SortIndex ?? 0;
        Quantity = _ownershipRecord?.Quantity ?? 1;
        Location = _ownershipRecord?.Location;

        OnPropertyChanged(nameof(CanNavigatePrevious));
        OnPropertyChanged(nameof(CanNavigateNext));
    }

    [RelayCommand]
    private void AddDeveloper()
    {
        if (!string.IsNullOrWhiteSpace(NewDeveloperName) && !Developers.Contains(NewDeveloperName, StringComparer.OrdinalIgnoreCase))
        {
            Developers.Add(NewDeveloperName.Trim());
        }
        NewDeveloperName = string.Empty;
    }

    [RelayCommand]
    private void RemoveDeveloper(string? name)
    {
        if (name is not null) Developers.Remove(name);
    }

    [RelayCommand]
    private void AddPublisher()
    {
        if (!string.IsNullOrWhiteSpace(NewPublisherName) && !Publishers.Contains(NewPublisherName, StringComparer.OrdinalIgnoreCase))
        {
            Publishers.Add(NewPublisherName.Trim());
        }
        NewPublisherName = string.Empty;
    }

    [RelayCommand]
    private void RemovePublisher(string? name)
    {
        if (name is not null) Publishers.Remove(name);
    }

    [RelayCommand]
    private void AddGenre()
    {
        if (!string.IsNullOrWhiteSpace(NewGenreName) && !Genres.Contains(NewGenreName, StringComparer.OrdinalIgnoreCase))
        {
            Genres.Add(NewGenreName.Trim());
        }
        NewGenreName = string.Empty;
    }

    [RelayCommand]
    private void RemoveGenre(string? name)
    {
        if (name is not null) Genres.Remove(name);
    }

    /// <summary>
    /// Imports a cover image from a local file into
    /// <c>data/Media/&lt;system&gt;/Box - Front/</c>, replacing any existing
    /// cover for this game. The file picker itself is owned by the view, per
    /// MVVM; this only performs the local file copy and database update.
    /// </summary>
    public void ImportCoverFromFile(string sourceFilePath)
    {
        var system = SelectedSystem ?? throw new InvalidOperationException("A system must be selected before importing a cover.");
        var extension = Path.GetExtension(sourceFilePath);
        var relativePath = Path.Combine(system.Key, BoxFrontMediaType, $"{Title}{extension}");
        var destinationPath = _dataPaths.ResolveMediaPath(relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Copy(sourceFilePath, destinationPath, overwrite: true);

        var existingAsset = _media.GetByGameAndType(_game.Id, BoxFrontMediaType).FirstOrDefault();
        if (existingAsset is not null)
        {
            existingAsset.RelativePath = relativePath;
            _media.Update(existingAsset);
        }
        else
        {
            _media.Add(new GameMedia { GameId = _game.Id, MediaType = BoxFrontMediaType, RelativePath = relativePath });
        }

        CoverImagePath = destinationPath;
    }

    [RelayCommand]
    private void AddTag()
    {
        if (!string.IsNullOrWhiteSpace(NewTagName) && !Tags.Contains(NewTagName, StringComparer.OrdinalIgnoreCase))
        {
            Tags.Add(NewTagName.Trim());
        }
        NewTagName = string.Empty;
    }

    [RelayCommand]
    private void RemoveTag(string? name)
    {
        if (name is not null) Tags.Remove(name);
    }

    [RelayCommand(CanExecute = nameof(CanNavigatePrevious))]
    private void NavigatePrevious()
    {
        SaveToDatabase();
        var index = _navigableGameIds.IndexOf(_game.Id);
        LoadGame(_navigableGameIds[index - 1]);
    }

    [RelayCommand(CanExecute = nameof(CanNavigateNext))]
    private void NavigateNext()
    {
        SaveToDatabase();
        var index = _navigableGameIds.IndexOf(_game.Id);
        LoadGame(_navigableGameIds[index + 1]);
    }

    [RelayCommand]
    private void Save()
    {
        SaveToDatabase();
        Saved?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);

    private void SaveToDatabase()
    {
        _game.Title = Title;
        _game.SortTitle = string.IsNullOrWhiteSpace(SortTitle) ? Title : SortTitle;
        _game.SystemId = SelectedSystem?.Id ?? _game.SystemId;
        _game.ReleaseDate = ReleaseDate;
        _game.Barcode = Barcode;
        _game.Format = Format;
        _game.Edition = Edition;
        _game.Region = Region;
        _game.Series = Series;
        _game.AudienceRating = AudienceRating;
        _game.PlayMode = PlayMode;
        _game.IsPortable = IsPortable;
        _game.Description = Description;
        _game.Rating = Rating;
        _game.IsCompleted = Completed;
        _game.DateModified = DateTime.UtcNow;
        _games.Update(_game);

        _games.SetDevelopers(_game.Id, Developers);
        _games.SetPublishers(_game.Id, Publishers);
        _games.SetGenres(_game.Id, Genres);
        _games.SetTags(_game.Id, Tags);

        if (_ownershipRecord is null)
        {
            _ownershipRecord = new GameOwnership { GameId = _game.Id };
            _ownershipRecord.Id = _ownership.Add(_ownershipRecord);
        }

        _ownershipRecord.Completeness = Completeness;
        _ownershipRecord.HasBox = HasBox;
        _ownershipRecord.HasManual = HasManual;
        _ownershipRecord.Condition = Condition;
        _ownershipRecord.Owner = Owner;
        _ownershipRecord.PurchaseDate = PurchaseDate;
        _ownershipRecord.PurchaseStore = PurchaseStore;
        _ownershipRecord.StorageDevice = StorageDevice;
        _ownershipRecord.StorageSlot = StorageSlot;
        _ownershipRecord.PurchasePrice = PurchasePrice;
        _ownershipRecord.CurrentValue = CurrentValue;
        _ownershipRecord.Completed = Completed;
        _ownershipRecord.CompletedDate = CompletedDate;
        _ownershipRecord.Notes = Notes;
        _ownershipRecord.Rating = Rating;
        _ownershipRecord.CollectionStatus = CollectionStatus;
        _ownershipRecord.SortIndex = SortIndex;
        _ownershipRecord.Quantity = Quantity;
        _ownershipRecord.Location = Location;
        _ownership.Update(_ownershipRecord);
    }
}
