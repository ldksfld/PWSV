using FluentValidation;
using PWSV.Application.Common.Validation;

namespace PWSV.Application.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username).Username();

        RuleFor(x => x.Password).StrongPassword();

        RuleFor(x => x.MasterPassword)
            .NotEmpty()
            .Length(8, ValidationRules.MaxSecretLength);
    }
}
