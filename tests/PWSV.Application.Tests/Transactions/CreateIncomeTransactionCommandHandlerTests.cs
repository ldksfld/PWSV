using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Transactions.Commands.CreateIncomeTransaction;
using PWSV.Domain.Entities;
using PWSV.Domain.Enums;
using PWSV.Domain.Exceptions;
using PWSV.Domain.ValueObjects;

namespace PWSV.Application.Tests.Transactions;

public sealed class CreateIncomeTransactionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidIncome_PersistsTransaction()
    {
        await using var db = CreateContext();
        Seed(db);
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);

        var result = await handler.Handle(
            new CreateIncomeTransactionCommand(1, 1, 250m, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), "salary", null),
            CancellationToken.None);

        result.Amount.Should().Be(250m);
        result.Description.Should().Be("salary");
        (await db.Transactions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithInactiveAccount_ThrowsAccountInactive()
    {
        await using var db = CreateContext();
        Seed(db);
        await db.SaveChangesAsync();
        var account = await db.Accounts.SingleAsync();
        account.IsActive = false;
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);

        var act = () => handler.Handle(
            new CreateIncomeTransactionCommand(1, 1, 50m, DateTime.UtcNow, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<AccountIsInactiveException>();
    }

    [Fact]
    public async Task Handle_WithExpenseCategoryForIncome_ThrowsCategoryMismatch()
    {
        await using var db = CreateContext();
        Seed(db);
        await db.SaveChangesAsync();
        var category = await db.Categories.SingleAsync();
        category.Kind = CategoryKind.Expense;
        await db.SaveChangesAsync();

        var handler = BuildHandler(db);

        var act = () => handler.Handle(
            new CreateIncomeTransactionCommand(1, 1, 50m, DateTime.UtcNow, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<CategoryTypeMismatchException>();
    }

    private static CreateIncomeTransactionCommandHandler BuildHandler(TestApplicationDbContext db)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(1);

        var crypto = Substitute.For<ICryptoService>();
        crypto.Encrypt(Arg.Any<string>()).Returns(new EncryptedString([1], [2], [3]));

        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTime.UtcNow);

        return new CreateIncomeTransactionCommandHandler(db, currentUser, crypto, clock);
    }

    private static TestApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestApplicationDbContext(options);
    }

    private static void Seed(TestApplicationDbContext db)
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
        db.Accounts.Add(new Account
        {
            Id = 1,
            UserId = 1,
            Name = "Cash",
            AccountTypeId = 1,
            CurrencyId = 1,
            Balance = 0m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        db.Categories.Add(new Category
        {
            Id = 1,
            UserId = 1,
            Name = "Salary",
            Kind = CategoryKind.Income,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
    }
}
