using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;

namespace PWSV.Application.Accounts.Commands.DeactivateAccount;

public sealed class DeactivateAccountCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeProvider clock) : IRequestHandler<DeactivateAccountCommand, Unit>
{
    public async Task<Unit> Handle(DeactivateAccountCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Необхідна авторизація.");

        var account = await db.Accounts
            .SingleOrDefaultAsync(a => a.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Account", command.Id);

        if (account.UserId != userId)
        {
            throw new ForbiddenException("Рахунок належить іншому користувачеві.");
        }

        if (account.Balance != 0m)
        {
            throw new ConflictException("Деактивація можлива лише за нульового балансу.");
        }

        if (!account.IsActive)
        {
            return Unit.Value;
        }

        account.IsActive = false;
        account.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
