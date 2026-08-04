using ClinicFlow.Api.Features.Auth;
using ClinicFlow.Api.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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

        Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

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
    [HttpGet("staff")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<StaffMemberResponse>>> GetStaffList()
    {
        var result = await _mediator.Send(new GetStaffListQuery());
        return Ok(result);
    }
    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshTokenResponse>> Refresh()
    {
        var refreshTokenValue = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshTokenValue))
        {
            return Unauthorized(new { detail = "No refresh token provided." });
        }

        var result = await _mediator.Send(new RefreshTokenCommand(refreshTokenValue));

        Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromServices] IRefreshTokenService refreshTokenService)
    {
        var refreshTokenValue = Request.Cookies["refreshToken"];

        if (!string.IsNullOrEmpty(refreshTokenValue))
        {
            await refreshTokenService.RevokeAsync(refreshTokenValue, CancellationToken.None);
        }

        Response.Cookies.Delete("refreshToken");
        return NoContent();
    }
}