// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
using System.Windows.Data;

namespace RomsCollect.Converters;

/// <summary>
/// Binds an enum-valued property to a group of RadioButtons: each
/// RadioButton's IsChecked compares the bound enum to its ConverterParameter.
/// </summary>
public sealed class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null && parameter is not null && value.Equals(parameter);

    public object? ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? parameter : Binding.DoNothing;
}
