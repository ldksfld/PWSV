using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PWSV.Application.Accounts.Commands.CreateAccount;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.Entities;
using PWSV.Domain.Enums;
using PWSV.Domain.ValueObjects;

namespace PWSV.Application.Tests.Accounts;

public sealed class CreateAccountCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidData_CreatesAccountAndEncryptsAccountNumber()
    {
        await using var db = CreateContext();
        SeedReferenceData(db);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(1);

        var crypto = Substitute.For<ICryptoService>();
        crypto.Encrypt("1234").Returns(new EncryptedString([1, 2, 3], [4, 5], [6, 7]));

        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var handler = new CreateAccountCommandHandler(db, currentUser, crypto, clock);

        var dto = await handler.Handle(
            new CreateAccountCommand("Cash", 1, 1, "1234", 100m),
            CancellationToken.None);

        dto.Name.Should().Be("Cash");
        dto.AccountNumber.Should().Be("1234");

        var stored = await db.Accounts.SingleAsync();
        stored.AccountNumberCipher.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });

        var openingTx = await db.Transactions.SingleAsync();
        openingTx.Amount.Should().Be(100m);
        openingTx.Kind.Should().Be(TransactionKind.Income);

        var openingCategory = await db.Categories.SingleAsync(c => c.Name == CreateAccountCommandHandler.OpeningBalanceCategoryName);
        openingCategory.Kind.Should().Be(CategoryKind.Income);
    }

    [Fact]
    public async Task Handle_WithZeroBalance_DoesNotCreateOpeningTransaction()
    {
        await using var db = CreateContext();
        SeedReferenceData(db);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(1);

        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTime.UtcNow);

        var handler = new CreateAccountCommandHandler(db, currentUser, Substitute.For<ICryptoService>(), clock);

        await handler.Handle(new CreateAccountCommand("Empty", 1, 1, null, 0m), CancellationToken.None);

        (await db.Transactions.CountAsync()).Should().Be(0);
        (await db.Categories.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithUnknownAccountType_Throws()
    {
        await using var db = CreateContext();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(1);

        var handler = new CreateAccountCommandHandler(
            db,
            currentUser,
            Substitute.For<ICryptoService>(),
            Substitute.For<IDateTimeProvider>());

        var act = () => handler.Handle(new CreateAccountCommand("Cash", 999, 1, null, 0m), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithoutAuth_ThrowsForbidden()
    {
        await using var db = CreateContext();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns((int?)null);

        var handler = new CreateAccountCommandHandler(
            db,
            currentUser,
            Substitute.For<ICryptoService>(),
            Substitute.For<IDateTimeProvider>());

        var act = () => handler.Handle(new CreateAccountCommand("Cash", 1, 1, null, 0m), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    private static TestApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestApplicationDbContext(options);
    }

    private static void SeedReferenceData(TestApplicationDbContext db)
    {
        db.Users.Add(new User
        {
            Id = 1,
            Username = "ivan",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });
        db.AccountTypes.Add(new AccountType
        {
            Id = 1,
            Code = "CASH",
            DisplayName = "Готівка"
        });
        db.Currencies.Add(new Currency
        {
            Id = 1,
            Code = "UAH",
            Name = "Гривня",
            Symbol = "₴",
            DecimalPlaces = 2
        });
    }
}
