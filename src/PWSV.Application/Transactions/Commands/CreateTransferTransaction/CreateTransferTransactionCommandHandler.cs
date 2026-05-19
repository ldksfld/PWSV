using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
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

        var source = await db.Accounts
            .SingleOrDefaultAsync(a => a.Id == command.SourceAccountId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Account", command.SourceAccountId);

        var destination = await db.Accounts
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

        if (source.CurrencyId != destination.CurrencyId && !command.ExchangeRate.HasValue)
        {
            throw new ConflictException(
                "Для переказу між рахунками у різних валютах необхідно вказати курс конверсії.");
        }

        var convertedAmount = source.CurrencyId == destination.CurrencyId
            ? command.Amount
            : decimal.Round(
                command.Amount * command.ExchangeRate.GetValueOrDefault(),
                2,
                MidpointRounding.AwayFromZero);

        var sourceDescription = EncryptDescription(command.Description);
        var destinationDescription = EncryptDescription(command.Description);
        var now = clock.UtcNow;

        var sourceTx = new Transaction
        {
            AccountId = source.Id,
            Kind = TransactionKind.Transfer,
            Amount = command.Amount,
            OccurredAt = command.OccurredAt,
            DescriptionCipher = sourceDescription?.Cipher,
            DescriptionNonce = sourceDescription?.Nonce,
            DescriptionTag = sourceDescription?.Tag,
            CreatedAt = now
        };
        sourceTx.EnsurePositiveAmount();

        db.Transactions.Add(sourceTx);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var destinationTx = new Transaction
        {
            AccountId = destination.Id,
            Kind = TransactionKind.Transfer,
            Amount = convertedAmount,
            OccurredAt = command.OccurredAt,
            DescriptionCipher = destinationDescription?.Cipher,
            DescriptionNonce = destinationDescription?.Nonce,
            DescriptionTag = destinationDescription?.Tag,
            CreatedAt = now,
            LinkedTransactionId = sourceTx.Id
        };
        destinationTx.EnsurePositiveAmount();

        db.Transactions.Add(destinationTx);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        sourceTx.LinkedTransactionId = destinationTx.Id;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var sourceDto = MapToDto(sourceTx, source, command.Description);
        var destinationDto = MapToDto(destinationTx, destination, command.Description);
        return new TransferResult(sourceDto, destinationDto);
    }

    private (byte[] Cipher, byte[] Nonce, byte[] Tag)? EncryptDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var encrypted = crypto.Encrypt(description.Trim());
        return (encrypted.Cipher, encrypted.Nonce, encrypted.Tag);
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
