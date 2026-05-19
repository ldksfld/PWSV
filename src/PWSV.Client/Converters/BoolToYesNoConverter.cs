using System.Globalization;
using System.Windows.Data;

namespace PWSV.Client.Converters;

public sealed class BoolToYesNoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && b ? "Так" : "Ні";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
