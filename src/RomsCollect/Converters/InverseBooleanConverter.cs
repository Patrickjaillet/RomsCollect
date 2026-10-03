// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
using System.Windows.Data;

namespace RomsCollect.Converters;

/// <summary>Negates a boolean value, for binding the "off" state of a two-way toggle.</summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool boolValue && !boolValue;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool boolValue && !boolValue;
}
