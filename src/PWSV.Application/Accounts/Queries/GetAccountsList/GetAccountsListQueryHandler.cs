using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Accounts.Dto;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;

namespace PWSV.Application.Accounts.Queries.GetAccountsList;

public sealed class GetAccountsListQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetAccountsListQuery, IReadOnlyList<AccountDto>>
{
    public async Task<IReadOnlyList<AccountDto>> Handle(GetAccountsListQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Необхідна авторизація.");

        var queryable = db.Accounts
            .AsNoTracking()
            .Include(a => a.AccountType)
            .Include(a => a.Currency)
            .Where(a => a.UserId == userId);

        if (!query.IncludeInactive)
        {
            queryable = queryable.Where(a => a.IsActive);
        }

        if (query.AccountTypeId.HasValue)
        {
            queryable = queryable.Where(a => a.AccountTypeId == query.AccountTypeId.Value);
        }

        if (query.CurrencyId.HasValue)
        {
            queryable = queryable.Where(a => a.CurrencyId == query.CurrencyId.Value);
        }

        queryable = query.SortBy switch
        {
            AccountSortField.Currency => queryable.OrderBy(a => a.Currency.Code).ThenBy(a => a.Name),
            _ => queryable.OrderBy(a => a.Name)
        };

        var accounts = await queryable
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return accounts.Adapt<List<AccountDto>>();
    }
}
