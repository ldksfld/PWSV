using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Accounts.Dto;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;

namespace PWSV.Application.Accounts.Commands.UpdateAccount;

public sealed class UpdateAccountCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ICryptoService crypto,
    IDateTimeProvider clock) : IRequestHandler<UpdateAccountCommand, AccountDto>
{
    public async Task<AccountDto> Handle(UpdateAccountCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Необхідна авторизація.");

        var account = await db.Accounts
            .Include(a => a.AccountType)
            .Include(a => a.Currency)
            .SingleOrDefaultAsync(a => a.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Account", command.Id);

        if (account.UserId != userId)
        {
            throw new ForbiddenException("Рахунок належить іншому користувачеві.");
        }

        if (account.IsActive && !command.IsActive && account.Balance != 0m)
        {
            throw new ConflictException("Деактивація можлива лише за нульового балансу.");
        }

        var accountNumber = string.IsNullOrWhiteSpace(command.AccountNumber) ? null : command.AccountNumber.Trim();

        account.Name = command.Name.Trim();
        account.IsActive = command.IsActive;
        account.UpdatedAt = clock.UtcNow;

        if (accountNumber is null)
        {
            account.AccountNumberCipher = null;
            account.AccountNumberNonce = null;
            account.AccountNumberTag = null;
        }
        else
        {
            var encrypted = crypto.Encrypt(accountNumber);
            account.AccountNumberCipher = encrypted.Cipher;
            account.AccountNumberNonce = encrypted.Nonce;
            account.AccountNumberTag = encrypted.Tag;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return account.Adapt<AccountDto>() with { AccountNumber = accountNumber };
    }
}
