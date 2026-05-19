using System.Globalization;
using PWSV.Domain.Exceptions;

namespace PWSV.Domain.ValueObjects;

public readonly record struct Money
{
    public decimal Amount { get; }
    public int CurrencyId { get; }

    public Money(decimal amount, int currencyId)
    {
        if (amount < 0)
        {
            throw new InvalidAmountException(amount);
        }

        Amount = amount;
        CurrencyId = currencyId;
    }

    public static Money Zero(int currencyId) => new(0m, currencyId);

    public override string ToString() => Amount.ToString("F2", CultureInfo.InvariantCulture);
}
