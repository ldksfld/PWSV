using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PWSV.Application.Accounts.Commands.CreateAccount;
using PWSV.Application.Accounts.Commands.DeactivateAccount;
using PWSV.Application.Accounts.Commands.UpdateAccount;
using PWSV.Application.Accounts.Dto;
using PWSV.Application.Accounts.Queries.GetAccountById;
using PWSV.Application.Accounts.Queries.GetAccountsList;

namespace PWSV.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/accounts")]
public sealed class AccountsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetListAsync(
        [FromQuery] bool includeInactive = false,
        [FromQuery] int? accountTypeId = null,
        [FromQuery] int? currencyId = null,
        [FromQuery] AccountSortField sortBy = AccountSortField.Name,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAccountsListQuery(includeInactive, accountTypeId, currencyId, sortBy);
        var accounts = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(accounts);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AccountDetailsDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var details = await mediator.Send(new GetAccountByIdQuery(id), cancellationToken).ConfigureAwait(false);
        return Ok(details);
    }

    [HttpPost]
    public async Task<ActionResult<AccountDto>> CreateAsync([FromBody] CreateAccountCommand command, CancellationToken cancellationToken)
    {
        var account = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = account.Id }, account);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AccountDto>> UpdateAsync(
        int id,
        [FromBody] UpdateAccountCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(new { error = "Id у шляху не співпадає з тілом запиту." });
        }

        var account = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Ok(account);
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeactivateAccountCommand(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
