using System.Globalization;
using System.Windows.Data;

namespace PWSV.Client.Converters;

public sealed class EqualityMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is null || values.Length < 2)
        {
            return false;
        }

        var first = values[0];
        for (var i = 1; i < values.Length; i++)
        {
            if (!Equals(first, values[i]))
            {
                return false;
            }
        }

        return true;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
