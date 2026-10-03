// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using RomsCollect.Models;
using RomsCollect.ViewModels;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace RomsCollect.Views.Windows;

[SupportedOSPlatform("windows")]
public partial class SettingsWindow : FluentWindow
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.SystemRemovalRequested += OnSystemRemovalRequested;
    }

    private void OnSystemRemovalRequested(object? sender, SystemRemovalRequestedEventArgs e)
    {
        if (e.AffectedGameCount == 0)
        {
            var confirmMessage = string.Format(App.Localization["Settings.RemoveSystem.NoGamesConfirmMessage"], e.System.Name);
            var confirmResult = System.Windows.MessageBox.Show(
                this,
                confirmMessage,
                App.Localization["Settings.RemoveSystem.NoGamesConfirmTitle"],
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirmResult == System.Windows.MessageBoxResult.Yes)
            {
                _viewModel.ConfirmDeleteSystemWithGames(e.System);
            }

            return;
        }

        var message = string.Format(
            App.Localization["Settings.RemoveSystem.WithGamesConfirmMessage"],
            e.System.Name,
            e.AffectedGameCount);

        var result = System.Windows.MessageBox.Show(
            this,
            message,
            App.Localization["Settings.RemoveSystem.WithGamesConfirmTitle"],
            System.Windows.MessageBoxButton.YesNoCancel,
            System.Windows.MessageBoxImage.Warning);

        switch (result)
        {
            case System.Windows.MessageBoxResult.Yes:
                ReassignThenRemove(e.System, e.AffectedGameCount, e.OtherSystems);
                break;

            case System.Windows.MessageBoxResult.No:
                _viewModel.ConfirmDeleteSystemWithGames(e.System);
                break;

            case System.Windows.MessageBoxResult.Cancel:
            default:
                break;
        }
    }

    private void ReassignThenRemove(GameSystem system, int affectedGameCount, IReadOnlyList<GameSystem> otherSystems)
    {
        if (otherSystems.Count == 0)
        {
            System.Windows.MessageBox.Show(
                this,
                string.Format(App.Localization["Settings.RemoveSystem.NoOtherSystems"], system.Name),
                App.Localization["Settings.RemoveSystem.ReassignTitle"],
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        var reassignMessage = string.Format(App.Localization["Settings.RemoveSystem.ReassignMessage"], affectedGameCount, system.Name);
        var selectWindow = new SelectSystemWindow(reassignMessage, otherSystems) { Owner = this };

        if (selectWindow.ShowDialog() == true && selectWindow.SelectedSystem is not null)
        {
            _viewModel.ReassignGamesAndDeleteSystem(system, selectWindow.SelectedSystem);
        }
    }

    private void OnBrowseSystemIconClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: GameSystem system })
        {
            return;
        }

        var dialog = new OpenFileDialog { Filter = "Image|*.png;*.jpg;*.jpeg;*.bmp;*.ico" };
        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.SetSystemIcon(system, dialog.FileName);
        }
    }

    private void OnBrowseEmulatorExecutableClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Executable|*.exe" };
        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.NewEmulatorExecutablePath = dialog.FileName;
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveToStorage();
        ApplyTheme();
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close();

    private void ApplyTheme()
    {
        ApplicationThemeManager.Apply(_viewModel.IsDarkTheme ? ApplicationTheme.Dark : ApplicationTheme.Light);

        if (ColorConverter.ConvertFromString(_viewModel.AccentColorHex) is Color accentColor)
        {
            ApplicationAccentColorManager.Apply(accentColor);
        }
    }
}
