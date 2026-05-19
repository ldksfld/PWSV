using PWSV.Application.Transactions.Dto;

namespace PWSV.Application.Accounts.Dto;

public sealed record AccountDetailsDto
{
    public AccountDto Account { get; init; } = null!;
    public IReadOnlyList<TransactionDto> RecentTransactions { get; init; } = [];
}
