using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Features.Billing;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicFlow.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class BillingController : ControllerBase
{
    private readonly IMediator _mediator;

    public BillingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("appointments/{id}/payment")]
    [Authorize(Roles = "Receptionist")]
    public async Task<ActionResult<RecordPaymentResponse>> RecordPayment(Guid id, [FromBody] RecordPaymentBody body)
    {
        var result = await _mediator.Send(new RecordPaymentCommand(id, body.Amount, body.Method));
        return Ok(result);
    }
}

public record RecordPaymentBody(decimal Amount, PaymentMethod Method);