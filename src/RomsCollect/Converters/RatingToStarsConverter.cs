// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
using System.Windows.Data;

namespace RomsCollect.Converters;

/// <summary>Converts a 0-10 rating into a 5-star Unicode string (e.g. 8 -> "★★★★☆").</summary>
public sealed class RatingToStarsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int rating)
        {
            return string.Empty;
        }

        var starCount = (int)Math.Round(rating / 2.0, MidpointRounding.AwayFromZero);
        starCount = Math.Clamp(starCount, 0, 5);
        return new string('★', starCount) + new string('☆', 5 - starCount);
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
