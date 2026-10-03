// SPDX-License-Identifier: GPL-3.0-or-later
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Data;
using RomsCollect.ViewModels;
using Wpf.Ui.Controls;

namespace RomsCollect.Views.Windows;

/// <summary>
/// "Select columns" dialog: toggles column visibility for the tabular
/// catalog view, columns grouped by category (Main / Hardware / Edition /
/// Completeness / Personal).
/// </summary>
[SupportedOSPlatform("windows")]
public partial class SelectColumnsWindow : FluentWindow
{
    public SelectColumnsWindow(CatalogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        var collectionView = new ListCollectionView(viewModel.AvailableColumns)
        {
            SortDescriptions = { new SortDescription(nameof(CatalogColumnDefinition.Category), ListSortDirection.Ascending) },
        };
        collectionView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(CatalogColumnDefinition.Category)));
        ColumnsItemsControl.ItemsSource = collectionView;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
