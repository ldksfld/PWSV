namespace PWSV.Domain.Entities;

public sealed class AccountType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public ICollection<Account> Accounts { get; set; } = [];
}
