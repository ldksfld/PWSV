using PWSV.Domain.Exceptions;

namespace PWSV.Domain.Entities;

public sealed class Currency
{
    public const byte MaxAmountDecimalPlaces = 2;

    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public byte DecimalPlaces { get; set; } = 2;

    public int AmountDecimalPlaces => Math.Min(DecimalPlaces, MaxAmountDecimalPlaces);

    public decimal RoundAmount(decimal amount) =>
        decimal.Round(amount, AmountDecimalPlaces, MidpointRounding.AwayFromZero);

    public void EnsureSupportsAmount(decimal amount)
    {
        if (decimal.Round(amount, AmountDecimalPlaces) != amount)
        {
            throw new AmountPrecisionException(Code, AmountDecimalPlaces);
        }
    }
}
