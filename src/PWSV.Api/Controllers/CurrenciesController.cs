using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PWSV.Application.Currencies.Commands.CreateCurrency;
using PWSV.Application.Currencies.Dto;
using PWSV.Application.Currencies.Queries.GetCurrenciesList;

namespace PWSV.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/currencies")]
public sealed class CurrenciesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CurrencyDto>>> GetListAsync(CancellationToken cancellationToken)
    {
        var currencies = await mediator.Send(new GetCurrenciesListQuery(), cancellationToken).ConfigureAwait(false);
        return Ok(currencies);
    }

    [HttpPost]
    public async Task<ActionResult<CurrencyDto>> CreateAsync([FromBody] CreateCurrencyCommand command, CancellationToken cancellationToken)
    {
        var currency = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetListAsync), null, currency);
    }
}
