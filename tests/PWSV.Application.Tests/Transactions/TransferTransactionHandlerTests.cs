using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Transactions.Commands.CreateTransferTransaction;
using PWSV.Application.Transactions.Commands.DeleteTransaction;
using PWSV.Application.Transactions.Dto;
using PWSV.Domain.Entities;
using PWSV.Domain.Exceptions;
using PWSV.Domain.ValueObjects;

namespace PWSV.Application.Tests.Transactions;

public sealed class TransferTransactionHandlerTests
{
    private const int UahCash = 1;
    private const int UahCard = 2;
    private const int UsdCard = 3;

    public TransferTransactionHandlerTests()
    {
        MappingSetup.EnsureRegistered();
    }

    [Fact]
    public async Task Create_SameCurrency_LinksBothLegsWithDirections()
    {
        await using var db = await CreateSeededContextAsync();

        var result = await CreateTransferHandler(db).Handle(
            new CreateTransferTransactionCommand(UahCash, UahCard, 300m, null, DateTime.UtcNow, "  Поповнення картки  "),
            CancellationToken.None);

        result.Source.Direction.Should().Be(TransactionDirection.Outgoing);
        result.Destination.Direction.Should().Be(TransactionDirection.Incoming);
        result.Source.LinkedTransactionId.Should().Be(result.Destination.Id);
        result.Destination.LinkedTransactionId.Should().Be(result.Source.Id);
        result.Destination.Amount.Should().Be(300m);
        result.Source.Description.Should().Be("Поповнення картки");
    }

    [Fact]
    public async Task Create_CrossCurrency_RoundsConvertedAmountToTwoDecimals()
    {
        await using var db = await CreateSeededContextAsync();

        var result = await CreateTransferHandler(db).Handle(
            new CreateTransferTransactionCommand(UahCash, UsdCard, 1000m, 0.02412345m, DateTime.UtcNow, null),
            CancellationToken.None);

        result.Destination.Amount.Should().Be(24.12m);
    }

    [Fact]
    public async Task Create_CrossCurrencyResultingInZero_ThrowsConflict()
    {
        await using var db = await CreateSeededContextAsync();

        var act = () => CreateTransferHandler(db).Handle(
            new CreateTransferTransactionCommand(UahCash, UsdCard, 0.10m, 0.024m, DateTime.UtcNow, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        (await db.Transactions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Create_CrossCurrencyWithoutRate_ThrowsConflict()
    {
        await using var db = await CreateSeededContextAsync();

        var act = () => CreateTransferHandler(db).Handle(
            new CreateTransferTransactionCommand(UahCash, UsdCard, 100m, null, DateTime.UtcNow, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_TransferByEitherLeg_RemovesBothLegs(bool deleteBySource)
    {
        await using var db = await CreateSeededContextAsync();
        var transfer = await CreateTransferHandler(db).Handle(
            new CreateTransferTransactionCommand(UahCash, UahCard, 300m, null, DateTime.UtcNow, null),
            CancellationToken.None);

        var legId = deleteBySource ? transfer.Source.Id : transfer.Destination.Id;
        await CreateDeleteHandler(db).Handle(new DeleteTransactionCommand(legId), CancellationToken.None);

        (await db.Transactions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Delete_TransferWhenLinkedAccountIsInactive_ThrowsAndKeepsBothLegs()
    {
        await using var db = await CreateSeededContextAsync();
        var transfer = await CreateTransferHandler(db).Handle(
            new CreateTransferTransactionCommand(UahCash, UahCard, 300m, null, DateTime.UtcNow, null),
            CancellationToken.None);

        var card = await db.Accounts.SingleAsync(a => a.Id == UahCard);
        card.IsActive = false;
        await db.SaveChangesAsync();

        var act = () => CreateDeleteHandler(db).Handle(new DeleteTransactionCommand(transfer.Source.Id), CancellationToken.None);

        await act.Should().ThrowAsync<AccountIsInactiveException>();
        (await db.Transactions.CountAsync()).Should().Be(2);
    }

    private static CreateTransferTransactionCommandHandler CreateTransferHandler(TestApplicationDbContext db)
    {
        var crypto = Substitute.For<ICryptoService>();
        crypto.Encrypt(Arg.Any<string>()).Returns(new EncryptedString([1], [2], [3]));

        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTime.UtcNow);

        return new CreateTransferTransactionCommandHandler(db, CreateCurrentUser(), crypto, clock);
    }

    private static DeleteTransactionCommandHandler CreateDeleteHandler(TestApplicationDbContext db) =>
        new(db, CreateCurrentUser());

    private static ICurrentUserService CreateCurrentUser()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(1);
        return currentUser;
    }

    private static async Task<TestApplicationDbContext> CreateSeededContextAsync()
    {
        var options = new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new TestApplicationDbContext(options);

        db.Users.Add(new User { Id = 1, Username = "ivan", PasswordHash = "hash", CreatedAt = DateTime.UtcNow });
        db.AccountTypes.Add(new AccountType { Id = 1, Code = "CASH", DisplayName = "Готівка" });
        db.Currencies.AddRange(
            new Currency { Id = 1, Code = "UAH", Name = "Гривня", Symbol = "₴", DecimalPlaces = 2 },
            new Currency { Id = 2, Code = "USD", Name = "Долар США", Symbol = "$", DecimalPlaces = 2 });
        db.Accounts.AddRange(
            CreateAccount(UahCash, "Готівка", 1),
            CreateAccount(UahCard, "Картка", 1),
            CreateAccount(UsdCard, "Картка USD", 2));
        await db.SaveChangesAsync();
        return db;
    }

    private static Account CreateAccount(int id, string name, int currencyId) => new()
    {
        Id = id,
        UserId = 1,
        Name = name,
        AccountTypeId = 1,
        CurrencyId = currencyId,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
}
