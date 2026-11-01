using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PWSV.Application.Auth.Commands.Login;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.Entities;
using PWSV.Domain.ValueObjects;

namespace PWSV.Application.Tests.Auth;

public sealed class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCredentials_PassesEncryptedSamplesAndIssuesToken()
    {
        await using var db = CreateContext();
        await SeedUserAsync(db, withEncryptedAccount: true);

        var crypto = Substitute.For<ICryptoService>();
        crypto.TryInitializeKey(default!, default!, default, default, default!).ReturnsForAnyArgs(true);

        var jwt = Substitute.For<IJwtService>();
        jwt.Generate(1, "ivan").Returns(new JwtTokenResult("token", DateTime.UtcNow.AddHours(8)));

        var handler = new LoginCommandHandler(db, CreateHasher(), crypto, jwt);

        var response = await handler.Handle(new LoginCommand("ivan", "StrongPass1", "MasterKey#1"), CancellationToken.None);

        response.AccessToken.Should().Be("token");
        crypto.Received(1).TryInitializeKey(
            "MasterKey#1",
            Arg.Any<byte[]>(),
            100_000,
            32,
            Arg.Is<IReadOnlyCollection<EncryptedString>>(samples => samples.Count == 1));
    }

    [Fact]
    public async Task Handle_WithWrongMasterPassword_ThrowsUnauthorizedAndDoesNotIssueToken()
    {
        await using var db = CreateContext();
        await SeedUserAsync(db, withEncryptedAccount: true);

        var crypto = Substitute.For<ICryptoService>();
        crypto.TryInitializeKey(default!, default!, default, default, default!).ReturnsForAnyArgs(false);

        var jwt = Substitute.For<IJwtService>();
        var handler = new LoginCommandHandler(db, CreateHasher(), crypto, jwt);

        var act = () => handler.Handle(new LoginCommand("ivan", "StrongPass1", "WrongMaster"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("*майстер-пароль*");
        jwt.DidNotReceiveWithAnyArgs().Generate(default, default!);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ThrowsUnauthorizedWithoutDerivingKey()
    {
        await using var db = CreateContext();
        await SeedUserAsync(db, withEncryptedAccount: false);

        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        var crypto = Substitute.For<ICryptoService>();

        var handler = new LoginCommandHandler(db, hasher, crypto, Substitute.For<IJwtService>());

        var act = () => handler.Handle(new LoginCommand("ivan", "bad", "MasterKey#1"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        crypto.DidNotReceiveWithAnyArgs().TryInitializeKey(default!, default!, default, default, default!);
    }

    private static IPasswordHasher CreateHasher()
    {
        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Verify("StrongPass1", "hash").Returns(true);
        return hasher;
    }

    private static async Task SeedUserAsync(TestApplicationDbContext db, bool withEncryptedAccount)
    {
        db.Users.Add(new User
        {
            Id = 1,
            Username = "ivan",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            KeyDerivationConfig = new KeyDerivationConfig
            {
                Salt = [1, 2, 3],
                Iterations = 100_000,
                KeyLength = 32
            }
        });

        if (withEncryptedAccount)
        {
            db.AccountTypes.Add(new AccountType { Id = 1, Code = "CASH", DisplayName = "Готівка" });
            db.Currencies.Add(new Currency { Id = 1, Code = "UAH", Name = "Гривня", Symbol = "₴", DecimalPlaces = 2 });
            db.Accounts.Add(new Account
            {
                Id = 1,
                UserId = 1,
                Name = "Картка",
                AccountTypeId = 1,
                CurrencyId = 1,
                AccountNumberCipher = [9],
                AccountNumberNonce = [8],
                AccountNumberTag = [7],
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }

    private static TestApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestApplicationDbContext(options);
    }
}
