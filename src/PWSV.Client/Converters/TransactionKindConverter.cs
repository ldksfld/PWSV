using System.Globalization;
using System.Windows.Data;

namespace PWSV.Client.Converters;

public sealed class TransactionKindConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value as string) switch
        {
            "Income" => "Дохід",
            "Expense" => "Витрата",
            "Transfer" => "Переказ",
            _ => value ?? string.Empty
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
