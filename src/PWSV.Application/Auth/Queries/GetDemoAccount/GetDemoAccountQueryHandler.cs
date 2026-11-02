using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PWSV.Application.Auth.Dto;
using PWSV.Application.Common.Interfaces;

namespace PWSV.Application.Auth.Queries.GetDemoAccount;

public sealed class GetDemoAccountQueryHandler(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    IOptions<DemoAccountOptions> options) : IRequestHandler<GetDemoAccountQuery, DemoAccountDto?>
{
    public async Task<DemoAccountDto?> Handle(GetDemoAccountQuery query, CancellationToken cancellationToken)
    {
        var demo = options.Value;
        if (!demo.IsConfigured)
        {
            return null;
        }

        var passwordHash = await db.Users
            .AsNoTracking()
            .Where(u => u.Username == demo.Username)
            .Select(u => u.PasswordHash)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return passwordHash is not null && passwordHasher.Verify(demo.Password, passwordHash)
            ? new DemoAccountDto(demo.Username, demo.Password, demo.MasterPassword)
            : null;
    }
}
