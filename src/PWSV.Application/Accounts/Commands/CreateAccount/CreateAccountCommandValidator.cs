using FluentValidation;

namespace PWSV.Application.Accounts.Commands.CreateAccount;

public sealed class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Length(1, 128);
        RuleFor(x => x.AccountTypeId).GreaterThan(0);
        RuleFor(x => x.CurrencyId).GreaterThan(0);
        RuleFor(x => x.AccountNumber).MaximumLength(64);
        RuleFor(x => x.InitialBalance).GreaterThanOrEqualTo(0);
    }
}
