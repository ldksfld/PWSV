using System.Globalization;

namespace PWSV.Client.Services;

public static class DecimalInput
{
    public static bool TryParse(string? input, out decimal result)
    {
        result = 0m;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var normalized = input.Trim().Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    public static string Format(decimal value)
        => value.ToString(CultureInfo.InvariantCulture);

    public static string Format(decimal? value)
        => value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
}
