// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RomsCollect.Services.Input;
using RomsCollect.ViewModels;

namespace RomsCollect.Views.Controls;

/// <summary>
/// Wheel/carousel layout: the selected game's cover is centered and
/// emphasized, with a few neighbors shown smaller on each side. Left/Right
/// arrow keys move the selection; the control keeps its own small
/// projection (<see cref="VisibleTiles"/>) of the bound MainViewModel's
/// Games/SelectedGame so the wheel and grid share the same selection state.
/// </summary>
[SupportedOSPlatform("windows")]
public partial class GameWheelControl : UserControl
{
    private const int NeighborCountOnEachSide = 2;

    public static readonly DependencyProperty VisibleTilesProperty = DependencyProperty.Register(
        nameof(VisibleTiles), typeof(ObservableCollection<WheelTileViewModel>), typeof(GameWheelControl),
        new PropertyMetadata(new ObservableCollection<WheelTileViewModel>()));

    public ObservableCollection<WheelTileViewModel> VisibleTiles
    {
        get => (ObservableCollection<WheelTileViewModel>)GetValue(VisibleTilesProperty);
        private set => SetValue(VisibleTilesProperty, value);
    }

    private MainViewModel? _viewModel;
    private readonly GamepadService _gamepad = new();

    /// <summary>Raised when the gamepad's Accept (A/South) button is pressed, carrying the selected game.</summary>
    public event EventHandler<GameCardViewModel>? PlayRequested;

    public GameWheelControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        IsVisibleChanged += OnIsVisibleChanged;
        MouseDown += (_, _) => Focus();

        _gamepad.LeftPressed += (_, _) => MoveSelection(-1);
        _gamepad.RightPressed += (_, _) => MoveSelection(1);
        _gamepad.AcceptPressed += (_, _) =>
        {
            if (_viewModel?.SelectedGame is { } game)
            {
                PlayRequested?.Invoke(this, game);
            }
        };

        Unloaded += (_, _) => _gamepad.Dispose();
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            Focus();
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.Games.CollectionChanged -= OnGamesCollectionChanged;
        }

        _viewModel = e.NewValue as MainViewModel;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.Games.CollectionChanged += OnGamesCollectionChanged;
        }

        RebuildVisibleTiles();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.SelectedGame) or nameof(MainViewModel.Games))
        {
            RebuildVisibleTiles();
        }
    }

    private void OnGamesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => RebuildVisibleTiles();

    private void RebuildVisibleTiles()
    {
        VisibleTiles.Clear();

        if (_viewModel is null || _viewModel.Games.Count == 0)
        {
            return;
        }

        var selectedIndex = _viewModel.SelectedGame is null
            ? 0
            : Math.Max(0, _viewModel.Games.IndexOf(_viewModel.SelectedGame));

        var startIndex = Math.Max(0, selectedIndex - NeighborCountOnEachSide);
        var endIndex = Math.Min(_viewModel.Games.Count - 1, selectedIndex + NeighborCountOnEachSide);

        for (var index = startIndex; index <= endIndex; index++)
        {
            VisibleTiles.Add(new WheelTileViewModel
            {
                Game = _viewModel.Games[index],
                IsCenterTile = index == selectedIndex,
            });
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
                MoveSelection(-1);
                e.Handled = true;
                break;
            case Key.Right:
                MoveSelection(1);
                e.Handled = true;
                break;
        }
    }

    /// <summary>Moves the selection by <paramref name="direction"/> steps (-1 or +1), shared by keyboard and gamepad input.</summary>
    private void MoveSelection(int direction)
    {
        if (_viewModel is null || _viewModel.Games.Count == 0)
        {
            return;
        }

        var selectedIndex = _viewModel.SelectedGame is null ? 0 : _viewModel.Games.IndexOf(_viewModel.SelectedGame);
        var newIndex = selectedIndex + direction;

        if (newIndex >= 0 && newIndex < _viewModel.Games.Count)
        {
            _viewModel.SelectedGame = _viewModel.Games[newIndex];
        }
    }
}
