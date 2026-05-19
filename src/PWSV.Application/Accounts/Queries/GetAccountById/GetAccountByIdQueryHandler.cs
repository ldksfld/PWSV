using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Accounts.Dto;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Transactions.Dto;
using PWSV.Domain.ValueObjects;

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

        var accountDto = account.Adapt<AccountDto>();
        var decryptedNumber = DecryptAccountNumber(
            account.AccountNumberCipher,
            account.AccountNumberNonce,
            account.AccountNumberTag);
        accountDto = accountDto with { AccountNumber = decryptedNumber };

        var transactions = await db.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Where(t => t.AccountId == query.Id)
            .OrderByDescending(t => t.OccurredAt)
            .Take(RecentTransactionsLimit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var transactionDtos = transactions
            .Select(t =>
            {
                var dto = t.Adapt<TransactionDto>();
                var description = DecryptDescription(
                    t.DescriptionCipher,
                    t.DescriptionNonce,
                    t.DescriptionTag);
                return dto with { Description = description };
            })
            .ToList();

        return new AccountDetailsDto
        {
            Account = accountDto,
            RecentTransactions = transactionDtos
        };
    }

    private string? DecryptAccountNumber(byte[]? cipher, byte[]? nonce, byte[]? tag)
    {
        if (cipher is null || nonce is null || tag is null || cipher.Length == 0 || !crypto.IsReady)
        {
            return null;
        }

        return crypto.Decrypt(new EncryptedString(cipher, nonce, tag));
    }

    private string? DecryptDescription(byte[]? cipher, byte[]? nonce, byte[]? tag)
    {
        if (cipher is null || nonce is null || tag is null || cipher.Length == 0 || !crypto.IsReady)
        {
            return null;
        }

        return crypto.Decrypt(new EncryptedString(cipher, nonce, tag));
    }
}
