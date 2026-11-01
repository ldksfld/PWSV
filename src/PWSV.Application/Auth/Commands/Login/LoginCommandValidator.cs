using FluentValidation;
using PWSV.Application.Common.Validation;

namespace PWSV.Application.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
        RuleFor(x => x.MasterPassword).NotEmpty().MaximumLength(ValidationRules.MaxSecretLength);
    }
}
