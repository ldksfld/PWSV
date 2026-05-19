using FluentValidation;
using PWSV.Application.Common.Interfaces;

namespace PWSV.Application.Transactions.Commands.UpdateTransaction;

public sealed class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionCommandValidator(IDateTimeProvider clock)
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.OccurredAt).LessThanOrEqualTo(clock.UtcNow.AddMinutes(1));
        RuleFor(x => x.Description).MaximumLength(512);
        RuleFor(x => x.Counterparty).MaximumLength(256);
    }
}
