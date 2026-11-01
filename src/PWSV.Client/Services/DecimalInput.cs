using System.Globalization;

namespace PWSV.Client.Services;

public static class DecimalInput
{
    public const decimal MaxAmount = 999_999_999_999.99m;
    public const int MaxAmountDecimalPlaces = 2;

    public static bool TryParse(string? input, out decimal result)
    {
        result = 0m;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var normalized = new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray())
            .Replace(',', '.');

        return decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out result);
    }

    public static string? ValidateAmount(decimal amount, int currencyDecimalPlaces, bool allowZero)
    {
        if (amount < 0m || (!allowZero && amount == 0m))
        {
            return allowZero ? "Сума не може бути від'ємною." : "Сума повинна бути більшою за нуль.";
        }

        if (amount > MaxAmount)
        {
            return "Сума не може перевищувати 999 999 999 999,99.";
        }

        var decimalPlaces = Math.Min(currencyDecimalPlaces, MaxAmountDecimalPlaces);
        if (decimal.Round(amount, decimalPlaces) != amount)
        {
            return decimalPlaces == 0
                ? "Сума в цій валюті має бути цілим числом."
                : $"Сума може містити не більше {decimalPlaces} знаків після коми.";
        }

        return null;
    }

    public static string Format(decimal value)
        => value.ToString(CultureInfo.InvariantCulture);

    public static string Format(decimal? value)
        => value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
}
