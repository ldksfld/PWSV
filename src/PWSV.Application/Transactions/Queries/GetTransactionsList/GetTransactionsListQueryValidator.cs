using FluentValidation;

namespace PWSV.Application.Transactions.Queries.GetTransactionsList;

public sealed class GetTransactionsListQueryValidator : AbstractValidator<GetTransactionsListQuery>
{
    public GetTransactionsListQueryValidator()
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
                .WithMessage("Дата \"до\" не може бути раніше дати \"від\".")
            .When(x => x.From.HasValue && x.To.HasValue);
        RuleFor(x => x.Kind).IsInEnum().When(x => x.Kind.HasValue);
        RuleFor(x => x.SortBy).IsInEnum();
        RuleFor(x => x.SortDirection).IsInEnum();
    }
}
