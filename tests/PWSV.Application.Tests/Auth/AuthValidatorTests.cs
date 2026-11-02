using FluentAssertions;
using PWSV.Application.Auth.Commands.Login;
using PWSV.Application.Auth.Commands.Register;

namespace PWSV.Application.Tests.Auth;

public sealed class AuthValidatorTests
{
    private const string ValidPassword = "Strong#Pass1";
    private const string ValidMasterPassword = "MasterKey#1";

    [Theory]
    [InlineData("olena", true)]
    [InlineData("  ole  ", true)]
    [InlineData("ol", false)]
    [InlineData("  ol  ", false)]
    [InlineData("", false)]
    public void Register_ValidatesTrimmedUsernameLength(string username, bool expectedValid)
    {
        var validator = new RegisterCommandValidator();

        var result = validator.Validate(new RegisterCommand(username, ValidPassword, ValidMasterPassword));

        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("Strong#Pass1", true)]
    [InlineData("Пароль2026", true)]
    [InlineData("Short1A", false)]
    [InlineData("alllowercase1", false)]
    [InlineData("ALLUPPERCASE1", false)]
    [InlineData("NoDigitsHere", false)]
    public void Register_RequiresStrongPassword(string password, bool expectedValid)
    {
        var validator = new RegisterCommandValidator();

        var result = validator.Validate(new RegisterCommand("olena", password, ValidMasterPassword));

        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(7, false)]
    [InlineData(8, true)]
    [InlineData(256, true)]
    [InlineData(257, false)]
    public void Register_LimitsMasterPasswordLength(int length, bool expectedValid)
    {
        var validator = new RegisterCommandValidator();

        var result = validator.Validate(new RegisterCommand("olena", ValidPassword, new string('m', length)));

        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("", ValidPassword, ValidMasterPassword)]
    [InlineData("olena", "", ValidMasterPassword)]
    [InlineData("olena", ValidPassword, "")]
    public void Login_RequiresAllCredentials(string username, string password, string masterPassword)
    {
        var validator = new LoginCommandValidator();

        var result = validator.Validate(new LoginCommand(username, password, masterPassword));

        result.IsValid.Should().BeFalse();
    }
}
