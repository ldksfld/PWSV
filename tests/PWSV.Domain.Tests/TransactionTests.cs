using FluentAssertions;
using PWSV.Domain.Entities;
using PWSV.Domain.Exceptions;

namespace PWSV.Domain.Tests;

public sealed class TransactionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.5)]
    public void EnsurePositiveAmount_WithNonPositiveAmount_Throws(decimal amount)
    {
        var transaction = new Transaction { Amount = amount };
        var act = () => transaction.EnsurePositiveAmount();
        act.Should().Throw<InvalidAmountException>();
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(100)]
    [InlineData(99999.99)]
    public void EnsurePositiveAmount_WithPositiveAmount_DoesNotThrow(decimal amount)
    {
        var transaction = new Transaction { Amount = amount };
        var act = () => transaction.EnsurePositiveAmount();
        act.Should().NotThrow();
    }
}
