using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PWSV.Application.Auth.Commands.DeleteUser;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.Entities;
using PWSV.Domain.ValueObjects;

namespace PWSV.Application.Tests.Auth;

public sealed class DeleteUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithoutAuthenticatedUser_ThrowsForbidden()
    {
        await using var db = CreateContext();
        await SeedUserAsync(db);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns((int?)null);

        var handler = new DeleteUserCommandHandler(db, currentUser, CreateHasher(), Substitute.For<ICryptoService>());

        var act = () => handler.Handle(new DeleteUserCommand("StrongPass1", "MasterKey#1"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        (await db.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenUserNoLongerExists_ThrowsNotFound()
    {
        await using var db = CreateContext();
        var handler = new DeleteUserCommandHandler(db, CreateCurrentUser(), CreateHasher(), Substitute.For<ICryptoService>());

        var act = () => handler.Handle(new DeleteUserCommand("StrongPass1", "MasterKey#1"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithWrongPassword_RejectsPasswordFieldWithoutDerivingKey()
    {
        await using var db = CreateContext();
        await SeedUserAsync(db);
        var crypto = Substitute.For<ICryptoService>();

        var handler = new DeleteUserCommandHandler(db, CreateCurrentUser(), CreateHasher(), crypto);

        var act = () => handler.Handle(new DeleteUserCommand("WrongPass1", "MasterKey#1"), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainKey(nameof(DeleteUserCommand.Password))
            .WhoseValue.Should().Equal("Пароль невірний.");
        crypto.DidNotReceiveWithAnyArgs().TryInitializeKey(default!, default!, default, default, default!);
        crypto.DidNotReceive().Reset();
        (await db.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithWrongMasterPassword_RejectsMasterPasswordFieldAndKeepsSessionKey()
    {
        await using var db = CreateContext();
        await SeedUserAsync(db);
        var crypto = Substitute.For<ICryptoService>();
        crypto.TryInitializeKey(default!, default!, default, default, default!).ReturnsForAnyArgs(false);

        var handler = new DeleteUserCommandHandler(db, CreateCurrentUser(), CreateHasher(), crypto);

        var act = () => handler.Handle(new DeleteUserCommand("StrongPass1", "Typo#Master1"), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainKey(nameof(DeleteUserCommand.MasterPassword))
            .WhoseValue.Should().Equal("Майстер-пароль невірний.");
        crypto.Received(1).TryInitializeKey(
            "Typo#Master1",
            Arg.Is<byte[]>(salt => salt.SequenceEqual(new byte[] { 1, 2, 3 })),
            100_000,
            32,
            Arg.Is<IReadOnlyCollection<EncryptedString>>(samples =>
                samples.Count == 1 && samples.Single().Cipher.SequenceEqual(new byte[] { 11 })));
        crypto.DidNotReceive().Reset();
        (await db.Users.CountAsync()).Should().Be(1);
    }

    [Theory]
    [InlineData("", "MasterKey#1", nameof(DeleteUserCommand.Password))]
    [InlineData("StrongPass1", "", nameof(DeleteUserCommand.MasterPassword))]
    [InlineData("   ", "MasterKey#1", nameof(DeleteUserCommand.Password))]
    public void Validator_RequiresBothSecrets(string password, string masterPassword, string expectedProperty)
    {
        var validator = new DeleteUserCommandValidator();

        var result = validator.Validate(new DeleteUserCommand(password, masterPassword));

        result.Errors.Select(e => e.PropertyName).Should().Equal(expectedProperty);
    }

    [Fact]
    public void Validator_RejectsOverlongSecretsAndAcceptsValidOnes()
    {
        var validator = new DeleteUserCommandValidator();

        validator.Validate(new DeleteUserCommand(new string('a', 129), "MasterKey#1")).IsValid.Should().BeFalse();
        validator.Validate(new DeleteUserCommand("StrongPass1", new string('m', 257))).IsValid.Should().BeFalse();
        validator.Validate(new DeleteUserCommand("StrongPass1", "MasterKey#1")).IsValid.Should().BeTrue();
    }

    private static ICurrentUserService CreateCurrentUser()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(1);
        return currentUser;
    }

    private static IPasswordHasher CreateHasher()
    {
        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Verify("StrongPass1", "hash").Returns(true);
        return hasher;
    }

    private static async Task SeedUserAsync(TestApplicationDbContext db)
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
                KeyLength = 32,
                VerifierCipher = [11],
                VerifierNonce = [12],
                VerifierTag = [13]
            }
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
