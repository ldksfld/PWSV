using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PWSV.Application.Categories.Commands.CreateCategory;
using PWSV.Application.Categories.Commands.DeactivateCategory;
using PWSV.Application.Categories.Commands.UpdateCategory;
using PWSV.Application.Common.Exceptions;
using PWSV.Application.Common.Interfaces;
using PWSV.Domain.Entities;
using PWSV.Domain.Enums;

namespace PWSV.Application.Tests.Categories;

public sealed class CategoryCommandHandlerTests
{
    private const int Transport = 1;
    private const int Taxi = 2;
    private const int TaxiNight = 3;
    private const int Food = 4;
    private const int Salary = 5;

    [Fact]
    public async Task Update_RenamingToExistingSiblingName_ThrowsConflict()
    {
        await using var db = await CreateSeededContextAsync();

        var act = () => CreateUpdateHandler(db).Handle(new UpdateCategoryCommand(Food, "транспорт", null), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Update_MovingUnderOwnDescendant_ThrowsConflict()
    {
        await using var db = await CreateSeededContextAsync();

        var act = () => CreateUpdateHandler(db).Handle(new UpdateCategoryCommand(Transport, "Транспорт", TaxiNight), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Update_MovingUnderCategoryOfOtherKind_ThrowsConflict()
    {
        await using var db = await CreateSeededContextAsync();

        var act = () => CreateUpdateHandler(db).Handle(new UpdateCategoryCommand(Food, "Харчування", Salary), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Update_DeactivatingParent_DeactivatesWholeSubtree()
    {
        await using var db = await CreateSeededContextAsync();

        await CreateUpdateHandler(db).Handle(new UpdateCategoryCommand(Transport, "Транспорт", null, false), CancellationToken.None);

        var inactive = await db.Categories.Where(c => !c.IsActive).Select(c => c.Id).ToListAsync();
        inactive.Should().BeEquivalentTo([Transport, Taxi, TaxiNight]);
    }

    [Fact]
    public async Task Update_ReactivatingChildOfInactiveParent_ThrowsConflict()
    {
        await using var db = await CreateSeededContextAsync();
        await CreateDeactivateHandler(db).Handle(new DeactivateCategoryCommand(Transport), CancellationToken.None);

        var act = () => CreateUpdateHandler(db).Handle(new UpdateCategoryCommand(Taxi, "Таксі", Transport, true), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Update_ReactivatingRootCategory_Succeeds()
    {
        await using var db = await CreateSeededContextAsync();
        await CreateDeactivateHandler(db).Handle(new DeactivateCategoryCommand(Transport), CancellationToken.None);

        var dto = await CreateUpdateHandler(db).Handle(new UpdateCategoryCommand(Transport, "Транспорт", null, true), CancellationToken.None);

        dto.IsActive.Should().BeTrue();
        (await db.Categories.SingleAsync(c => c.Id == Taxi)).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Deactivate_DeactivatesCategoryAndAllDescendants()
    {
        await using var db = await CreateSeededContextAsync();

        await CreateDeactivateHandler(db).Handle(new DeactivateCategoryCommand(Taxi), CancellationToken.None);

        var inactive = await db.Categories.Where(c => !c.IsActive).Select(c => c.Id).ToListAsync();
        inactive.Should().BeEquivalentTo([Taxi, TaxiNight]);
    }

    [Fact]
    public async Task Create_UnderInactiveParent_ThrowsConflict()
    {
        await using var db = await CreateSeededContextAsync();
        await CreateDeactivateHandler(db).Handle(new DeactivateCategoryCommand(Transport), CancellationToken.None);

        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTime.UtcNow);
        var handler = new CreateCategoryCommandHandler(db, CreateCurrentUser(), clock);

        var act = () => handler.Handle(new CreateCategoryCommand("Метро", CategoryKind.Expense, Transport), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    private static UpdateCategoryCommandHandler CreateUpdateHandler(TestApplicationDbContext db) =>
        new(db, CreateCurrentUser());

    private static DeactivateCategoryCommandHandler CreateDeactivateHandler(TestApplicationDbContext db) =>
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
        db.Categories.AddRange(
            CreateCategory(Transport, "Транспорт", CategoryKind.Expense, null),
            CreateCategory(Taxi, "Таксі", CategoryKind.Expense, Transport),
            CreateCategory(TaxiNight, "Нічне таксі", CategoryKind.Expense, Taxi),
            CreateCategory(Food, "Харчування", CategoryKind.Expense, null),
            CreateCategory(Salary, "Зарплата", CategoryKind.Income, null));
        await db.SaveChangesAsync();
        return db;
    }

    private static Category CreateCategory(int id, string name, CategoryKind kind, int? parentId) => new()
    {
        Id = id,
        UserId = 1,
        Name = name,
        Kind = kind,
        ParentCategoryId = parentId,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
}
