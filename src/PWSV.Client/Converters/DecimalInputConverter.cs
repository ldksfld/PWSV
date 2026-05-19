using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PWSV.Client.Converters;

public sealed class DecimalInputConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            decimal d => d.ToString(CultureInfo.InvariantCulture),
            double dbl => dbl.ToString(CultureInfo.InvariantCulture),
            _ => string.Empty
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isNullable = Nullable.GetUnderlyingType(targetType) is not null;

        if (value is not string raw)
        {
            return DependencyProperty.UnsetValue;
        }

        var trimmed = raw.Trim();
        if (trimmed.Length == 0)
        {
            return isNullable ? null : 0m;
        }

        var normalized = trimmed.Replace(',', '.');

        if (normalized is "." or "-" or "-." or "+." or "+")
        {
            return DependencyProperty.UnsetValue;
        }

        if (decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return DependencyProperty.UnsetValue;
    }
}
