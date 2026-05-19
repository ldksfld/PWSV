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
}
