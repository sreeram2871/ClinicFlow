using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Features.Appointments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace ClinicFlow.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class AppointmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AppointmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("appointments")]
    [Authorize]
    public async Task<ActionResult<BookAppointmentResponse>> BookAppointment(BookAppointmentCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPatch("appointments/{id}/confirm")]
    [Authorize(Roles = "Receptionist,Doctor")]
    public async Task<IActionResult> Confirm(Guid id)
    {
        await _mediator.Send(new ConfirmAppointmentCommand(id));
        return NoContent();
    }

    [HttpPatch("appointments/{id}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        await _mediator.Send(new CancelAppointmentCommand(id, userId, role));
        return NoContent();
    }

    [HttpPatch("appointments/{id}/complete")]
    [Authorize(Roles = "Receptionist")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteAppointmentBody body)
    {
        await _mediator.Send(new CompleteAppointmentCommand(id, body.Status));
        return NoContent();
    }
    [HttpGet("doctors/{doctorId}/schedule")]
    [Authorize]
    public async Task<ActionResult<DoctorScheduleResponse>> GetDoctorSchedule(Guid doctorId, [FromQuery] DateTime date)
    {
        var result = await _mediator.Send(new GetDoctorScheduleQuery(doctorId, date));
        return Ok(result);
    }
    [HttpGet("doctors")]
    [Authorize(Roles = "Admin,Receptionist,Patient")]
    public async Task<ActionResult<List<DoctorListItemResponse>>> GetDoctorsList()
    {
        var result = await _mediator.Send(new GetDoctorsListQuery());
        return Ok(result);
    }

    public record CompleteAppointmentBody(AppointmentStatus Status);
}