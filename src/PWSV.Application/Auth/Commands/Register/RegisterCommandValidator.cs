using FluentValidation;

namespace PWSV.Application.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .Length(3, 64);

        RuleFor(x => x.Password)
            .NotEmpty()
            .Length(8, 128)
            .Matches("[a-z]")
                .WithMessage("Пароль повинен містити щонайменше одну літеру нижнього регістру.")
            .Matches("[A-Z]")
                .WithMessage("Пароль повинен містити щонайменше одну літеру верхнього регістру.")
            .Matches("[0-9]")
                .WithMessage("Пароль повинен містити щонайменше одну цифру.");

        RuleFor(x => x.MasterPassword)
            .NotEmpty()
            .MinimumLength(8);
    }
}
