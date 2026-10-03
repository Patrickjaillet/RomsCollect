// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.Versioning;
using System.Windows;
using Wpf.Ui.Controls;

namespace RomsCollect.Views.Windows;

/// <summary>
/// Shown when launching a game fails. Offers a direct link to
/// Settings > Emulators when the failure is a configuration issue
/// (no profile configured, or the configured executable is missing) —
/// the two outcomes a user can actually fix from there.
/// </summary>
[SupportedOSPlatform("windows")]
public partial class GameLaunchErrorWindow : FluentWindow
{
    /// <summary>True when the user clicked "Open Settings" to go fix the emulator configuration.</summary>
    public bool OpenSettingsRequested { get; private set; }

    public GameLaunchErrorWindow(string message, bool offerSettingsLink)
    {
        InitializeComponent();
        MessageText.Text = message;
        OpenSettingsButton.Visibility = offerSettingsLink ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnOpenSettingsClick(object sender, RoutedEventArgs e)
    {
        OpenSettingsRequested = true;
        Close();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
