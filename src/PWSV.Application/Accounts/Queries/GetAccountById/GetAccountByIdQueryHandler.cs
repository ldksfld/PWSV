using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Accounts.Dto;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Transactions.Dto;

namespace PWSV.Application.Accounts.Queries.GetAccountById;

public sealed class GetAccountByIdQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ICryptoService crypto) : IRequestHandler<GetAccountByIdQuery, AccountDetailsDto>
{
    private const int RecentTransactionsLimit = 50;

    public async Task<AccountDetailsDto> Handle(GetAccountByIdQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Необхідна авторизація.");

        var account = await db.Accounts
            .AsNoTracking()
            .Include(a => a.AccountType)
            .Include(a => a.Currency)
            .SingleOrDefaultAsync(a => a.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Account", query.Id);

        if (account.UserId != userId)
        {
            throw new ForbiddenException("Рахунок належить іншому користувачеві.");
        }

        var accountDto = account.Adapt<AccountDto>() with
        {
            AccountNumber = crypto.DecryptOrNull(
                account.AccountNumberCipher,
                account.AccountNumberNonce,
                account.AccountNumberTag)
        };

        var transactions = await db.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Where(t => t.AccountId == query.Id)
            .OrderByDescending(t => t.OccurredAt)
            .ThenByDescending(t => t.Id)
            .Take(RecentTransactionsLimit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var transactionDtos = transactions
            .Select(t => t.Adapt<TransactionDto>() with
            {
                Description = crypto.DecryptOrNull(t.DescriptionCipher, t.DescriptionNonce, t.DescriptionTag)
            })
            .ToList();

        return new AccountDetailsDto
        {
            Account = accountDto,
            RecentTransactions = transactionDtos
        };
    }
}
