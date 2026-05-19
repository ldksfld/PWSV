using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Accounts.Dto;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.ValueObjects;

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

        account.Name = command.Name.Trim();
        account.IsActive = command.IsActive;
        account.UpdatedAt = clock.UtcNow;

        if (string.IsNullOrWhiteSpace(command.AccountNumber))
        {
            account.AccountNumberCipher = null;
            account.AccountNumberNonce = null;
            account.AccountNumberTag = null;
        }
        else
        {
            var encrypted = crypto.Encrypt(command.AccountNumber.Trim());
            account.AccountNumberCipher = encrypted.Cipher;
            account.AccountNumberNonce = encrypted.Nonce;
            account.AccountNumberTag = encrypted.Tag;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = account.Adapt<AccountDto>();
        var plain = TryDecrypt(account.AccountNumberCipher, account.AccountNumberNonce, account.AccountNumberTag);
        return dto with { AccountNumber = plain };
    }

    private string? TryDecrypt(byte[]? cipher, byte[]? nonce, byte[]? tag)
    {
        if (cipher is null || nonce is null || tag is null || cipher.Length == 0)
        {
            return null;
        }

        if (!crypto.IsReady)
        {
            return null;
        }

        return crypto.Decrypt(new EncryptedString(cipher, nonce, tag));
    }
}
