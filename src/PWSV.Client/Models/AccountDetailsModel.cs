namespace PWSV.Client.Models;

public sealed record AccountDetailsModel
{
    public AccountModel Account { get; init; } = null!;
    public IReadOnlyList<TransactionModel> RecentTransactions { get; init; } = [];
}
