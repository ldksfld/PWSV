using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Auth.Dto;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.Entities;

namespace PWSV.Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ICryptoService cryptoService,
    IJwtService jwtService,
    IDateTimeProvider clock) : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private const int SaltLengthBytes = 32;
    private const int DefaultIterations = 100_000;
    private const int DefaultKeyLength = 32;

    public async Task<AuthResponseDto> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException(
                "Користувач вже зареєстрований. Повторна реєстрація заборонена.");
        }

        var salt = RandomNumberGenerator.GetBytes(SaltLengthBytes);
        cryptoService.InitializeKey(command.MasterPassword, salt, DefaultIterations, DefaultKeyLength);

        var keyDerivationConfig = new KeyDerivationConfig
        {
            Salt = salt,
            Iterations = DefaultIterations,
            KeyLength = DefaultKeyLength
        };
        MasterKeyVerifier.Store(keyDerivationConfig, cryptoService);

        var user = new User
        {
            Username = command.Username.Trim(),
            PasswordHash = passwordHasher.Hash(command.Password),
            CreatedAt = clock.UtcNow,
            KeyDerivationConfig = keyDerivationConfig
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var token = jwtService.Generate(user.Id, user.Username);
        return new AuthResponseDto(user.Id, user.Username, token.AccessToken, token.ExpiresAtUtc);
    }
}
