using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PWSV.Application.Common.Models;
using PWSV.Application.Transactions.Commands.CreateExpenseTransaction;
using PWSV.Application.Transactions.Commands.CreateIncomeTransaction;
using PWSV.Application.Transactions.Commands.CreateTransferTransaction;
using PWSV.Application.Transactions.Commands.DeleteTransaction;
using PWSV.Application.Transactions.Commands.UpdateTransaction;
using PWSV.Application.Transactions.Dto;
using PWSV.Application.Transactions.Queries.GetTransactionById;
using PWSV.Application.Transactions.Queries.GetTransactionsList;
using PWSV.Domain.Enums;

namespace PWSV.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/transactions")]
public sealed class TransactionsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<TransactionDto>>> GetListAsync(
        [FromQuery] int? accountId = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] TransactionKind? kind = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] TransactionSortField sortBy = TransactionSortField.OccurredAt,
        [FromQuery] SortDirection sortDirection = SortDirection.Descending,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTransactionsListQuery(accountId, categoryId, kind, from, to, page, pageSize, sortBy, sortDirection);
        var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<TransactionDto>> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var transaction = await mediator.Send(new GetTransactionByIdQuery(id), cancellationToken).ConfigureAwait(false);
        return Ok(transaction);
    }

    [HttpPost("income")]
    public async Task<ActionResult<TransactionDto>> CreateIncomeAsync(
        [FromBody] CreateIncomeTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = dto.Id }, dto);
    }

    [HttpPost("expense")]
    public async Task<ActionResult<TransactionDto>> CreateExpenseAsync(
        [FromBody] CreateExpenseTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = dto.Id }, dto);
    }

    [HttpPost("transfer")]
    public async Task<ActionResult<TransferResult>> CreateTransferAsync(
        [FromBody] CreateTransferTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<TransactionDto>> UpdateAsync(
        long id,
        [FromBody] UpdateTransactionCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(new { error = "Id у шляху не співпадає з тілом запиту." });
        }

        var dto = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteTransactionCommand(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
