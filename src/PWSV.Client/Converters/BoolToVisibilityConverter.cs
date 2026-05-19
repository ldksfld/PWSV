using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PWSV.Client.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var visible = value switch
        {
            null => false,
            bool b => b,
            string s => !string.IsNullOrWhiteSpace(s),
            ICollection coll => coll.Count > 0,
            int n => n != 0,
            long l => l != 0L,
            _ => true
        };

        if (parameter is string s2 && string.Equals(s2, "Invert", StringComparison.OrdinalIgnoreCase))
        {
            visible = !visible;
        }

        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
