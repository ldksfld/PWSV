using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Transactions.Dto;
using PWSV.Domain.Entities;
using PWSV.Domain.Enums;
using PWSV.Domain.Exceptions;

namespace PWSV.Application.Transactions.Commands.CreateExpenseTransaction;

public sealed class CreateExpenseTransactionCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ICryptoService crypto,
    IDateTimeProvider clock) : IRequestHandler<CreateExpenseTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(CreateExpenseTransactionCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Необхідна авторизація.");

        var account = await db.Accounts
            .Include(a => a.Currency)
            .SingleOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Account", command.AccountId);

        if (account.UserId != userId)
        {
            throw new ForbiddenException("Рахунок належить іншому користувачеві.");
        }

        if (!account.IsActive)
        {
            throw new AccountIsInactiveException(account.Id);
        }

        var category = await db.Categories
            .SingleOrDefaultAsync(c => c.Id == command.CategoryId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Category", command.CategoryId);

        if (category.UserId != userId)
        {
            throw new ForbiddenException("Категорія належить іншому користувачеві.");
        }

        category.EnsureMatches(TransactionKind.Expense);

        if (!category.IsActive)
        {
            throw new ConflictException("Категорію деактивовано. Оберіть активну категорію.");
        }

        account.Currency.EnsureSupportsAmount(command.Amount);

        var description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();

        var transaction = new Transaction
        {
            AccountId = command.AccountId,
            CategoryId = command.CategoryId,
            Kind = TransactionKind.Expense,
            Amount = command.Amount,
            OccurredAt = command.OccurredAt,
            Counterparty = string.IsNullOrWhiteSpace(command.Counterparty) ? null : command.Counterparty.Trim(),
            CreatedAt = clock.UtcNow
        };
        transaction.EnsurePositiveAmount();

        if (description is not null)
        {
            var encrypted = crypto.Encrypt(description);
            transaction.DescriptionCipher = encrypted.Cipher;
            transaction.DescriptionNonce = encrypted.Nonce;
            transaction.DescriptionTag = encrypted.Tag;
        }

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var saved = await db.Transactions
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Category)
            .SingleAsync(t => t.Id == transaction.Id, cancellationToken)
            .ConfigureAwait(false);

        return saved.Adapt<TransactionDto>() with { Description = description };
    }
}
