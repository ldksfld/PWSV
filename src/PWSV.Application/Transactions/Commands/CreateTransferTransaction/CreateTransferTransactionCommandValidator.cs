using FluentValidation;
using PWSV.Application.Common.Interfaces;

namespace PWSV.Application.Transactions.Commands.CreateTransferTransaction;

public sealed class CreateTransferTransactionCommandValidator : AbstractValidator<CreateTransferTransactionCommand>
{
    public CreateTransferTransactionCommandValidator(IDateTimeProvider clock)
    {
        RuleFor(x => x.SourceAccountId).GreaterThan(0);
        RuleFor(x => x.DestinationAccountId)
            .GreaterThan(0)
            .NotEqual(x => x.SourceAccountId)
                .WithMessage("Рахунок-джерело та рахунок-отримувач повинні відрізнятись.");
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.OccurredAt).LessThanOrEqualTo(clock.UtcNow.AddMinutes(1));
        RuleFor(x => x.Description).MaximumLength(512);
        When(
            x => x.ExchangeRate.HasValue,
            () => RuleFor(x => x.ExchangeRate.GetValueOrDefault()).GreaterThan(0m));
    }
}
