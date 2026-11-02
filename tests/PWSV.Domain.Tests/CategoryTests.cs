using FluentAssertions;
using PWSV.Domain.Entities;
using PWSV.Domain.Enums;
using PWSV.Domain.Exceptions;

namespace PWSV.Domain.Tests;

public sealed class CategoryTests
{
    [Theory]
    [InlineData(CategoryKind.Income, TransactionKind.Income)]
    [InlineData(CategoryKind.Expense, TransactionKind.Expense)]
    public void EnsureMatches_WithMatchingKinds_DoesNotThrow(CategoryKind categoryKind, TransactionKind transactionKind)
    {
        var category = new Category { Kind = categoryKind };
        var act = () => category.EnsureMatches(transactionKind);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(CategoryKind.Income, TransactionKind.Expense)]
    [InlineData(CategoryKind.Expense, TransactionKind.Income)]
    [InlineData(CategoryKind.Income, TransactionKind.Transfer)]
    [InlineData(CategoryKind.Expense, TransactionKind.Transfer)]
    public void EnsureMatches_WithMismatchedKinds_ThrowsCategoryTypeMismatch(CategoryKind categoryKind, TransactionKind transactionKind)
    {
        var category = new Category { Kind = categoryKind };
        var act = () => category.EnsureMatches(transactionKind);
        act.Should().Throw<CategoryTypeMismatchException>();
    }

    [Theory]
    [InlineData(CategoryKind.Expense, TransactionKind.Income, "Тип категорії «витрата» не відповідає типу транзакції «дохід».")]
    [InlineData(CategoryKind.Income, TransactionKind.Expense, "Тип категорії «дохід» не відповідає типу транзакції «витрата».")]
    [InlineData(CategoryKind.Income, TransactionKind.Transfer, "Тип категорії «дохід» не відповідає типу транзакції «переказ».")]
    public void EnsureMatches_WithMismatchedKinds_ReportsUkrainianKindNames(CategoryKind categoryKind, TransactionKind transactionKind, string expectedMessage)
    {
        var category = new Category { Kind = categoryKind };

        var act = () => category.EnsureMatches(transactionKind);

        act.Should().Throw<CategoryTypeMismatchException>().WithMessage(expectedMessage);
    }
}
