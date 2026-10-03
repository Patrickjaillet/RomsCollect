// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.Versioning;
using System.Windows;
using RomsCollect.Models;
using Wpf.Ui.Controls;

namespace RomsCollect.Views.Windows;

/// <summary>
/// Small modal letting the user pick which system should receive the games
/// of a system that is about to be removed, as the non-destructive
/// alternative to deleting those games along with the system.
/// </summary>
[SupportedOSPlatform("windows")]
public partial class SelectSystemWindow : FluentWindow
{
    public GameSystem? SelectedSystem { get; private set; }

    public SelectSystemWindow(string message, IReadOnlyList<GameSystem> candidateSystems)
    {
        InitializeComponent();
        MessageText.Text = message;
        TargetSystemComboBox.ItemsSource = candidateSystems;

        if (candidateSystems.Count > 0)
        {
            TargetSystemComboBox.SelectedIndex = 0;
        }
        else
        {
            ConfirmButton.IsEnabled = false;
        }
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        SelectedSystem = TargetSystemComboBox.SelectedItem as GameSystem;
        if (SelectedSystem is null)
        {
            return;
        }

        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
