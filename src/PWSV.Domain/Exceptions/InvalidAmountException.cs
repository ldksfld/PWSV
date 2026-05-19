namespace PWSV.Domain.Exceptions;

public sealed class InvalidAmountException : DomainException
{
    public InvalidAmountException(decimal amount)
        : base($"Сума транзакції повинна бути більше нуля. Отримано: {amount}.")
    {
        Amount = amount;
    }

    public decimal Amount { get; }
}
