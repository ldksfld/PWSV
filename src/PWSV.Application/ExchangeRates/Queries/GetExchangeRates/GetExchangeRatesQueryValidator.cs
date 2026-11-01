using FluentValidation;

namespace PWSV.Application.ExchangeRates.Queries.GetExchangeRates;

public sealed class GetExchangeRatesQueryValidator : AbstractValidator<GetExchangeRatesQuery>
{
    public GetExchangeRatesQueryValidator()
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
                .WithMessage("Дата \"до\" не може бути раніше дати \"від\".")
            .When(x => x.From.HasValue && x.To.HasValue);
    }
}
