using FluentAssertions;
using PWSV.Domain.Entities;
using PWSV.Domain.Exceptions;

namespace PWSV.Domain.Tests;

public sealed class CurrencyTests
{
    [Theory]
    [InlineData(2, 10.5)]
    [InlineData(2, 0.01)]
    [InlineData(8, 0.15)]
    [InlineData(0, 100)]
    public void EnsureSupportsAmount_WithinAllowedPrecision_DoesNotThrow(byte decimalPlaces, decimal amount)
    {
        var currency = new Currency { Code = "TST", DecimalPlaces = decimalPlaces };
        var act = () => currency.EnsureSupportsAmount(amount);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(2, 0.001)]
    [InlineData(8, 0.00012345)]
    [InlineData(0, 10.5)]
    public void EnsureSupportsAmount_BeyondAllowedPrecision_ThrowsAmountPrecisionException(byte decimalPlaces, decimal amount)
    {
        var currency = new Currency { Code = "TST", DecimalPlaces = decimalPlaces };
        var act = () => currency.EnsureSupportsAmount(amount);
        act.Should().Throw<AmountPrecisionException>();
    }

    [Theory]
    [InlineData(2, 2.415, 2.42)]
    [InlineData(8, 0.123456, 0.12)]
    [InlineData(0, 10.5, 11)]
    public void RoundAmount_RoundsAwayFromZeroToSupportedPrecision(byte decimalPlaces, decimal amount, decimal expected)
    {
        var currency = new Currency { Code = "TST", DecimalPlaces = decimalPlaces };
        currency.RoundAmount(amount).Should().Be(expected);
    }
}
