using FluentAssertions;
using PWSV.Application.Categories.Commands.CreateCategory;
using PWSV.Application.Categories.Commands.UpdateCategory;
using PWSV.Domain.Enums;

namespace PWSV.Application.Tests.Categories;

public sealed class CategoryValidatorTests
{
    [Theory]
    [InlineData("Продукти", CategoryKind.Expense, null, true)]
    [InlineData("Премія", CategoryKind.Income, 5, true)]
    [InlineData("", CategoryKind.Expense, null, false)]
    [InlineData("Продукти", (CategoryKind)'X', null, false)]
    [InlineData("Продукти", CategoryKind.Expense, 0, false)]
    [InlineData("Продукти", CategoryKind.Expense, -3, false)]
    public void Create_ValidatesNameKindAndParent(string name, CategoryKind kind, int? parentId, bool expectedValid)
    {
        var validator = new CreateCategoryCommandValidator();

        var result = validator.Validate(new CreateCategoryCommand(name, kind, parentId));

        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(1, "Ринок", null, true)]
    [InlineData(0, "Ринок", null, false)]
    [InlineData(1, "", null, false)]
    [InlineData(1, "Ринок", 0, false)]
    public void Update_ValidatesIdNameAndParent(int id, string name, int? parentId, bool expectedValid)
    {
        var validator = new UpdateCategoryCommandValidator();

        var result = validator.Validate(new UpdateCategoryCommand(id, name, parentId));

        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void Update_RejectsNameLongerThanColumn(int length, bool expectedValid)
    {
        var validator = new UpdateCategoryCommandValidator();

        var result = validator.Validate(new UpdateCategoryCommand(1, new string('Ж', length), null));

        result.IsValid.Should().Be(expectedValid);
    }
}
