// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
using System.Runtime.Versioning;
using System.Windows.Data;
using RomsCollect.ViewModels;

namespace RomsCollect.Converters;

/// <summary>Resolves a CatalogColumnCategory (or its CollectionViewGroup wrapper) to its localized display name.</summary>
[SupportedOSPlatform("windows")]
public sealed class CatalogColumnCategoryToLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var category = value switch
        {
            CatalogColumnCategory direct => direct,
            System.Windows.Data.CollectionViewGroup group when group.Name is CatalogColumnCategory grouped => grouped,
            _ => (CatalogColumnCategory?)null,
        };

        return category switch
        {
            CatalogColumnCategory.Main => App.Localization["SelectColumns.Category.Main"],
            CatalogColumnCategory.Hardware => App.Localization["SelectColumns.Category.Hardware"],
            CatalogColumnCategory.Edition => App.Localization["SelectColumns.Category.Edition"],
            CatalogColumnCategory.Completeness => App.Localization["SelectColumns.Category.Completeness"],
            CatalogColumnCategory.Personal => App.Localization["SelectColumns.Category.Personal"],
            _ => value?.ToString() ?? string.Empty,
        };
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
