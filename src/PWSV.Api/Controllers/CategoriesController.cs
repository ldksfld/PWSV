using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PWSV.Application.Categories.Commands.CreateCategory;
using PWSV.Application.Categories.Commands.DeactivateCategory;
using PWSV.Application.Categories.Commands.UpdateCategory;
using PWSV.Application.Categories.Dto;
using PWSV.Application.Categories.Queries.GetCategoriesList;
using PWSV.Application.Categories.Queries.GetCategoryTree;
using PWSV.Domain.Enums;

namespace PWSV.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/categories")]
public sealed class CategoriesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetListAsync(
        [FromQuery] CategoryKind? kind = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetCategoriesListQuery(kind, includeInactive), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpGet("tree")]
    public async Task<ActionResult<IReadOnlyList<CategoryTreeNodeDto>>> GetTreeAsync(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetCategoryTreeQuery(includeInactive), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> CreateAsync([FromBody] CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetListAsync), new { kind = category.Kind }, category);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryDto>> UpdateAsync(
        int id,
        [FromBody] UpdateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(new { error = "Id у шляху не співпадає з тілом запиту." });
        }

        var category = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Ok(category);
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeactivateCategoryCommand(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
