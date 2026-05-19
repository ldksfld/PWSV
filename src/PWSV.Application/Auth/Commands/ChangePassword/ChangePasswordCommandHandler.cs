using MediatR;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;

namespace PWSV.Application.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IPasswordHasher passwordHasher,
    IDateTimeProvider clock) : IRequestHandler<ChangePasswordCommand, Unit>
{
    public async Task<Unit> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Необхідна авторизація.");

        var user = await db.Users.FindAsync([userId], cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("User", userId);

        if (!passwordHasher.Verify(command.OldPassword, user.PasswordHash))
        {
            throw new UnauthorizedException("Старий пароль невірний.");
        }

        user.PasswordHash = passwordHasher.Hash(command.NewPassword);
        user.UpdatedAt = clock.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
