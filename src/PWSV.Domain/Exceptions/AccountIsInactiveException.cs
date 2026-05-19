namespace PWSV.Domain.Exceptions;

public sealed class AccountIsInactiveException : DomainException
{
    public AccountIsInactiveException(int accountId)
        : base($"Рахунок {accountId} деактивовано та недоступний для операцій.")
    {
        AccountId = accountId;
    }

    public int AccountId { get; }
}
