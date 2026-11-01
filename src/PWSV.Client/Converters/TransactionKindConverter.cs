using System.Globalization;
using System.Windows.Data;
using PWSV.Client.Models;

namespace PWSV.Client.Converters;

public sealed class TransactionKindConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TransactionModel { Kind: "Transfer" } transfer)
        {
            return transfer.Direction == "Outgoing" ? "Переказ (списання)" : "Переказ (зарахування)";
        }

        var kind = value is TransactionModel transaction ? transaction.Kind : value as string;
        return kind switch
        {
            "Income" => "Дохід",
            "Expense" => "Витрата",
            "Transfer" => "Переказ",
            _ => kind ?? string.Empty
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
