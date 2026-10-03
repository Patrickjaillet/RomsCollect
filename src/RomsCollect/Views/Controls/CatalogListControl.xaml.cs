// SPDX-License-Identifier: GPL-3.0-or-later
using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using RomsCollect.ViewModels;
using RomsCollect.Views.Windows;

namespace RomsCollect.Views.Controls;

/// <summary>
/// Tabular catalog view. Columns are generated per-DataGrid from
/// <see cref="CatalogViewModel.VisibleColumns"/>
/// so toggling a column in the "Select Columns" dialog is reflected on the
/// next reload without a fixed, hand-authored column list.
/// </summary>
[SupportedOSPlatform("windows")]
public partial class CatalogListControl : UserControl
{
    private bool _isReorderingColumns;

    public CatalogListControl()
    {
        InitializeComponent();
    }

    private CatalogViewModel? ViewModel => DataContext as CatalogViewModel;

    private void OnRowsDataGridLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not DataGrid dataGrid || ViewModel is not { } viewModel)
        {
            return;
        }

        PopulateColumns(dataGrid, viewModel);
    }

    private void PopulateColumns(DataGrid dataGrid, CatalogViewModel viewModel)
    {
        dataGrid.Columns.Clear();
        dataGrid.Columns.Add(new DataGridCheckBoxColumn
        {
            Binding = new System.Windows.Data.Binding(nameof(CatalogRowViewModel.IsChecked)),
            Width = 32,
            CanUserReorder = false,
        });

        foreach (var column in viewModel.VisibleColumns)
        {
            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = column.DisplayName,
                Binding = new System.Windows.Data.Binding(GetBindingPath(column.Kind)),
                IsReadOnly = true,
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                // SortMemberPath doubles as this column's CatalogColumnKind
                // identifier (via GetColumnKindFromBindingPath) since
                // DataGridColumn has no free-form Tag property.
                SortMemberPath = column.Kind.ToString(),
            });
        }
    }

    private static string GetBindingPath(CatalogColumnKind kind) => kind switch
    {
        CatalogColumnKind.Title => nameof(CatalogRowViewModel.Title),
        CatalogColumnKind.Platform => nameof(CatalogRowViewModel.PlatformName),
        CatalogColumnKind.Publisher => nameof(CatalogRowViewModel.Publisher),
        CatalogColumnKind.Developer => nameof(CatalogRowViewModel.Developer),
        CatalogColumnKind.ReleaseDate => nameof(CatalogRowViewModel.ReleaseDate),
        CatalogColumnKind.Series => nameof(CatalogRowViewModel.Series),
        CatalogColumnKind.Genre => nameof(CatalogRowViewModel.Genre),
        CatalogColumnKind.Format => nameof(CatalogRowViewModel.Format),
        CatalogColumnKind.Region => nameof(CatalogRowViewModel.Region),
        CatalogColumnKind.Edition => nameof(CatalogRowViewModel.Edition),
        CatalogColumnKind.Completeness => nameof(CatalogRowViewModel.Completeness),
        CatalogColumnKind.HasBox => nameof(CatalogRowViewModel.HasBox),
        CatalogColumnKind.HasManual => nameof(CatalogRowViewModel.HasManual),
        CatalogColumnKind.CollectionStatus => nameof(CatalogRowViewModel.CollectionStatus),
        CatalogColumnKind.Completed => nameof(CatalogRowViewModel.Completed),
        CatalogColumnKind.Condition => nameof(CatalogRowViewModel.Condition),
        CatalogColumnKind.Rating => nameof(CatalogRowViewModel.Rating),
        CatalogColumnKind.Location => nameof(CatalogRowViewModel.Location),
        _ => nameof(CatalogRowViewModel.Title),
    };

    private void OnColumnReordered(object sender, DataGridColumnEventArgs e)
    {
        if (_isReorderingColumns || sender is not DataGrid dataGrid || ViewModel is not { } viewModel)
        {
            return;
        }

        var orderedKinds = dataGrid.Columns
            .Where(c => c.SortMemberPath is { Length: > 0 })
            .OrderBy(c => c.DisplayIndex)
            .Select(c => Enum.Parse<CatalogColumnKind>(c.SortMemberPath))
            .ToList();

        viewModel.UpdateColumnOrder(orderedKinds);

        // Rebuild every grouped DataGrid (there is one per group when
        // grouping is active) so the new order is reflected everywhere, not
        // only in the DataGrid the user actually dragged a header in.
        _isReorderingColumns = true;
        foreach (var otherDataGrid in FindVisualChildren<DataGrid>(GroupsItemsControl))
        {
            PopulateColumns(otherDataGrid, viewModel);
        }

        _isReorderingColumns = false;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private void OnDataGridSorting(object sender, DataGridSortingEventArgs e)
    {
        // Sorting is driven by CatalogViewModel (re-sorts and re-groups the
        // source rows) rather than the DataGrid's own view sort, so the
        // header click just toggles the view model's sort state.
        e.Handled = true;

        if (Enum.TryParse<CatalogColumnKind>(e.Column.SortMemberPath, out var columnKind) && ViewModel is { } viewModel)
        {
            viewModel.ToggleSortDirectionCommand.Execute(columnKind);
            e.Column.SortDirection = viewModel.SortDescending
                ? System.ComponentModel.ListSortDirection.Descending
                : System.ComponentModel.ListSortDirection.Ascending;
        }
    }

    private void OnAddGamesClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } viewModel)
        {
            return;
        }

        var addGameViewModel = viewModel.CreateAddGameViewModel();
        var dialogResult = new AddGameWindow(addGameViewModel) { Owner = Window.GetWindow(this) }.ShowDialog();

        if (dialogResult == true)
        {
            viewModel.Reload();
        }
    }

    private void OnSelectColumnsClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } viewModel)
        {
            return;
        }

        new SelectColumnsWindow(viewModel) { Owner = Window.GetWindow(this) }.ShowDialog();
        viewModel.Reload();
    }

    private void OnBulkDeleteClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } viewModel)
        {
            return;
        }

        var count = viewModel.CheckedRowCount;
        var message = string.Format(App.Localization["Catalog.Bulk.DeleteConfirmMessage"], count);
        var result = System.Windows.MessageBox.Show(
            Window.GetWindow(this),
            message,
            App.Localization["Catalog.Bulk.DeleteConfirmTitle"],
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            viewModel.DeleteCheckedGames();
        }
    }

    private void OnBulkAddTagClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } viewModel)
        {
            return;
        }

        var message = string.Format(App.Localization["Catalog.BulkAddTag.Message"], viewModel.CheckedRowCount);
        var dialog = new BulkAddTagWindow(message) { Owner = Window.GetWindow(this) };

        if (dialog.ShowDialog() == true && dialog.TagName is not null)
        {
            viewModel.AddTagToCheckedGames(dialog.TagName);
        }
    }

    private void OnBulkSetStatusClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } viewModel)
        {
            return;
        }

        var message = string.Format(App.Localization["Catalog.BulkSetStatus.Message"], viewModel.CheckedRowCount);
        var dialog = new BulkSetCollectionStatusWindow(message) { Owner = Window.GetWindow(this) };

        if (dialog.ShowDialog() == true && dialog.SelectedStatus is not null)
        {
            viewModel.SetCollectionStatusForCheckedGames(dialog.SelectedStatus);
        }
    }

    private void OnBulkExportCsvClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } viewModel)
        {
            return;
        }

        var dialog = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "RomsCollect-export.csv" };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            File.WriteAllText(dialog.FileName, viewModel.BuildCsvForCheckedGames());
        }
    }
}
