// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.Versioning;
using System.Windows;
using RomsCollect.ViewModels;
using Wpf.Ui.Controls;

namespace RomsCollect.Views.Windows;

[SupportedOSPlatform("windows")]
public partial class ScanWindow : FluentWindow
{
    public ScanWindow(ScanViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
