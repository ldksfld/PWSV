using FluentValidation;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Common.Validation;

namespace PWSV.Application.Transactions.Commands.UpdateTransaction;

public sealed class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionCommandValidator(IDateTimeProvider clock)
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.AccountId).GreaterThan(0).When(x => x.AccountId.HasValue);
        RuleFor(x => x.CategoryId).GreaterThan(0).When(x => x.CategoryId.HasValue);
        RuleFor(x => x.Amount).PositiveAmount();
        RuleFor(x => x.OccurredAt).OperationDate(clock);
        RuleFor(x => x.Description).MaximumLength(512);
        RuleFor(x => x.Counterparty).MaximumLength(256);
    }
}
