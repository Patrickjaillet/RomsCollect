// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RomsCollect.ViewModels;

/// <summary>
/// Backing view model for the advanced search filter panel, opened from the
/// filter icon next to the search box. Every criterion is optional and
/// combines with the others (logical AND) and with the existing search text
/// / selected system / sidebar entry filters already applied by
/// <see cref="MainViewModel"/>.
/// </summary>
public sealed partial class AdvancedFilterViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string? _selectedGenre;

    [ObservableProperty]
    private int? _selectedYear;

    [ObservableProperty]
    private int _minimumRating;

    [ObservableProperty]
    private bool _favoritesOnly;

    [ObservableProperty]
    private bool _completedOnly;

    [ObservableProperty]
    private string? _selectedCompleteness;

    public ObservableCollection<string> AvailableGenres { get; } = [];
    public ObservableCollection<int> AvailableYears { get; } = [];
    public ObservableCollection<string> AvailableCompletenessValues { get; } = [];

    /// <summary>True when at least one criterion differs from its default (unset) value.</summary>
    public bool HasActiveFilters =>
        SelectedGenre is not null
        || SelectedYear is not null
        || MinimumRating > 0
        || FavoritesOnly
        || CompletedOnly
        || SelectedCompleteness is not null;

    partial void OnSelectedGenreChanged(string? value) => OnPropertyChanged(nameof(HasActiveFilters));
    partial void OnSelectedYearChanged(int? value) => OnPropertyChanged(nameof(HasActiveFilters));
    partial void OnMinimumRatingChanged(int value) => OnPropertyChanged(nameof(HasActiveFilters));
    partial void OnFavoritesOnlyChanged(bool value) => OnPropertyChanged(nameof(HasActiveFilters));
    partial void OnCompletedOnlyChanged(bool value) => OnPropertyChanged(nameof(HasActiveFilters));
    partial void OnSelectedCompletenessChanged(string? value) => OnPropertyChanged(nameof(HasActiveFilters));

    [RelayCommand]
    private void ToggleOpen() => IsOpen = !IsOpen;

    [RelayCommand]
    private void Reset()
    {
        SelectedGenre = null;
        SelectedYear = null;
        MinimumRating = 0;
        FavoritesOnly = false;
        CompletedOnly = false;
        SelectedCompleteness = null;
    }
}
