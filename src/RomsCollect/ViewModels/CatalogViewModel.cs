// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;
using System.Runtime.Versioning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RomsCollect.Models;
using RomsCollect.Services.Database;

namespace RomsCollect.ViewModels;

/// <summary>
/// Backing view model for the tabular catalog view: configurable columns,
/// folder grouping, column sort, and manual "Add Games" entry — never an
/// online lookup.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class CatalogViewModel : ObservableObject
{
    private readonly GameRepository _games;
    private readonly GameOwnershipRepository _ownership;
    private readonly SystemRepository _systems;
    private readonly SettingsRepository _settings;

    private const string ColumnOrderSettingsKey = "catalogColumnOrder";

    private List<CatalogRowViewModel> _allRows = [];

    public ObservableCollection<CatalogColumnDefinition> AvailableColumns { get; } = [];
    public ObservableCollection<CatalogGroupViewModel> Groups { get; } = [];

    [ObservableProperty]
    private CatalogGroupingMode _groupingMode = CatalogGroupingMode.NoFolders;

    [ObservableProperty]
    private CatalogColumnKind _sortColumn = CatalogColumnKind.Title;

    [ObservableProperty]
    private bool _sortDescending;

    public IReadOnlyList<CatalogColumnDefinition> VisibleColumns
        => AvailableColumns.Where(c => c.IsVisible).OrderBy(c => c.SortOrder).ToList();

    public CatalogViewModel(GameRepository games, GameOwnershipRepository ownership, SystemRepository systems, SettingsRepository settings)
    {
        _games = games;
        _ownership = ownership;
        _systems = systems;
        _settings = settings;

        InitializeColumns();
        ApplySavedColumnOrder();
        Reload();
    }

    private void InitializeColumns()
    {
        AvailableColumns.Add(new CatalogColumnDefinition(CatalogColumnKind.Title, CatalogColumnCategory.Main, App.Localization["Catalog.Column.Title"], true, 0));
        AvailableColumns.Add(new CatalogColumnDefinition(CatalogColumnKind.Platform, CatalogColumnCategory.Main, App.Localization["Catalog.Column.Platform"], true, 1));
        AvailableColumns.Add(new CatalogColumnDefinition(CatalogColumnKind.Publisher, CatalogColumnCategory.Main, App.Localization["Catalog.Column.Publisher"], true, 2));
        AvailableColumns.Add(new CatalogColumnDefinition(CatalogColumnKind.Developer, CatalogColumnCategory.Main, App.Localization["Catalog.Column.Developer"], true, 3));
        AvailableColumns.Add(new CatalogColumnDefinition(CatalogColumnKind.ReleaseDate, CatalogColumnCategory.Main, App.Localization["Catalog.Column.ReleaseDate"], true, 4));
        AvailableColumns.Add(new CatalogColumnDefinition(CatalogColumnKind.Series, CatalogColumnCategory.Main, App.Localization["Catalog.Column.Series"], false, 5));
        AvailableColumns.Add(new CatalogColumnDefinition(CatalogColumnKind.Genre, CatalogColumnCategory.Main, App.Localization["Catalog.Column.Genre"], true, 6));

        AvailableColumns.Add(new CatalogColumnDefinition(CatalogColumnKind.Format, CatalogColumnCategory.Hardware, App.Localization["Catalog.Column.Format"], false, 7));
        AvailableColumns.Add(new CatalogColumnDefinition(CatalogColumnKind.Region, CatalogColumnCategory.Hardware, App.Localization["Catalog.Column.Region"], false, 8));

        AvailableColumns.Add(new CatalogColumnDefinition(CatalogColumnKind.Edition, CatalogColumnCategory.Edition, App.Localization["Catalog.Column.Edition"], false, 9));

        AvailableColumns.Add(new CatalogColumnDefinition(CatalogColumnKind.Completeness, CatalogColumnCategory.Completeness, App.Localization["Catalog.Column.Completeness"], true, 10));
        AvailableColumns.Add(new CatalogColumnDefinition(CatalogColumnKind.HasBox, CatalogColumnCategory.Completeness, App.Localization["Catalog.Column.HasBox"], false, 11));
        AvailableColumns.Add(new CatalogColumnDefinition(CatalogColumnKind.HasManual, CatalogColumnCategory.Completeness, App.Localization["Catalog.Column.HasManual"], false, 12));

        AvailableColumns.Add(new CatalogColumnDefinition(CatalogColumnKind.CollectionStatus, CatalogColumnCategory.Personal, App.Localization["Catalog.Column.CollectionStatus"], true, 13));
        AvailableColumns.Add(new CatalogColumnDefinition(CatalogColumnKind.Completed, CatalogColumnCategory.Personal, App.Localization["Catalog.Column.Completed"], false, 14));
        AvailableColumns.Add(new CatalogColumnDefinition(CatalogColumnKind.Condition, CatalogColumnCategory.Personal, App.Localization["Catalog.Column.Condition"], false, 15));
        AvailableColumns.Add(new CatalogColumnDefinition(CatalogColumnKind.Rating, CatalogColumnCategory.Personal, App.Localization["Catalog.Column.Rating"], true, 16));
        AvailableColumns.Add(new CatalogColumnDefinition(CatalogColumnKind.Location, CatalogColumnCategory.Personal, App.Localization["Catalog.Column.Location"], false, 17));

        foreach (var column in AvailableColumns)
        {
            column.PropertyChanged += (_, _) => OnPropertyChanged(nameof(VisibleColumns));
        }
    }

    private void ApplySavedColumnOrder()
    {
        var saved = _settings.Get(ColumnOrderSettingsKey);
        if (string.IsNullOrWhiteSpace(saved))
        {
            return;
        }

        var columnsByKind = AvailableColumns.ToDictionary(c => c.Kind);
        foreach (var entry in saved.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = entry.Split(':');
            if (parts.Length == 2
                && Enum.TryParse<CatalogColumnKind>(parts[0], out var kind)
                && int.TryParse(parts[1], out var sortOrder)
                && columnsByKind.TryGetValue(kind, out var column))
            {
                column.SortOrder = sortOrder;
            }
        }

        OnPropertyChanged(nameof(VisibleColumns));
    }

    /// <summary>
    /// Persists a new column order (the user just dragged a header to a new
    /// position in the catalog's DataGrid) and reflects it in
    /// <see cref="VisibleColumns"/>, so every grouped DataGrid picks it up
    /// on its next reload.
    /// </summary>
    public void UpdateColumnOrder(IReadOnlyList<CatalogColumnKind> orderedVisibleKinds)
    {
        for (var index = 0; index < orderedVisibleKinds.Count; index++)
        {
            var column = AvailableColumns.FirstOrDefault(c => c.Kind == orderedVisibleKinds[index]);
            if (column is not null)
            {
                column.SortOrder = index;
            }
        }

        var serialized = string.Join(',', AvailableColumns.Select(c => $"{c.Kind}:{c.SortOrder}"));
        _settings.Set(ColumnOrderSettingsKey, serialized);

        OnPropertyChanged(nameof(VisibleColumns));
    }

    public int CheckedRowCount { get; private set; }

    public void Reload()
    {
        var systemsById = _systems.GetAll().ToDictionary(s => s.Id);

        _allRows = _games.GetAll().Select(game =>
        {
            var systemName = systemsById.TryGetValue(game.SystemId, out var system) ? system.Name : string.Empty;
            var ownership = _ownership.GetByGame(game.Id).FirstOrDefault();
            var developer = _games.GetDeveloperNames(game.Id).FirstOrDefault() ?? string.Empty;
            var publisher = _games.GetPublisherNames(game.Id).FirstOrDefault() ?? string.Empty;
            var genre = string.Join(", ", _games.GetGenreNames(game.Id));
            return new CatalogRowViewModel(game, ownership, systemName, publisher, developer, genre);
        }).ToList();

        foreach (var row in _allRows)
        {
            row.PropertyChanged += OnRowPropertyChanged;
        }

        CheckedRowCount = 0;
        OnPropertyChanged(nameof(CheckedRowCount));

        ApplySortAndGrouping();
    }

    private void OnRowPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CatalogRowViewModel.IsChecked))
        {
            CheckedRowCount = _allRows.Count(r => r.IsChecked);
            OnPropertyChanged(nameof(CheckedRowCount));
        }
    }

    /// <summary>
    /// Builds a ready-to-use <see cref="AddGameViewModel"/> for the "Add
    /// Games" dialog, reusing this view model's own repositories so the
    /// calling view never needs to know about <see cref="GameRepository"/>
    /// or <see cref="SystemRepository"/> directly (kept strictly behind the
    /// ViewModel layer, per the MVVM boundary).
    /// </summary>
    public AddGameViewModel CreateAddGameViewModel() => new(_games, _systems);

    /// <summary>Every row currently checked, across all groups.</summary>
    public IReadOnlyList<CatalogRowViewModel> GetCheckedRows() => _allRows.Where(r => r.IsChecked).ToList();

    /// <summary>
    /// Deletes every checked game outright. The view owns the confirmation
    /// dialog naming the exact number of games, per the MVVM boundary.
    /// </summary>
    public void DeleteCheckedGames()
    {
        foreach (var row in GetCheckedRows())
        {
            _games.Delete(row.Game.Id);
        }

        Reload();
    }

    /// <summary>Adds <paramref name="tagName"/> to every checked game, in addition to any tags it already has.</summary>
    public void AddTagToCheckedGames(string tagName)
    {
        var trimmed = tagName.Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        foreach (var row in GetCheckedRows())
        {
            var existingTags = _games.GetTagNames(row.Game.Id);
            if (!existingTags.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            {
                _games.SetTags(row.Game.Id, existingTags.Append(trimmed));
            }
        }

        Reload();
    }

    /// <summary>
    /// Sets the Collection Status of every checked game's first ownership
    /// record, creating one if the game does not have one yet.
    /// </summary>
    public void SetCollectionStatusForCheckedGames(string collectionStatus)
    {
        foreach (var row in GetCheckedRows())
        {
            var ownership = _ownership.GetByGame(row.Game.Id).FirstOrDefault();
            if (ownership is null)
            {
                _ownership.Add(new GameOwnership { GameId = row.Game.Id, CollectionStatus = collectionStatus });
            }
            else
            {
                ownership.CollectionStatus = collectionStatus;
                _ownership.Update(ownership);
            }
        }

        Reload();
    }

    /// <summary>
    /// Builds CSV text (one row per checked game, columns matching
    /// <see cref="VisibleColumns"/>) for the caller to write to a file the
    /// user picked via a save dialog.
    /// </summary>
    public string BuildCsvForCheckedGames()
    {
        var columns = VisibleColumns;
        var lines = new List<string>
        {
            string.Join(",", columns.Select(c => EscapeCsvField(c.DisplayName))),
        };

        foreach (var row in GetCheckedRows())
        {
            var values = columns.Select(c => EscapeCsvField(GetColumnValue(row, c.Kind)));
            lines.Add(string.Join(",", values));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string GetColumnValue(CatalogRowViewModel row, CatalogColumnKind kind) => kind switch
    {
        CatalogColumnKind.Title => row.Title,
        CatalogColumnKind.Platform => row.PlatformName,
        CatalogColumnKind.Publisher => row.Publisher,
        CatalogColumnKind.Developer => row.Developer,
        CatalogColumnKind.ReleaseDate => row.ReleaseDate ?? string.Empty,
        CatalogColumnKind.Series => row.Series ?? string.Empty,
        CatalogColumnKind.Genre => row.Genre,
        CatalogColumnKind.Format => row.Format ?? string.Empty,
        CatalogColumnKind.Region => row.Region ?? string.Empty,
        CatalogColumnKind.Edition => row.Edition ?? string.Empty,
        CatalogColumnKind.Completeness => row.Completeness ?? string.Empty,
        CatalogColumnKind.HasBox => row.HasBox.ToString(),
        CatalogColumnKind.HasManual => row.HasManual.ToString(),
        CatalogColumnKind.CollectionStatus => row.CollectionStatus ?? string.Empty,
        CatalogColumnKind.Completed => row.Completed.ToString(),
        CatalogColumnKind.Condition => row.Condition ?? string.Empty,
        CatalogColumnKind.Rating => row.Rating?.ToString() ?? string.Empty,
        CatalogColumnKind.Location => row.Location ?? string.Empty,
        _ => string.Empty,
    };

    private static string EscapeCsvField(string value)
        => value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    partial void OnGroupingModeChanged(CatalogGroupingMode value) => ApplySortAndGrouping();

    partial void OnSortColumnChanged(CatalogColumnKind value) => ApplySortAndGrouping();

    partial void OnSortDescendingChanged(bool value) => ApplySortAndGrouping();

    [RelayCommand]
    private void ToggleSortDirection(CatalogColumnKind column)
    {
        if (SortColumn == column)
        {
            SortDescending = !SortDescending;
        }
        else
        {
            SortColumn = column;
            SortDescending = false;
        }
    }

    private void ApplySortAndGrouping()
    {
        var sorted = SortRows(_allRows);

        Groups.Clear();
        foreach (var (groupName, rows) in GroupRows(sorted))
        {
            Groups.Add(new CatalogGroupViewModel(groupName, rows));
        }
    }

    private IEnumerable<CatalogRowViewModel> SortRows(IEnumerable<CatalogRowViewModel> rows)
    {
        Func<CatalogRowViewModel, object?> keySelector = SortColumn switch
        {
            CatalogColumnKind.Title => r => r.Title,
            CatalogColumnKind.Platform => r => r.PlatformName,
            CatalogColumnKind.Publisher => r => r.Publisher,
            CatalogColumnKind.Developer => r => r.Developer,
            CatalogColumnKind.ReleaseDate => r => r.ReleaseDate,
            CatalogColumnKind.Series => r => r.Series,
            CatalogColumnKind.Genre => r => r.Genre,
            CatalogColumnKind.Format => r => r.Format,
            CatalogColumnKind.Region => r => r.Region,
            CatalogColumnKind.Edition => r => r.Edition,
            CatalogColumnKind.Completeness => r => r.Completeness,
            CatalogColumnKind.CollectionStatus => r => r.CollectionStatus,
            CatalogColumnKind.Condition => r => r.Condition,
            CatalogColumnKind.Rating => r => r.Rating,
            CatalogColumnKind.Location => r => r.Location,
            _ => r => r.Title,
        };

        return SortDescending
            ? rows.OrderByDescending(keySelector, Comparer<object?>.Create(CompareValues))
            : rows.OrderBy(keySelector, Comparer<object?>.Create(CompareValues));
    }

    private static int CompareValues(object? left, object? right)
    {
        if (left is null && right is null) return 0;
        if (left is null) return -1;
        if (right is null) return 1;
        return Comparer<object>.Default.Compare(left, right);
    }

    private IEnumerable<(string GroupName, List<CatalogRowViewModel> Rows)> GroupRows(IEnumerable<CatalogRowViewModel> rows)
    {
        switch (GroupingMode)
        {
            case CatalogGroupingMode.Platform:
                foreach (var group in rows.GroupBy(r => r.PlatformName).OrderBy(g => g.Key))
                {
                    yield return (group.Key, group.ToList());
                }
                break;

            case CatalogGroupingMode.Completeness:
                foreach (var group in rows.GroupBy(r => r.Completeness ?? "(Unspecified)").OrderBy(g => g.Key))
                {
                    yield return (group.Key, group.ToList());
                }
                break;

            case CatalogGroupingMode.PlatformAndCompleteness:
                foreach (var group in rows.GroupBy(r => $"{r.PlatformName} / {r.Completeness ?? "(Unspecified)"}").OrderBy(g => g.Key))
                {
                    yield return (group.Key, group.ToList());
                }
                break;

            case CatalogGroupingMode.Genre:
                foreach (var group in rows.GroupBy(r => string.IsNullOrEmpty(r.Genre) ? "(Unspecified)" : r.Genre).OrderBy(g => g.Key))
                {
                    yield return (group.Key, group.ToList());
                }
                break;

            case CatalogGroupingMode.Developer:
                foreach (var group in rows.GroupBy(r => string.IsNullOrEmpty(r.Developer) ? "(Unspecified)" : r.Developer).OrderBy(g => g.Key))
                {
                    yield return (group.Key, group.ToList());
                }
                break;

            case CatalogGroupingMode.Publisher:
                foreach (var group in rows.GroupBy(r => string.IsNullOrEmpty(r.Publisher) ? "(Unspecified)" : r.Publisher).OrderBy(g => g.Key))
                {
                    yield return (group.Key, group.ToList());
                }
                break;

            case CatalogGroupingMode.NoFolders:
            default:
                yield return (string.Empty, rows.ToList());
                break;
        }
    }
}
