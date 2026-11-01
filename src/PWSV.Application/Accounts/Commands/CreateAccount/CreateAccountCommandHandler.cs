using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Accounts.Dto;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.Entities;
using PWSV.Domain.Enums;

namespace PWSV.Application.Accounts.Commands.CreateAccount;

public sealed class CreateAccountCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ICryptoService crypto,
    IDateTimeProvider clock) : IRequestHandler<CreateAccountCommand, AccountDto>
{
    public const string OpeningBalanceCategoryName = "Початковий залишок";

    public async Task<AccountDto> Handle(CreateAccountCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Необхідна авторизація.");

        var accountTypeExists = await db.AccountTypes.AnyAsync(t => t.Id == command.AccountTypeId, cancellationToken).ConfigureAwait(false);
        if (!accountTypeExists)
        {
            throw new NotFoundException("AccountType", command.AccountTypeId);
        }

        var currency = await db.Currencies
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == command.CurrencyId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Currency", command.CurrencyId);

        currency.EnsureSupportsAmount(command.InitialBalance);

        var accountNumber = string.IsNullOrWhiteSpace(command.AccountNumber) ? null : command.AccountNumber.Trim();

        var account = new Account
        {
            UserId = userId,
            Name = command.Name.Trim(),
            AccountTypeId = command.AccountTypeId,
            CurrencyId = command.CurrencyId,
            Balance = 0m,
            IsActive = true,
            CreatedAt = clock.UtcNow
        };

        if (accountNumber is not null)
        {
            var encrypted = crypto.Encrypt(accountNumber);
            account.AccountNumberCipher = encrypted.Cipher;
            account.AccountNumberNonce = encrypted.Nonce;
            account.AccountNumberTag = encrypted.Tag;
        }

        db.Accounts.Add(account);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (command.InitialBalance > 0m)
        {
            var openingCategory = await EnsureOpeningBalanceCategoryAsync(userId, cancellationToken).ConfigureAwait(false);
            var opening = new Transaction
            {
                AccountId = account.Id,
                CategoryId = openingCategory.Id,
                Kind = TransactionKind.Income,
                Amount = command.InitialBalance,
                OccurredAt = clock.UtcNow,
                CreatedAt = clock.UtcNow
            };
            db.Transactions.Add(opening);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var saved = await db.Accounts
            .AsNoTracking()
            .Include(a => a.AccountType)
            .Include(a => a.Currency)
            .SingleAsync(a => a.Id == account.Id, cancellationToken)
            .ConfigureAwait(false);

        return saved.Adapt<AccountDto>() with { AccountNumber = accountNumber };
    }

    private async Task<Category> EnsureOpeningBalanceCategoryAsync(int userId, CancellationToken cancellationToken)
    {
        var existing = await db.Categories
            .Where(c => c.UserId == userId
                && c.ParentCategoryId == null
                && c.Kind == CategoryKind.Income
                && c.Name == OpeningBalanceCategoryName)
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return existing;
        }

        var created = new Category
        {
            UserId = userId,
            Name = OpeningBalanceCategoryName,
            Kind = CategoryKind.Income,
            IsActive = true,
            CreatedAt = clock.UtcNow
        };
        db.Categories.Add(created);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return created;
    }
}
