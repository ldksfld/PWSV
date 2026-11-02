using FluentAssertions;
using PWSV.Application.Common.Exceptions;

namespace PWSV.Application.Tests.Common;

public sealed class NotFoundExceptionTests
{
    [Theory]
    [InlineData("Account", 5, "Рахунок з ідентифікатором 5 не знайдено.")]
    [InlineData("Category", 7, "Категорію з ідентифікатором 7 не знайдено.")]
    [InlineData("Transaction", 42L, "Транзакцію з ідентифікатором 42 не знайдено.")]
    [InlineData("Widget", 1, "Widget з ідентифікатором 1 не знайдено.")]
    public void Constructor_UsesUkrainianEntityTitleInMessage(string entityName, object key, string expected)
    {
        var exception = new NotFoundException(entityName, key);

        exception.Message.Should().Be(expected);
        exception.EntityName.Should().Be(entityName);
    }
}
