using FluentAssertions;
using PWSV.Application.Accounts.Commands.CreateAccount;
using PWSV.Application.Accounts.Commands.UpdateAccount;

namespace PWSV.Application.Tests.Accounts;

public sealed class AccountValidatorTests
{
    [Theory]
    [InlineData("Готівка", 1, 1, 0, true)]
    [InlineData("Депозит", 2, 3, 999_999_999_999.99, true)]
    [InlineData("", 1, 1, 0, false)]
    [InlineData("   ", 1, 1, 0, false)]
    [InlineData("Картка", 0, 1, 0, false)]
    [InlineData("Картка", 1, 0, 0, false)]
    [InlineData("Картка", 1, 1, -0.01, false)]
    [InlineData("Картка", 1, 1, 10.005, false)]
    [InlineData("Картка", 1, 1, 1_000_000_000_000, false)]
    public void Create_ValidatesNameReferencesAndInitialBalance(string name, int accountTypeId, int currencyId, decimal initialBalance, bool expectedValid)
    {
        var validator = new CreateAccountCommandValidator();

        var result = validator.Validate(new CreateAccountCommand(name, accountTypeId, currencyId, null, initialBalance));

        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void Create_RejectsNameLongerThanColumn(int length, bool expectedValid)
    {
        var validator = new CreateAccountCommandValidator();

        var result = validator.Validate(new CreateAccountCommand(new string('Я', length), 1, 1, null, 0m));

        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(64, true)]
    [InlineData(65, false)]
    public void Update_LimitsAccountNumberLength(int length, bool expectedValid)
    {
        var validator = new UpdateAccountCommandValidator();

        var result = validator.Validate(new UpdateAccountCommand(1, "Картка", new string('1', length), true));

        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void Update_WithNonPositiveId_IsInvalid()
    {
        var validator = new UpdateAccountCommandValidator();

        var result = validator.Validate(new UpdateAccountCommand(0, "Картка", null, true));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateAccountCommand.Id));
    }
}
