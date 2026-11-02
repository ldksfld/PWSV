using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PWSV.Application.Auth.Commands.ChangePassword;
using PWSV.Application.Auth.Commands.DeleteUser;
using PWSV.Application.Auth.Commands.Login;
using PWSV.Application.Auth.Commands.Register;
using PWSV.Application.Auth.Dto;
using PWSV.Application.Auth.Queries.GetDemoAccount;

namespace PWSV.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> RegisterAsync([FromBody] RegisterCommand command, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    public async Task<ActionResult<AuthResponseDto>> LoginAsync([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [Authorize]
    [HttpPost("delete-user")]
    public async Task<IActionResult> DeleteUserAsync([FromBody] DeleteUserCommand command, CancellationToken cancellationToken)
    {
        await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("demo")]
    [EnableRateLimiting("auth-demo")]
    public async Task<ActionResult<DemoAccountDto>> GetDemoAccountAsync(CancellationToken cancellationToken)
    {
        var demo = await mediator.Send(new GetDemoAccountQuery(), cancellationToken).ConfigureAwait(false);
        return demo is null ? NotFound() : Ok(demo);
    }
}
