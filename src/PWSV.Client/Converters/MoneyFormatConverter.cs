using System.Globalization;
using System.Windows.Data;

namespace PWSV.Client.Converters;

public sealed class MoneyFormatConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal amount)
        {
            return amount.ToString("N2", CultureInfo.GetCultureInfo("uk-UA"));
        }
        return value ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
