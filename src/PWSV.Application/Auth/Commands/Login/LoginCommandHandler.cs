using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Auth.Dto;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.ValueObjects;

namespace PWSV.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ICryptoService cryptoService,
    IJwtService jwtService) : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private const int VerificationSamplesPerSource = 3;

    public async Task<AuthResponseDto> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var username = command.Username.Trim();

        var user = await db.Users
            .Include(u => u.KeyDerivationConfig)
            .SingleOrDefaultAsync(u => u.Username == username, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Невірне ім'я користувача або пароль.");
        }

        var config = user.KeyDerivationConfig
            ?? throw new ConflictException(
                "Параметри деривації ключа відсутні. Зверніться до адміністратора.");

        var verificationSamples = await FindVerificationSamplesAsync(user.Id, cancellationToken).ConfigureAwait(false);

        var keyAccepted = cryptoService.TryInitializeKey(
            command.MasterPassword,
            config.Salt,
            config.Iterations,
            config.KeyLength,
            verificationSamples);

        if (!keyAccepted)
        {
            throw new UnauthorizedException("Невірний майстер-пароль.");
        }

        var token = jwtService.Generate(user.Id, user.Username);
        return new AuthResponseDto(user.Id, user.Username, token.AccessToken, token.ExpiresAtUtc);
    }

    private async Task<IReadOnlyCollection<EncryptedString>> FindVerificationSamplesAsync(int userId, CancellationToken cancellationToken)
    {
        var accountNumbers = await db.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId
                && a.AccountNumberCipher != null
                && a.AccountNumberNonce != null
                && a.AccountNumberTag != null)
            .OrderBy(a => a.Id)
            .Take(VerificationSamplesPerSource)
            .Select(a => new EncryptedString(a.AccountNumberCipher!, a.AccountNumberNonce!, a.AccountNumberTag!))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var descriptions = await db.Transactions
            .AsNoTracking()
            .Where(t => t.Account.UserId == userId
                && t.DescriptionCipher != null
                && t.DescriptionNonce != null
                && t.DescriptionTag != null)
            .OrderBy(t => t.Id)
            .Take(VerificationSamplesPerSource)
            .Select(t => new EncryptedString(t.DescriptionCipher!, t.DescriptionNonce!, t.DescriptionTag!))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. accountNumbers, .. descriptions];
    }
}
