using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PWSV.Application.Auth.Commands.ChangePassword;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.Entities;

namespace PWSV.Application.Tests.Auth;

public sealed class ChangePasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithWrongOldPassword_ThrowsValidationExceptionForOldPassword()
    {
        await using var db = CreateContext();
        await SeedUserAsync(db);

        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var handler = new ChangePasswordCommandHandler(db, CreateCurrentUser(), hasher, Substitute.For<IDateTimeProvider>());

        var act = () => handler.Handle(new ChangePasswordCommand("Wrong1pass", "NewPass#2026"), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainKey(nameof(ChangePasswordCommand.OldPassword));
    }

    [Fact]
    public async Task Handle_WithCorrectOldPassword_StoresNewHash()
    {
        await using var db = CreateContext();
        await SeedUserAsync(db);

        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Verify("OldPass#2026", "old-hash").Returns(true);
        hasher.Hash("NewPass#2026").Returns("new-hash");

        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc));

        var handler = new ChangePasswordCommandHandler(db, CreateCurrentUser(), hasher, clock);

        await handler.Handle(new ChangePasswordCommand("OldPass#2026", "NewPass#2026"), CancellationToken.None);

        var user = await db.Users.SingleAsync();
        user.PasswordHash.Should().Be("new-hash");
        user.UpdatedAt.Should().Be(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Validator_RejectsWeakAndUnchangedPasswords()
    {
        var validator = new ChangePasswordCommandValidator();

        validator.Validate(new ChangePasswordCommand("OldPass#2026", "short")).IsValid.Should().BeFalse();
        validator.Validate(new ChangePasswordCommand("OldPass#2026", "OldPass#2026")).IsValid.Should().BeFalse();
        validator.Validate(new ChangePasswordCommand("OldPass#2026", "НовийПароль2026")).IsValid.Should().BeTrue();
    }

    private static ICurrentUserService CreateCurrentUser()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(1);
        return currentUser;
    }

    private static async Task SeedUserAsync(TestApplicationDbContext db)
    {
        db.Users.Add(new User
        {
            Id = 1,
            Username = "ivan",
            PasswordHash = "old-hash",
            CreatedAt = DateTime.UtcNow
        });
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
