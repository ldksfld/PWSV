namespace PWSV.Domain.Exceptions;

public sealed class InvalidCurrencyException : DomainException
{
    public InvalidCurrencyException(int currencyId)
        : base($"Ідентифікатор валюти повинен бути додатним. Отримано: {currencyId}.")
    {
        CurrencyId = currencyId;
    }

    public int CurrencyId { get; }
}
