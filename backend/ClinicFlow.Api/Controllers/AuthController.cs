using ClinicFlow.Api.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
namespace ClinicFlow.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserResponse>> GetMe()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _mediator.Send(new GetCurrentUserQuery(userId));
        return Ok(result);
    }
    [HttpPost("register-patient")]
    public async Task<ActionResult<RegisterPatientResponse>> RegisterPatient(RegisterPatientCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
    [HttpPost("register-staff")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RegisterStaffResponse>> RegisterStaff(RegisterStaffCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}