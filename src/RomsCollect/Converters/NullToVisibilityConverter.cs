// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RomsCollect.Converters;

/// <summary>
/// Collapses an element when the bound value is null or an empty string.
/// Pass "Invert" as the ConverterParameter to show the element instead when
/// the value is null/empty.
/// </summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isNullOrEmpty = value is null || (value is string text && text.Length == 0);
        if (string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase))
        {
            isNullOrEmpty = !isNullOrEmpty;
        }

        return isNullOrEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
