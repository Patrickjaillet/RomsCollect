// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.Versioning;
using System.Windows;
using Wpf.Ui.Controls;

namespace RomsCollect.Views.Windows;

[SupportedOSPlatform("windows")]
public partial class KeyboardShortcutsWindow : FluentWindow
{
    public KeyboardShortcutsWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
