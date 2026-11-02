using FluentAssertions;
using PWSV.Application.Categories;
using PWSV.Domain.Entities;
using PWSV.Domain.Enums;

namespace PWSV.Application.Tests.Categories;

public sealed class CategoryHierarchyTests
{
    [Fact]
    public void GetDescendantIds_ForRootOfDeepTree_ReturnsAllLevels()
    {
        var categories = new[] { Create(1, null), Create(2, 1), Create(3, 2), Create(4, 3), Create(5, null), Create(6, 5) };

        var descendants = CategoryHierarchy.GetDescendantIds(categories, 1);

        descendants.Should().BeEquivalentTo([2, 3, 4]);
    }

    [Fact]
    public void GetDescendantIds_ForLeaf_ReturnsEmptySet()
    {
        var categories = new[] { Create(1, null), Create(2, 1) };

        var descendants = CategoryHierarchy.GetDescendantIds(categories, 2);

        descendants.Should().BeEmpty();
    }

    [Fact]
    public void GetDescendantIds_ForUnknownRoot_ReturnsEmptySet()
    {
        var categories = new[] { Create(1, null) };

        var descendants = CategoryHierarchy.GetDescendantIds(categories, 99);

        descendants.Should().BeEmpty();
    }

    [Fact]
    public void GetDescendantIds_WithCyclicData_TerminatesWithoutIncludingRoot()
    {
        var categories = new[] { Create(1, 3), Create(2, 1), Create(3, 2) };

        var descendants = CategoryHierarchy.GetDescendantIds(categories, 1);

        descendants.Should().BeEquivalentTo([2, 3]);
    }

    private static Category Create(int id, int? parentId) => new()
    {
        Id = id,
        UserId = 1,
        Name = $"Категорія {id}",
        Kind = CategoryKind.Expense,
        ParentCategoryId = parentId
    };
}
