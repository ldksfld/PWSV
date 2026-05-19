using FluentValidation;

namespace PWSV.Application.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.OldPassword).NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .Length(8, 128)
            .Matches("[a-z]")
                .WithMessage("Пароль повинен містити щонайменше одну літеру нижнього регістру.")
            .Matches("[A-Z]")
                .WithMessage("Пароль повинен містити щонайменше одну літеру верхнього регістру.")
            .Matches("[0-9]")
                .WithMessage("Пароль повинен містити щонайменше одну цифру.");

        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.OldPassword)
            .WithMessage("Новий пароль не повинен співпадати зі старим.");
    }
}
