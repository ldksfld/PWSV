using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PWSV.Application.ExchangeRates.Commands.CreateExchangeRate;
using PWSV.Application.ExchangeRates.Dto;
using PWSV.Application.ExchangeRates.Queries.GetExchangeRates;

namespace PWSV.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/exchange-rates")]
public sealed class ExchangeRatesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExchangeRateDto>>> GetAsync(
        [FromQuery] int? baseCurrencyId = null,
        [FromQuery] int? quoteCurrencyId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetExchangeRatesQuery(baseCurrencyId, quoteCurrencyId, from, to);
        var rates = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(rates);
    }

    [HttpPost]
    public async Task<ActionResult<ExchangeRateDto>> CreateAsync(
        [FromBody] CreateExchangeRateCommand command,
        CancellationToken cancellationToken)
    {
        var rate = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetAsync), null, rate);
    }
}
