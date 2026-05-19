using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Transactions.Dto;
using PWSV.Domain.Enums;
using PWSV.Domain.Exceptions;

namespace PWSV.Application.Transactions.Commands.UpdateTransaction;

public sealed class UpdateTransactionCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ICryptoService crypto,
    IDateTimeProvider clock) : IRequestHandler<UpdateTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(UpdateTransactionCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Необхідна авторизація.");

        var transaction = await db.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .SingleOrDefaultAsync(t => t.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Transaction", command.Id);

        if (transaction.Account.UserId != userId)
        {
            throw new ForbiddenException("Транзакція належить іншому користувачеві.");
        }

        if (transaction.Kind == TransactionKind.Transfer)
        {
            throw new ConflictException(
                "Редагування транзакцій-переказів недоступне; видаліть переказ та створіть новий.");
        }

        if (command.CategoryId.HasValue && command.CategoryId.Value != transaction.CategoryId)
        {
            var category = await db.Categories
                .SingleOrDefaultAsync(c => c.Id == command.CategoryId.Value, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new NotFoundException("Category", command.CategoryId.Value);

            if (category.UserId != userId)
            {
                throw new ForbiddenException("Категорія належить іншому користувачеві.");
            }

            category.EnsureMatches(transaction.Kind);
            transaction.CategoryId = category.Id;
        }

        if (command.AccountId.HasValue && command.AccountId.Value != transaction.AccountId)
        {
            var newAccount = await db.Accounts
                .SingleOrDefaultAsync(a => a.Id == command.AccountId.Value, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new NotFoundException("Account", command.AccountId.Value);

            if (newAccount.UserId != userId)
            {
                throw new ForbiddenException("Рахунок належить іншому користувачеві.");
            }

            if (!newAccount.IsActive)
            {
                throw new AccountIsInactiveException(newAccount.Id);
            }

            if (newAccount.CurrencyId != transaction.Account.CurrencyId)
            {
                throw new ConflictException(
                    "Не можна перенести транзакцію у рахунок з іншою валютою.");
            }

            transaction.AccountId = newAccount.Id;
        }

        transaction.Amount = command.Amount;
        transaction.OccurredAt = command.OccurredAt;
        transaction.Counterparty = command.Counterparty?.Trim();
        transaction.UpdatedAt = clock.UtcNow;
        transaction.EnsurePositiveAmount();

        if (string.IsNullOrWhiteSpace(command.Description))
        {
            transaction.DescriptionCipher = null;
            transaction.DescriptionNonce = null;
            transaction.DescriptionTag = null;
        }
        else
        {
            var encrypted = crypto.Encrypt(command.Description.Trim());
            transaction.DescriptionCipher = encrypted.Cipher;
            transaction.DescriptionNonce = encrypted.Nonce;
            transaction.DescriptionTag = encrypted.Tag;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = transaction.Adapt<TransactionDto>();
        return dto with { Description = command.Description };
    }
}
