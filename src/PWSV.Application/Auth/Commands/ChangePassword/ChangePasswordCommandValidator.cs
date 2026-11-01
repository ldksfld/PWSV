using FluentValidation;
using PWSV.Application.Common.Validation;

namespace PWSV.Application.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.OldPassword).NotEmpty().MaximumLength(128);

        RuleFor(x => x.NewPassword).StrongPassword();

        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.OldPassword)
            .WithMessage("Новий пароль не повинен збігатися зі старим.");
    }
}
