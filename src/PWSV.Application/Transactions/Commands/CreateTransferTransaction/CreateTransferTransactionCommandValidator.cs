using FluentValidation;
using PWSV.Application.Common.Interfaces;
using PWSV.Application.Common.Validation;

namespace PWSV.Application.Transactions.Commands.CreateTransferTransaction;

public sealed class CreateTransferTransactionCommandValidator : AbstractValidator<CreateTransferTransactionCommand>
{
    public CreateTransferTransactionCommandValidator(IDateTimeProvider clock)
    {
        RuleFor(x => x.SourceAccountId).GreaterThan(0);
        RuleFor(x => x.DestinationAccountId)
            .GreaterThan(0)
            .NotEqual(x => x.SourceAccountId)
                .WithMessage("Рахунок-джерело та рахунок-отримувач повинні відрізнятися.");
        RuleFor(x => x.Amount).PositiveAmount();
        RuleFor(x => x.OccurredAt).OperationDate(clock);
        RuleFor(x => x.Description).MaximumLength(512);
        RuleFor(x => x.ExchangeRate.GetValueOrDefault())
            .ExchangeRateValue()
            .OverridePropertyName(nameof(CreateTransferTransactionCommand.ExchangeRate))
            .When(x => x.ExchangeRate.HasValue);
    }
}
