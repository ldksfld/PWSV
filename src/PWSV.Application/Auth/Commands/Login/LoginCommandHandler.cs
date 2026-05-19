using MediatR;
using Microsoft.EntityFrameworkCore;
using PWSV.Application.Auth.Dto;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;

namespace PWSV.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ICryptoService cryptoService,
    IJwtService jwtService) : IRequestHandler<LoginCommand, AuthResponseDto>
{
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

        cryptoService.InitializeKey(
            command.MasterPassword,
            config.Salt,
            config.Iterations,
            config.KeyLength);

        var token = jwtService.Generate(user.Id, user.Username);
        return new AuthResponseDto(user.Id, user.Username, token.AccessToken, token.ExpiresAtUtc);
    }
}
