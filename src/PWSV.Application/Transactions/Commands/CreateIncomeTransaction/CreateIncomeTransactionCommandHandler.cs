using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Transactions.Dto;
using PWSV.Domain.Entities;
using PWSV.Domain.Enums;
using PWSV.Domain.Exceptions;

namespace PWSV.Application.Transactions.Commands.CreateIncomeTransaction;

public sealed class CreateIncomeTransactionCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ICryptoService crypto,
    IDateTimeProvider clock) : IRequestHandler<CreateIncomeTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(CreateIncomeTransactionCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Необхідна авторизація.");

        var account = await db.Accounts
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

        category.EnsureMatches(TransactionKind.Income);

        var transaction = new Transaction
        {
            AccountId = command.AccountId,
            CategoryId = command.CategoryId,
            Kind = TransactionKind.Income,
            Amount = command.Amount,
            OccurredAt = command.OccurredAt,
            Counterparty = command.Counterparty?.Trim(),
            CreatedAt = clock.UtcNow
        };
        transaction.EnsurePositiveAmount();

        if (!string.IsNullOrWhiteSpace(command.Description))
        {
            var encrypted = crypto.Encrypt(command.Description.Trim());
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

        var dto = saved.Adapt<TransactionDto>();
        return dto with { Description = command.Description };
    }
}
