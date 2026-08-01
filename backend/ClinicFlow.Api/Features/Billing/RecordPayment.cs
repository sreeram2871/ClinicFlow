using ClinicFlow.Api.Domain.Entities;
using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Infrastructure.Data;
using ClinicFlow.Api.Infrastructure.Multitenancy;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Billing;

public record RecordPaymentCommand(Guid AppointmentId, decimal Amount, PaymentMethod Method) : IRequest<RecordPaymentResponse>;

public record RecordPaymentResponse(Guid PaymentId);

public class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Payment amount must be greater than zero.");
    }
}

public class RecordPaymentHandler : IRequestHandler<RecordPaymentCommand, RecordPaymentResponse>
{
    private readonly ClinicFlowDbContext _db;
    private readonly ICurrentTenantProvider _tenantProvider;

    public RecordPaymentHandler(ClinicFlowDbContext db, ICurrentTenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public async Task<RecordPaymentResponse> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Appointment not found.");

        // Business rule: only makes sense to bill for a visit that actually
        // happened — not one still pending, or one that was cancelled.
        if (appointment.Status != AppointmentStatus.Completed)
        {
            throw new InvalidOperationException(
                $"Cannot record a payment for an appointment with status '{appointment.Status}'. Only Completed appointments can be billed.");
        }

        var alreadyPaid = await _db.Payments.AnyAsync(p => p.AppointmentId == request.AppointmentId, cancellationToken);
        if (alreadyPaid)
        {
            throw new InvalidOperationException("A payment has already been recorded for this appointment.");
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId,
            AppointmentId = request.AppointmentId,
            Amount = request.Amount,
            Method = request.Method,
            PaidAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(cancellationToken);

        return new RecordPaymentResponse(payment.Id);
    }
}