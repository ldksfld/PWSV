using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PWSV.Application.AccountTypes.Dto;
using PWSV.Application.AccountTypes.Queries.GetAccountTypesList;

namespace PWSV.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/account-types")]
public sealed class AccountTypesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountTypeDto>>> GetListAsync(CancellationToken cancellationToken)
    {
        var items = await mediator.Send(new GetAccountTypesListQuery(), cancellationToken).ConfigureAwait(false);
        return Ok(items);
    }
}
