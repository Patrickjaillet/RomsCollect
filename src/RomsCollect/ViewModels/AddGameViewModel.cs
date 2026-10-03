// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RomsCollect.Models;
using RomsCollect.Services.Database;

namespace RomsCollect.ViewModels;

/// <summary>
/// Backing view model for the manual "Add Games" dialog: purely local text
/// entry, no online lookup of any kind (unlike connected cataloging tools).
/// </summary>
public sealed partial class AddGameViewModel : ObservableObject
{
    private readonly GameRepository _games;

    public ObservableCollection<GameSystem> AvailableSystems { get; } = [];

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private GameSystem? _selectedSystem;

    public bool CanSave => !string.IsNullOrWhiteSpace(Title) && SelectedSystem is not null;

    public int? CreatedGameId { get; private set; }

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    public AddGameViewModel(GameRepository games, SystemRepository systems)
    {
        _games = games;
        foreach (var system in systems.GetAll())
        {
            AvailableSystems.Add(system);
        }
    }

    partial void OnTitleChanged(string value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSystemChanged(GameSystem? value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        var now = DateTime.UtcNow;
        CreatedGameId = _games.Add(new Game
        {
            SystemId = SelectedSystem!.Id,
            Title = Title.Trim(),
            SortTitle = Title.Trim(),
            DateAdded = now,
            DateModified = now,
        });

        Saved?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);
}
