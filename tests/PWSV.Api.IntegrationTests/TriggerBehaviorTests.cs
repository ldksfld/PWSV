using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PWSV.Domain.Entities;
using PWSV.Domain.Enums;
using PWSV.Infrastructure.Persistence;

namespace PWSV.Api.IntegrationTests;

public sealed class TriggerBehaviorTests : IAsyncLifetime
{
    private readonly PwsvWebApplicationFactory _factory = new();

    public Task InitializeAsync()
    {
        _ = _factory.Services;
        return Task.CompletedTask;
    }
    public async Task DisposeAsync()
    {
        await _factory.DisposeDatabaseAsync();
        _factory.Dispose();
    }

    [Fact]
    public async Task InsertingIncome_IncreasesBalance()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var (user, account, incomeCategory) = await SeedAsync(db);

        db.Transactions.Add(new Transaction
        {
            AccountId = account.Id,
            CategoryId = incomeCategory.Id,
            Kind = TransactionKind.Income,
            Amount = 500m,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var refreshed = await db.Accounts.AsNoTracking().SingleAsync(a => a.Id == account.Id);
        refreshed.Balance.Should().Be(500m);
    }

    [Fact]
    public async Task DeletingExpense_RestoresBalance()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var (user, account, _) = await SeedAsync(db);
        var expenseCategory = new Category
        {
            UserId = user.Id,
            Name = "Food",
            Kind = CategoryKind.Expense,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Categories.Add(expenseCategory);
        await db.SaveChangesAsync();

        var expense = new Transaction
        {
            AccountId = account.Id,
            CategoryId = expenseCategory.Id,
            Kind = TransactionKind.Expense,
            Amount = 100m,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        db.Transactions.Add(expense);
        await db.SaveChangesAsync();

        (await db.Accounts.AsNoTracking().SingleAsync(a => a.Id == account.Id)).Balance.Should().Be(-100m);

        db.Transactions.Remove(expense);
        await db.SaveChangesAsync();

        (await db.Accounts.AsNoTracking().SingleAsync(a => a.Id == account.Id)).Balance.Should().Be(0m);
    }

    [Fact]
    public async Task Transfer_AdjustsBothAccountsCorrectly()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var (user, source, _) = await SeedAsync(db);
        var destination = new Account
        {
            UserId = user.Id,
            Name = "Savings",
            AccountTypeId = source.AccountTypeId,
            CurrencyId = source.CurrencyId,
            Balance = 0m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Accounts.Add(destination);
        await db.SaveChangesAsync();

        var sourceTx = new Transaction
        {
            AccountId = source.Id,
            Kind = TransactionKind.Transfer,
            Amount = 200m,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        db.Transactions.Add(sourceTx);
        await db.SaveChangesAsync();

        var destinationTx = new Transaction
        {
            AccountId = destination.Id,
            Kind = TransactionKind.Transfer,
            Amount = 200m,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            LinkedTransactionId = sourceTx.Id
        };
        db.Transactions.Add(destinationTx);
        await db.SaveChangesAsync();

        sourceTx.LinkedTransactionId = destinationTx.Id;
        await db.SaveChangesAsync();

        var sourceAccount = await db.Accounts.AsNoTracking().SingleAsync(a => a.Id == source.Id);
        var destinationAccount = await db.Accounts.AsNoTracking().SingleAsync(a => a.Id == destination.Id);

        sourceAccount.Balance.Should().Be(-200m);
        destinationAccount.Balance.Should().Be(200m);
    }

    private static async Task<(User user, Account account, Category incomeCategory)> SeedAsync(ApplicationDbContext db)
    {
        var user = new User
        {
            Username = "tester",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var cashType = await db.AccountTypes.FirstAsync(t => t.Code == "CASH");
        var uah = await db.Currencies.FirstAsync(c => c.Code == "UAH");

        var account = new Account
        {
            UserId = user.Id,
            Name = "Wallet",
            AccountTypeId = cashType.Id,
            CurrencyId = uah.Id,
            Balance = 0m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Accounts.Add(account);

        var category = new Category
        {
            UserId = user.Id,
            Name = "Salary",
            Kind = CategoryKind.Income,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Categories.Add(category);

        await db.SaveChangesAsync();

        return (user, account, category);
    }
}
