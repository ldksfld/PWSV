using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using PWSV.Application.Auth;
using PWSV.Application.Auth.Queries.GetDemoAccount;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.Entities;

namespace PWSV.Application.Tests.Auth;

public sealed class GetDemoAccountQueryHandlerTests
{
    private static readonly DemoAccountOptions Demo = new()
    {
        Username = "demo",
        Password = "Demo#Pass2026",
        MasterPassword = "Demo#Master2026"
    };

    [Fact]
    public async Task Handle_WhenDemoUserKeepsDemoPassword_ReturnsConfiguredCredentials()
    {
        await using var db = CreateContext();
        await SeedUserAsync(db, "demo", "demo-hash");
        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Verify("Demo#Pass2026", "demo-hash").Returns(true);

        var result = await new GetDemoAccountQueryHandler(db, hasher, Options.Create(Demo))
            .Handle(new GetDemoAccountQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Username.Should().Be("demo");
        result.Password.Should().Be("Demo#Pass2026");
        result.MasterPassword.Should().Be("Demo#Master2026");
    }

    [Fact]
    public async Task Handle_WhenDemoPasswordWasChanged_ReturnsNull()
    {
        await using var db = CreateContext();
        await SeedUserAsync(db, "demo", "changed-hash");
        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await new GetDemoAccountQueryHandler(db, hasher, Options.Create(Demo))
            .Handle(new GetDemoAccountQuery(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenOnlyOwnUserIsRegistered_ReturnsNullWithoutCheckingPassword()
    {
        await using var db = CreateContext();
        await SeedUserAsync(db, "olena", "own-hash");
        var hasher = Substitute.For<IPasswordHasher>();

        var result = await new GetDemoAccountQueryHandler(db, hasher, Options.Create(Demo))
            .Handle(new GetDemoAccountQuery(), CancellationToken.None);

        result.Should().BeNull();
        hasher.DidNotReceiveWithAnyArgs().Verify(default!, default!);
    }

    [Theory]
    [InlineData("", "Demo#Pass2026", "Demo#Master2026")]
    [InlineData("demo", " ", "Demo#Master2026")]
    [InlineData("demo", "Demo#Pass2026", "")]
    public async Task Handle_WhenDemoAccountIsNotConfigured_ReturnsNull(string username, string password, string masterPassword)
    {
        await using var db = CreateContext();
        await SeedUserAsync(db, "demo", "demo-hash");
        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var options = new DemoAccountOptions { Username = username, Password = password, MasterPassword = masterPassword };

        var result = await new GetDemoAccountQueryHandler(db, hasher, Options.Create(options))
            .Handle(new GetDemoAccountQuery(), CancellationToken.None);

        result.Should().BeNull();
        hasher.DidNotReceiveWithAnyArgs().Verify(default!, default!);
    }

    private static async Task SeedUserAsync(TestApplicationDbContext db, string username, string passwordHash)
    {
        db.Users.Add(new User { Username = username, PasswordHash = passwordHash, CreatedAt = DateTime.UtcNow });
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
