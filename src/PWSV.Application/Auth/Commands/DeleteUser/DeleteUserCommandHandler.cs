using System.Data;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;

namespace PWSV.Application.Auth.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IPasswordHasher passwordHasher,
    ICryptoService cryptoService) : IRequestHandler<DeleteUserCommand, Unit>
{
    public async Task<Unit> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Необхідна авторизація.");

        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.KeyDerivationConfig)
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("User", userId);

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(DeleteUserCommand.Password), "Пароль невірний.")
            ]);
        }

        var config = user.KeyDerivationConfig
            ?? throw new ConflictException(
                "Параметри деривації ключа відсутні. Зверніться до адміністратора.");

        var verificationSamples = await MasterKeyVerifier
            .GetVerificationSamplesAsync(db, config, userId, cancellationToken)
            .ConfigureAwait(false);

        var keyAccepted = cryptoService.TryInitializeKey(
            command.MasterPassword,
            config.Salt,
            config.Iterations,
            config.KeyLength,
            verificationSamples);

        if (!keyAccepted)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(DeleteUserCommand.MasterPassword), "Майстер-пароль невірний.")
            ]);
        }

        await db.ExecuteInTransactionAsync(
            async ct =>
            {
                await db.Transactions.Where(t => t.Account.UserId == userId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await db.Accounts.Where(a => a.UserId == userId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await db.Categories.Where(c => c.UserId == userId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await db.ExchangeRates.ExecuteDeleteAsync(ct).ConfigureAwait(false);
                await db.Users.Where(u => u.Id == userId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
                return Unit.Value;
            },
            IsolationLevel.Serializable,
            cancellationToken).ConfigureAwait(false);

        cryptoService.Reset();
        return Unit.Value;
    }
}
