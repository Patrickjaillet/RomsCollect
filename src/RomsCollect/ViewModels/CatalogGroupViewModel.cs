// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;

namespace RomsCollect.ViewModels;

/// <summary>One folder/group of rows in the catalog list view.</summary>
public sealed class CatalogGroupViewModel
{
    public string GroupName { get; }
    public ObservableCollection<CatalogRowViewModel> Rows { get; }

    public CatalogGroupViewModel(string groupName, IEnumerable<CatalogRowViewModel> rows)
    {
        GroupName = groupName;
        Rows = new ObservableCollection<CatalogRowViewModel>(rows);
    }
}
