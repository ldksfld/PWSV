using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.Enums;
using PWSV.Domain.Exceptions;

namespace PWSV.Application.Transactions.Commands.DeleteTransaction;

public sealed class DeleteTransactionCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<DeleteTransactionCommand, Unit>
{
    public async Task<Unit> Handle(DeleteTransactionCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Необхідна авторизація.");

        var transaction = await db.Transactions
            .Include(t => t.Account)
            .SingleOrDefaultAsync(t => t.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Transaction", command.Id);

        if (transaction.Account.UserId != userId)
        {
            throw new ForbiddenException("Транзакція належить іншому користувачеві.");
        }

        if (!transaction.Account.IsActive)
        {
            throw new AccountIsInactiveException(transaction.AccountId);
        }

        if (transaction.Kind == TransactionKind.Transfer && transaction.LinkedTransactionId.HasValue)
        {
            var linked = await db.Transactions
                .Include(t => t.Account)
                .SingleOrDefaultAsync(t => t.Id == transaction.LinkedTransactionId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (linked is not null)
            {
                await db.LockAccountsAsync([transaction.AccountId, linked.AccountId], cancellationToken).ConfigureAwait(false);

                if (!linked.Account.IsActive)
                {
                    throw new AccountIsInactiveException(linked.AccountId);
                }

                var outgoing = transaction.IsOutgoing ? transaction : linked;
                outgoing.LinkedTransactionId = null;
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                db.Transactions.Remove(linked);
            }
        }

        db.Transactions.Remove(transaction);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
