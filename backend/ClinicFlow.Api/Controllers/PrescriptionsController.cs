using System.Security.Claims;
using ClinicFlow.Api.Features.Prescriptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicFlow.Api.Controllers;

[ApiController]
[Route("api/v1/patients/{patientId}/prescriptions")]
public class PrescriptionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PrescriptionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<CreatePrescriptionResponse>> Create(Guid patientId, [FromBody] CreatePrescriptionBody body)
    {
        var doctorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _mediator.Send(new CreatePrescriptionCommand(patientId, doctorId, body.MedicineName, body.Dosage, body.Notes));
        return Ok(result);
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<PrescriptionResponse>>> GetAll(Guid patientId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role)!;

        var result = await _mediator.Send(new GetPatientPrescriptionsQuery(patientId, userId, role));
        return Ok(result);
    }
}

public record CreatePrescriptionBody(string MedicineName, string Dosage, string? Notes);