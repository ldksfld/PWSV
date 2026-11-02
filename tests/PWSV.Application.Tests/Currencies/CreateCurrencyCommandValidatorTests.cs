using FluentAssertions;
using PWSV.Application.Currencies.Commands.CreateCurrency;

namespace PWSV.Application.Tests.Currencies;

public sealed class CreateCurrencyCommandValidatorTests
{
    [Theory]
    [InlineData("JPY", true)]
    [InlineData("usdt", true)]
    [InlineData(" CHF ", true)]
    [InlineData("ABCDEFGHIJ", true)]
    [InlineData("AB", false)]
    [InlineData("ABCDEFGHIJK", false)]
    [InlineData("ГРН", false)]
    [InlineData("US-D", false)]
    [InlineData("", false)]
    public void Validate_ChecksCurrencyCodeFormat(string code, bool expectedValid)
    {
        var validator = new CreateCurrencyCommandValidator();

        var result = validator.Validate(new CreateCurrencyCommand(code, "Валюта", "¤", 2));

        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(8, true)]
    [InlineData(9, false)]
    public void Validate_LimitsDecimalPlacesToDatabaseRange(byte decimalPlaces, bool expectedValid)
    {
        var validator = new CreateCurrencyCommandValidator();

        var result = validator.Validate(new CreateCurrencyCommand("SEK", "Шведська крона", "kr", decimalPlaces));

        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("", "kr")]
    [InlineData("Шведська крона", "")]
    [InlineData("Шведська крона", "123456789")]
    public void Validate_RequiresNameAndShortSymbol(string name, string symbol)
    {
        var validator = new CreateCurrencyCommandValidator();

        var result = validator.Validate(new CreateCurrencyCommand("SEK", name, symbol, 2));

        result.IsValid.Should().BeFalse();
    }
}
