using FluentValidation;

namespace PWSV.Application.Accounts.Commands.UpdateAccount;

public sealed class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().Length(1, 128);
        RuleFor(x => x.AccountNumber).MaximumLength(64);
    }
}
