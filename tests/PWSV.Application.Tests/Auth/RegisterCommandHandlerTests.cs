using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PWSV.Application.Auth.Commands.Register;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.Entities;

namespace PWSV.Application.Tests.Auth;

public sealed class RegisterCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenNoUsersExist_CreatesUserAndIssuesToken()
    {
        await using var db = CreateContext();
        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Hash("StrongPass1").Returns("hashed");

        var jwt = Substitute.For<IJwtService>();
        jwt.Generate(Arg.Any<int>(), Arg.Any<string>()).Returns(new JwtTokenResult("token", DateTime.UtcNow.AddHours(8)));

        var crypto = Substitute.For<ICryptoService>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var handler = new RegisterCommandHandler(db, hasher, crypto, jwt, clock);

        var response = await handler.Handle(new RegisterCommand("ivan", "StrongPass1", "MasterKey"), CancellationToken.None);

        response.Username.Should().Be("ivan");
        response.AccessToken.Should().Be("token");
        (await db.Users.CountAsync()).Should().Be(1);
        crypto.Received(1).InitializeKey("MasterKey", Arg.Any<byte[]>(), 100_000, 32);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyExists_ThrowsConflict()
    {
        await using var db = CreateContext();
        db.Users.Add(new User
        {
            Username = "existing",
            PasswordHash = "x",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new RegisterCommandHandler(
            db,
            Substitute.For<IPasswordHasher>(),
            Substitute.For<ICryptoService>(),
            Substitute.For<IJwtService>(),
            Substitute.For<IDateTimeProvider>());

        var act = () => handler.Handle(new RegisterCommand("new", "Pass", "Master"), CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }

    private static TestApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestApplicationDbContext(options);
    }
}
