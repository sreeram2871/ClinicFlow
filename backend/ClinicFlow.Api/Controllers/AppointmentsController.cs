using MediatR;
using Microsoft.AspNetCore.Mvc;
using ClinicFlow.Api.Features.Appointments;
using Microsoft.AspNetCore.Authorization;
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
}