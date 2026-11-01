using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PWSV.Application.Accounts.Commands.UpdateAccount;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.Entities;
using PWSV.Domain.ValueObjects;

namespace PWSV.Application.Tests.Accounts;

public sealed class UpdateAccountCommandHandlerTests
{
    [Fact]
    public async Task Handle_DeactivatingAccountWithNonZeroBalance_ThrowsConflict()
    {
        await using var db = CreateContext();
        await SeedAsync(db, balance: 150m);

        var handler = CreateHandler(db);

        var act = () => handler.Handle(new UpdateAccountCommand(1, "Картка", null, false), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        (await db.Accounts.SingleAsync()).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeactivatingAccountWithZeroBalance_Succeeds()
    {
        await using var db = CreateContext();
        await SeedAsync(db, balance: 0m);

        var dto = await CreateHandler(db).Handle(new UpdateAccountCommand(1, "Картка", null, false), CancellationToken.None);

        dto.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithAccountNumber_EncryptsTrimmedValueAndReturnsPlainText()
    {
        await using var db = CreateContext();
        await SeedAsync(db, balance: 10m);

        var crypto = Substitute.For<ICryptoService>();
        crypto.Encrypt("UA00 1234").Returns(new EncryptedString([1], [2], [3]));

        var dto = await CreateHandler(db, crypto).Handle(new UpdateAccountCommand(1, "  Картка  ", "  UA00 1234  ", true), CancellationToken.None);

        dto.Name.Should().Be("Картка");
        dto.AccountNumber.Should().Be("UA00 1234");
        (await db.Accounts.SingleAsync()).AccountNumberCipher.Should().Equal(1);
    }

    [Fact]
    public async Task Handle_WithEmptyAccountNumber_ClearsEncryptedNumber()
    {
        await using var db = CreateContext();
        await SeedAsync(db, balance: 10m);

        var dto = await CreateHandler(db).Handle(new UpdateAccountCommand(1, "Картка", "   ", true), CancellationToken.None);

        dto.AccountNumber.Should().BeNull();
        (await db.Accounts.SingleAsync()).AccountNumberCipher.Should().BeNull();
    }

    private static UpdateAccountCommandHandler CreateHandler(TestApplicationDbContext db, ICryptoService? crypto = null)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(1);

        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTime.UtcNow);

        return new UpdateAccountCommandHandler(db, currentUser, crypto ?? Substitute.For<ICryptoService>(), clock);
    }

    private static async Task SeedAsync(TestApplicationDbContext db, decimal balance)
    {
        db.Users.Add(new User { Id = 1, Username = "ivan", PasswordHash = "hash", CreatedAt = DateTime.UtcNow });
        db.AccountTypes.Add(new AccountType { Id = 1, Code = "BANK_CARD", DisplayName = "Банківська картка" });
        db.Currencies.Add(new Currency { Id = 1, Code = "UAH", Name = "Гривня", Symbol = "₴", DecimalPlaces = 2 });
        db.Accounts.Add(new Account
        {
            Id = 1,
            UserId = 1,
            Name = "Картка",
            AccountTypeId = 1,
            CurrencyId = 1,
            Balance = balance,
            IsActive = true,
            AccountNumberCipher = [5],
            AccountNumberNonce = [6],
            AccountNumberTag = [7],
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
