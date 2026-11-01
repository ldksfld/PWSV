using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Common.Validation;
using PWSV.Application.Transactions.Dto;
using PWSV.Domain.Entities;
using PWSV.Domain.Enums;
using PWSV.Domain.Exceptions;

namespace PWSV.Application.Transactions.Commands.CreateTransferTransaction;

public sealed class CreateTransferTransactionCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ICryptoService crypto,
    IDateTimeProvider clock) : IRequestHandler<CreateTransferTransactionCommand, TransferResult>
{
    public async Task<TransferResult> Handle(CreateTransferTransactionCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Необхідна авторизація.");

        await db.LockAccountsAsync([command.SourceAccountId, command.DestinationAccountId], cancellationToken).ConfigureAwait(false);

        var source = await db.Accounts
            .Include(a => a.Currency)
            .SingleOrDefaultAsync(a => a.Id == command.SourceAccountId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Account", command.SourceAccountId);

        var destination = await db.Accounts
            .Include(a => a.Currency)
            .SingleOrDefaultAsync(a => a.Id == command.DestinationAccountId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Account", command.DestinationAccountId);

        if (source.UserId != userId || destination.UserId != userId)
        {
            throw new ForbiddenException("Один з рахунків належить іншому користувачеві.");
        }

        if (!source.IsActive)
        {
            throw new AccountIsInactiveException(source.Id);
        }

        if (!destination.IsActive)
        {
            throw new AccountIsInactiveException(destination.Id);
        }

        var sameCurrency = source.CurrencyId == destination.CurrencyId;
        if (!sameCurrency && !command.ExchangeRate.HasValue)
        {
            throw new ConflictException(
                "Для переказу між рахунками у різних валютах необхідно вказати курс конверсії.");
        }

        source.Currency.EnsureSupportsAmount(command.Amount);

        var convertedAmount = sameCurrency
            ? command.Amount
            : destination.Currency.RoundAmount(command.Amount * command.ExchangeRate.GetValueOrDefault());

        if (convertedAmount <= 0m)
        {
            throw new ConflictException(
                "Сума зарахування після конвертації менша за мінімальну одиницю валюти отримувача.");
        }

        if (convertedAmount > ValidationRules.MaxAmount)
        {
            throw new ConflictException(
                "Сума зарахування після конвертації перевищує допустиму межу 999 999 999 999,99.");
        }

        var description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();
        var now = clock.UtcNow;

        var sourceTx = CreateTransfer(source.Id, command.Amount, command.OccurredAt, description, now);
        db.Transactions.Add(sourceTx);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var destinationTx = CreateTransfer(destination.Id, convertedAmount, command.OccurredAt, description, now);
        destinationTx.LinkedTransactionId = sourceTx.Id;
        db.Transactions.Add(destinationTx);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        sourceTx.LinkedTransactionId = destinationTx.Id;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var sourceDto = MapToDto(sourceTx, source, description);
        var destinationDto = MapToDto(destinationTx, destination, description);
        return new TransferResult(sourceDto, destinationDto);
    }

    private Transaction CreateTransfer(int accountId, decimal amount, DateTime occurredAt, string? description, DateTime now)
    {
        var transaction = new Transaction
        {
            AccountId = accountId,
            Kind = TransactionKind.Transfer,
            Amount = amount,
            OccurredAt = occurredAt,
            CreatedAt = now
        };
        transaction.EnsurePositiveAmount();

        if (description is not null)
        {
            var encrypted = crypto.Encrypt(description);
            transaction.DescriptionCipher = encrypted.Cipher;
            transaction.DescriptionNonce = encrypted.Nonce;
            transaction.DescriptionTag = encrypted.Tag;
        }

        return transaction;
    }

    private static TransactionDto MapToDto(Transaction transaction, Account account, string? description)
    {
        var dto = transaction.Adapt<TransactionDto>();
        return dto with
        {
            AccountName = account.Name,
            Description = description
        };
    }
}
