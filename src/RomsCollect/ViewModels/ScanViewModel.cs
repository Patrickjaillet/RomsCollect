// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RomsCollect.Models;
using RomsCollect.Services.Database;
using RomsCollect.Services.Scanning;

namespace RomsCollect.ViewModels;

/// <summary>
/// Backing view model for the "Scan collection" window: runs
/// <see cref="CollectionScanner.Scan"/> on a background thread so the UI
/// never freezes, then shows the resulting <see cref="ScanReport"/> to the
/// user for an explicit confirmation before anything is written to the
/// database via <see cref="CollectionScanner.Commit"/>.
/// </summary>
public sealed partial class ScanViewModel : ObservableObject
{
    private readonly CollectionScanner _scanner;
    private readonly SystemRepository _systems;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _hasReport;

    [ObservableProperty]
    private bool _isCommitted;

    [ObservableProperty]
    private string _currentSystemKey = string.Empty;

    [ObservableProperty]
    private GameSystem? _selectedSystem;

    public ObservableCollection<GameSystem> Systems { get; } = [];

    public ObservableCollection<ScanReportEntry> Added { get; } = [];
    public ObservableCollection<ScanReportEntry> Updated { get; } = [];
    public ObservableCollection<ScanReportEntry> Unchanged { get; } = [];
    public ObservableCollection<ScanReportEntry> Errors { get; } = [];

    public int AddedCount => Added.Count;
    public int UpdatedCount => Updated.Count;
    public int UnchangedCount => Unchanged.Count;
    public int ErrorsCount => Errors.Count;

    private ScanReport? _report;

    /// <summary>
    /// Set once <see cref="Commit"/> has actually written a report to the
    /// database, for the caller to surface as a "last scan" notification.
    /// Null until a commit happens — a reviewed-but-not-applied report
    /// never produces a notification.
    /// </summary>
    public ScanSummary? CommittedSummary { get; private set; }

    public ScanViewModel(CollectionScanner scanner, SystemRepository systems)
    {
        _scanner = scanner;
        _systems = systems;

        foreach (var system in _systems.GetAll())
        {
            Systems.Add(system);
        }
    }

    /// <summary>Scans every configured system's ROM directory.</summary>
    [RelayCommand]
    private Task ScanAll() => RunScanAsync(systemKey: null);

    /// <summary>Scans only <see cref="SelectedSystem"/>'s ROM directory.</summary>
    [RelayCommand]
    private Task ScanSelectedSystem() => SelectedSystem is null ? Task.CompletedTask : RunScanAsync(SelectedSystem.Key);

    private async Task RunScanAsync(string? systemKey)
    {
        IsScanning = true;
        HasReport = false;
        IsCommitted = false;
        Added.Clear();
        Updated.Clear();
        Unchanged.Clear();
        Errors.Clear();

        var progress = new Progress<string>(key => CurrentSystemKey = key);

        _report = await Task.Run(() => _scanner.Scan(systemKey, progress));

        foreach (var entry in _report.Added)
        {
            Added.Add(entry);
        }

        foreach (var entry in _report.Updated)
        {
            Updated.Add(entry);
        }

        foreach (var entry in _report.Unchanged)
        {
            Unchanged.Add(entry);
        }

        foreach (var entry in _report.Errors)
        {
            Errors.Add(entry);
        }

        OnPropertyChanged(nameof(AddedCount));
        OnPropertyChanged(nameof(UpdatedCount));
        OnPropertyChanged(nameof(UnchangedCount));
        OnPropertyChanged(nameof(ErrorsCount));

        CurrentSystemKey = string.Empty;
        IsScanning = false;
        HasReport = true;
    }

    /// <summary>
    /// Writes the scan report to the database. Never called automatically —
    /// the view only invokes this after the user has reviewed the report and
    /// explicitly confirmed it.
    /// </summary>
    [RelayCommand]
    private void Commit()
    {
        if (_report is null)
        {
            return;
        }

        _scanner.Commit(_report);
        CommittedSummary = new ScanSummary(DateTime.Now, AddedCount, UpdatedCount, ErrorsCount);
        IsCommitted = true;
    }
}

/// <summary>Compact result of a committed scan, shown as a "last scan" notification.</summary>
public sealed record ScanSummary(DateTime CommittedAt, int AddedCount, int UpdatedCount, int ErrorsCount);
