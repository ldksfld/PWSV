using System.Globalization;
using FluentAssertions;
using NSubstitute;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.ExchangeRates.Commands.CreateExchangeRate;

namespace PWSV.Application.Tests.ExchangeRates;

public sealed class ExchangeRateValidatorTests
{
    private static readonly DateTime Now = new(2026, 11, 1, 22, 30, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("2026-11-01", true)]
    [InlineData("2026-11-02", true)]
    [InlineData("1900-01-01", true)]
    [InlineData("2026-11-03", false)]
    [InlineData("2099-01-01", false)]
    [InlineData("1899-12-31", false)]
    [InlineData("0001-01-01", false)]
    public void ValidatesEffectiveDateRange(string date, bool expectedValid)
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        var validator = new CreateExchangeRateCommandValidator(clock);

        var result = validator.Validate(new CreateExchangeRateCommand(1, 2, 41.5m, DateOnly.Parse(date, CultureInfo.InvariantCulture)));

        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExchangeRateCommand.EffectiveDate));
        }
    }
}
