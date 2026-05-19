using MediatR;
using PWSV.Application.Accounts.Dto;

namespace PWSV.Application.Accounts.Queries.GetAccountsList;

public enum AccountSortField
{
    Name = 0,
    Currency = 1
}

public sealed record GetAccountsListQuery(
    bool IncludeInactive,
    int? AccountTypeId,
    int? CurrencyId,
    AccountSortField SortBy = AccountSortField.Name) : IRequest<IReadOnlyList<AccountDto>>;
