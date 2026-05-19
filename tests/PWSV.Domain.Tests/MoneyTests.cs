using FluentAssertions;
using PWSV.Domain.Exceptions;
using PWSV.Domain.ValueObjects;

namespace PWSV.Domain.Tests;

public sealed class MoneyTests
{
    [Fact]
    public void Constructor_WithNegativeAmount_ThrowsInvalidAmountException()
    {
        var act = () => new Money(-1m, 1);
        act.Should().Throw<InvalidAmountException>();
    }

    [Fact]
    public void Constructor_WithZeroAmount_AllowsZero()
    {
        var money = new Money(0m, 1);
        money.Amount.Should().Be(0m);
        money.CurrencyId.Should().Be(1);
    }

    [Fact]
    public void Zero_ReturnsZeroMoney()
    {
        var money = Money.Zero(2);
        money.Amount.Should().Be(0m);
        money.CurrencyId.Should().Be(2);
    }
}
