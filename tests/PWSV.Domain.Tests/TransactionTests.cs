using FluentAssertions;
using PWSV.Domain.Entities;
using PWSV.Domain.Enums;
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

    [Theory]
    [InlineData(TransactionKind.Income, null, false)]
    [InlineData(TransactionKind.Expense, null, true)]
    [InlineData(TransactionKind.Transfer, null, true)]
    [InlineData(TransactionKind.Transfer, 11L, true)]
    [InlineData(TransactionKind.Transfer, 9L, false)]
    public void IsOutgoing_ReflectsKindAndTransferLink(TransactionKind kind, long? linkedTransactionId, bool expected)
    {
        var transaction = new Transaction { Id = 10, Kind = kind, LinkedTransactionId = linkedTransactionId };
        transaction.IsOutgoing.Should().Be(expected);
    }
}
