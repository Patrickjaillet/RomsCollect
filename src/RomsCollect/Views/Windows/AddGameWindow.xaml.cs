// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.Versioning;
using System.Windows;
using RomsCollect.ViewModels;
using Wpf.Ui.Controls;

namespace RomsCollect.Views.Windows;

/// <summary>Manual "Add Games" dialog — purely local text entry, no online lookup.</summary>
[SupportedOSPlatform("windows")]
public partial class AddGameWindow : FluentWindow
{
    private readonly AddGameViewModel _viewModel;

    public AddGameWindow(AddGameViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.Saved += (_, _) => { DialogResult = true; Close(); };
        _viewModel.Cancelled += (_, _) => { DialogResult = false; Close(); };
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => _viewModel.CancelCommand.Execute(null);
}
