// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
using System.Windows.Data;

namespace RomsCollect.Converters;

/// <summary>
/// Binds a nullable string-valued property to a group of RadioButtons: each
/// RadioButton's IsChecked compares the bound string to its ConverterParameter.
/// </summary>
public sealed class StringEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string text && parameter is string comparand && string.Equals(text, comparand, StringComparison.OrdinalIgnoreCase);

    public object? ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? parameter : Binding.DoNothing;
}
