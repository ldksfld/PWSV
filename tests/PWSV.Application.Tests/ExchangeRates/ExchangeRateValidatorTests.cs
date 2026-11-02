using System.Globalization;
using FluentAssertions;
using NSubstitute;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.ExchangeRates.Commands.CreateExchangeRate;
using PWSV.Application.ExchangeRates.Queries.GetExchangeRates;

namespace PWSV.Application.Tests.ExchangeRates;

public sealed class ExchangeRateValidatorTests
{
    private static readonly DateTime Now = new(2026, 11, 1, 22, 30, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 11, 1);

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
        var validator = CreateValidator();

        var result = validator.Validate(new CreateExchangeRateCommand(1, 2, 41.5m, DateOnly.Parse(date, CultureInfo.InvariantCulture)));

        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExchangeRateCommand.EffectiveDate));
        }
    }

    [Theory]
    [InlineData(0.00000001, true)]
    [InlineData(4250000.12345678, true)]
    [InlineData(9999999999.9999, true)]
    [InlineData(0, false)]
    [InlineData(-41.5, false)]
    [InlineData(0.000000001, false)]
    [InlineData(10000000000, false)]
    public void ValidatesRatePrecisionAndRange(decimal rate, bool expectedValid)
    {
        var validator = CreateValidator();

        var result = validator.Validate(new CreateExchangeRateCommand(1, 2, rate, Today));

        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void Create_WithSameBaseAndQuoteCurrency_IsInvalid()
    {
        var validator = CreateValidator();

        var result = validator.Validate(new CreateExchangeRateCommand(3, 3, 1m, Today));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateExchangeRateCommand.QuoteCurrencyId));
    }

    [Theory]
    [InlineData("2026-10-01", "2026-10-31", true)]
    [InlineData("2026-10-31", "2026-10-31", true)]
    [InlineData("2026-10-31", "2026-10-01", false)]
    public void ListQuery_RequiresToNotBeforeFrom(string from, string to, bool expectedValid)
    {
        var validator = new GetExchangeRatesQueryValidator();
        var query = new GetExchangeRatesQuery(null, null, DateOnly.Parse(from, CultureInfo.InvariantCulture), DateOnly.Parse(to, CultureInfo.InvariantCulture));

        var result = validator.Validate(query);

        result.IsValid.Should().Be(expectedValid);
    }

    private static CreateExchangeRateCommandValidator CreateValidator()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        return new CreateExchangeRateCommandValidator(clock);
    }
}
