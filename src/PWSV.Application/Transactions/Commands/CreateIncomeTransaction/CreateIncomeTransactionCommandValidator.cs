using FluentValidation;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Common.Validation;

namespace PWSV.Application.Transactions.Commands.CreateIncomeTransaction;

public sealed class CreateIncomeTransactionCommandValidator : AbstractValidator<CreateIncomeTransactionCommand>
{
    public CreateIncomeTransactionCommandValidator(IDateTimeProvider clock)
    {
        RuleFor(x => x.AccountId).GreaterThan(0);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Amount).PositiveAmount();
        RuleFor(x => x.OccurredAt).OperationDate(clock);
        RuleFor(x => x.Description).MaximumLength(512);
        RuleFor(x => x.Counterparty).MaximumLength(256);
    }
}
