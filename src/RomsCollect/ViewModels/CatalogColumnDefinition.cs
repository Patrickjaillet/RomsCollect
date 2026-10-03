// SPDX-License-Identifier: GPL-3.0-or-later
using CommunityToolkit.Mvvm.ComponentModel;

namespace RomsCollect.ViewModels;

/// <summary>The category a catalog column belongs to, for the "Select columns" dialog grouping.</summary>
public enum CatalogColumnCategory
{
    Main,
    Hardware,
    Edition,
    Completeness,
    Personal,
}

/// <summary>Identifies one available column in the catalog list view.</summary>
public enum CatalogColumnKind
{
    Title,
    Platform,
    Publisher,
    Developer,
    ReleaseDate,
    Series,
    Genre,
    Format,
    Region,
    Edition,
    Completeness,
    HasBox,
    HasManual,
    CollectionStatus,
    Completed,
    Condition,
    Rating,
    Location,
}

/// <summary>One selectable/orderable column definition for the catalog list view.</summary>
public sealed partial class CatalogColumnDefinition : ObservableObject
{
    public CatalogColumnKind Kind { get; }
    public CatalogColumnCategory Category { get; }
    public string DisplayName { get; }

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private int _sortOrder;

    public CatalogColumnDefinition(CatalogColumnKind kind, CatalogColumnCategory category, string displayName, bool isVisible, int sortOrder)
    {
        Kind = kind;
        Category = category;
        DisplayName = displayName;
        _isVisible = isVisible;
        _sortOrder = sortOrder;
    }
}
