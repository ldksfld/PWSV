namespace PWSV.Domain.Exceptions;

public sealed class AmountPrecisionException : DomainException
{
    public AmountPrecisionException(string currencyCode, int decimalPlaces)
        : base(decimalPlaces == 0
            ? $"Сума у валюті {currencyCode} має бути цілим числом."
            : $"Сума у валюті {currencyCode} може містити не більше {decimalPlaces} знаків після коми.")
    {
        CurrencyCode = currencyCode;
        DecimalPlaces = decimalPlaces;
    }

    public string CurrencyCode { get; }
    public int DecimalPlaces { get; }
}
