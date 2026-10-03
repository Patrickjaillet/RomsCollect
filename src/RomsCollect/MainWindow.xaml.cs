// SPDX-License-Identifier: GPL-3.0-or-later
using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using RomsCollect.Services.Database;
using RomsCollect.Services.Emulation;
using RomsCollect.Services.Scanning;
using RomsCollect.ViewModels;
using RomsCollect.Views.Windows;
using Wpf.Ui.Controls;

namespace RomsCollect;

[SupportedOSPlatform("windows")]
public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;
    private WindowState _preFullScreenWindowState = WindowState.Normal;
    private bool _isFullScreen;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // Sets the title bar/taskbar icon at runtime from the portable
        // assets folder, like AboutWindow's logo — <ApplicationIcon> in the
        // .csproj only covers the .exe file's own icon (Explorer, taskbar
        // pinning), not what WPF shows in a running window by default.
        var iconPath = Path.Combine(AppContext.BaseDirectory, "assets", "icon", "RomsCollect.ico");
        if (File.Exists(iconPath))
        {
            Icon = BitmapFrame.Create(new Uri(iconPath));
        }
    }

    private void OnGameEditRequested(object? sender, GameCardViewModel game)
    {
        var navigableGameIds = _viewModel.Games.Select(g => g.GameId).ToList();

        var editViewModel = new GameEditViewModel(
            new GameRepository(App.Database),
            new GameOwnershipRepository(App.Database),
            new SystemRepository(App.Database),
            new GameMediaRepository(App.Database),
            App.DataPaths,
            game.GameId,
            navigableGameIds);

        new GameEditWindow(editViewModel) { Owner = this }.ShowDialog();
        _viewModel.RefreshSelectedGame();
    }

    private void OnGameMoreDetailsRequested(object? sender, GameCardViewModel game)
    {
        var detailsViewModel = new GameDetailsViewModel(
            new GameRepository(App.Database),
            new GameDownloadLinkRepository(App.Database),
            new GameMediaRepository(App.Database),
            new PlayHistoryRepository(App.Database),
            new SystemRepository(App.Database),
            App.DataPaths,
            game.GameId);

        var detailsWindow = new GameDetailsWindow(detailsViewModel) { Owner = this };
        detailsWindow.PlayRequested += (_, _) => OnGamePlayRequested(this, game);
        detailsWindow.ShowDialog();
        _viewModel.RefreshSelectedGame();
    }

    private void OnGameToggleFavoriteRequested(object? sender, GameCardViewModel game)
        => _viewModel.ToggleFavoriteCommand.Execute(game);

    private void OnGameToggleCompletedRequested(object? sender, GameCardViewModel game)
        => _viewModel.ToggleCompletedCommand.Execute(game);

    private void OnGameDeleteRequested(object? sender, GameCardViewModel game)
    {
        var message = string.Format(App.Localization["GameActions.DeleteConfirmMessage"], game.Title);
        var result = System.Windows.MessageBox.Show(
            this,
            message,
            App.Localization["GameActions.DeleteConfirmTitle"],
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            _viewModel.DeleteGameCommand.Execute(game);
        }
    }

    private void OnGameOpenRomFolderRequested(object? sender, GameCardViewModel game)
        => _viewModel.OpenRomFolderCommand.Execute(game);

    private void OnGameCardContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is not System.Windows.Controls.ListBoxItem { DataContext: GameCardViewModel game, ContextMenu: { } menu })
        {
            return;
        }

        menu.Items.Clear();

        var toggles = _viewModel.GetCollectionMembershipToggles(game.GameId);
        if (toggles.Count == 0)
        {
            menu.Items.Add(new System.Windows.Controls.MenuItem
            {
                Header = App.Localization["Sidebar.NoCollectionsYet"],
                IsEnabled = false,
            });
            return;
        }

        foreach (var toggle in toggles)
        {
            var header = string.Format(
                App.Localization[toggle.IsCurrentlyInCollection ? "Sidebar.RemoveFromCollection" : "Sidebar.AddToCollection"],
                toggle.CollectionName);

            var menuItem = new System.Windows.Controls.MenuItem
            {
                Header = header,
                Command = _viewModel.ToggleGameInCollectionCommand,
                CommandParameter = toggle,
            };
            menu.Items.Add(menuItem);
        }
    }

    private void OnGamePlayRequested(object? sender, GameCardViewModel game)
    {
        var systems = new SystemRepository(App.Database);
        var system = systems.GetById(game.Game.SystemId);
        if (system is null)
        {
            return;
        }

        var launcher = new GameLauncher(
            new EmulatorProfileRepository(App.Database),
            new PlayHistoryRepository(App.Database),
            App.DataPaths);

        var result = launcher.Launch(game.Game, system.Key);

        if (!result.Success)
        {
            var offerSettingsLink = result.Outcome is GameLaunchOutcome.NoEmulatorProfileConfigured or GameLaunchOutcome.EmulatorExecutableNotFound;
            var errorWindow = new GameLaunchErrorWindow(result.Detail ?? App.Localization["GameLaunch.UnknownError"], offerSettingsLink) { Owner = this };
            errorWindow.ShowDialog();

            if (errorWindow.OpenSettingsRequested)
            {
                OpenSettingsWindow();
            }
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SearchBox.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.R && Keyboard.Modifiers == ModifierKeys.Control)
        {
            OpenScanWindow();
            e.Handled = true;
        }
        else if (e.Key == Key.F5)
        {
            if (_viewModel.SelectedGame is { } game)
            {
                OnGamePlayRequested(this, game);
            }

            e.Handled = true;
        }
        else if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _viewModel.ToggleFavoriteCommand.Execute(_viewModel.SelectedGame);
            e.Handled = true;
        }
        else if (e.Key == Key.T && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _viewModel.ToggleCompletedCommand.Execute(_viewModel.SelectedGame);
            e.Handled = true;
        }
    }

    private void OnExitMenuItemClick(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private void OnToggleFullScreenClick(object sender, RoutedEventArgs e)
    {
        if (_isFullScreen)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            WindowState = _preFullScreenWindowState;
        }
        else
        {
            _preFullScreenWindowState = WindowState;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
        }

        _isFullScreen = !_isFullScreen;
    }

    private void OnAboutMenuItemClick(object sender, RoutedEventArgs e)
        => new AboutWindow { Owner = this }.ShowDialog();

    private void OnKeyboardShortcutsMenuItemClick(object sender, RoutedEventArgs e)
        => new KeyboardShortcutsWindow { Owner = this }.ShowDialog();

    private void OnSettingsMenuItemClick(object sender, RoutedEventArgs e) => OpenSettingsWindow();

    private void OpenSettingsWindow()
    {
        var settingsViewModel = new SettingsViewModel(
            new SettingsRepository(App.Database),
            new SystemRepository(App.Database),
            new EmulatorProfileRepository(App.Database),
            App.DataPaths);

        new SettingsWindow(settingsViewModel) { Owner = this }.ShowDialog();
        _viewModel.LoadSystems();
        _viewModel.ReloadDisplayPreferences();
    }

    private void OnScanMenuItemClick(object sender, RoutedEventArgs e) => OpenScanWindow();

    private void OnLastScanNotificationClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.LastScanSummary is not { } summary)
        {
            return;
        }

        var message = string.Format(
            App.Localization["Scan.NotificationDetailMessage"],
            summary.CommittedAt, summary.AddedCount, summary.UpdatedCount, summary.ErrorsCount);

        System.Windows.MessageBox.Show(
            this,
            message,
            App.Localization["Scan.NotificationDetailTitle"],
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    private void OpenScanWindow()
    {
        var scanner = new CollectionScanner(
            new SystemRepository(App.Database),
            new GameRepository(App.Database),
            App.DataPaths.RomsRoot);

        var scanViewModel = new ScanViewModel(scanner, new SystemRepository(App.Database));

        new ScanWindow(scanViewModel) { Owner = this }.ShowDialog();

        if (scanViewModel.CommittedSummary is { } summary)
        {
            _viewModel.SetLastScanSummary(summary);
        }

        _viewModel.LoadSystems();
        _viewModel.RefreshSelectedGame();
    }
}
