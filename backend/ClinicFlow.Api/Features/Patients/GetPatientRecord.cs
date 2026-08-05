using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Features.Patients.Shared;
using ClinicFlow.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Patients;

public record GetPatientRecordQuery(Guid PatientId, Guid RequestingUserId, string RequestingUserRole)
    : IRequest<PatientRecordResponse>;

public record PatientRecordResponse(
    Guid Id,
    string FullName,
    DateTime DateOfBirth,
    string Phone,
    string Email,
    List<RecentAppointment> RecentAppointments);

public record RecentAppointment(Guid Id, DateTime ScheduledStart, string Status, int? TokenNumber, string DoctorName);
public class GetPatientRecordHandler : IRequestHandler<GetPatientRecordQuery, PatientRecordResponse>
{
    private readonly ClinicFlowDbContext _db;
    private readonly PatientAccessGuard _accessGuard;

    public GetPatientRecordHandler(ClinicFlowDbContext db, PatientAccessGuard accessGuard)
    {
        _db = db;
        _accessGuard = accessGuard;
    }

    public async Task<PatientRecordResponse> Handle(GetPatientRecordQuery request, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.UserId == request.RequestingUserId, cancellationToken)
            ?? throw new KeyNotFoundException("No patient record linked to this account.");

        var rawAppointments = await _db.Appointments
            .Where(a => a.PatientId == patient.Id)
            .OrderByDescending(a => a.ScheduledStart)
            .Take(10)
            .ToListAsync(cancellationToken);

        var doctorIds = rawAppointments.Select(a => a.DoctorId).Distinct().ToList();
        var doctorNames = await _db.Users
            .Where(u => doctorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        var recentAppointments = new List<RecentAppointment>();

        foreach (var appointment in rawAppointments)
        {
            int? tokenNumber = null;

            if (appointment.Status is AppointmentStatus.Requested or AppointmentStatus.Confirmed)
            {
                var dayStart = appointment.ScheduledStart.Date;
                var dayEnd = dayStart.AddDays(1);

                tokenNumber = await _db.Appointments
                    .Where(a => a.DoctorId == appointment.DoctorId)
                    .Where(a => a.Status == AppointmentStatus.Requested || a.Status == AppointmentStatus.Confirmed)
                    .Where(a => a.ScheduledStart >= dayStart && a.ScheduledStart < dayEnd)
                    .Where(a => a.ScheduledStart <= appointment.ScheduledStart)
                    .CountAsync(cancellationToken);
            }

            recentAppointments.Add(new RecentAppointment(
                appointment.Id,
                appointment.ScheduledStart,
                appointment.Status.ToString(),
                tokenNumber,
                doctorNames.GetValueOrDefault(appointment.DoctorId, "Unknown Doctor")));
        }

        return new PatientRecordResponse(
            patient.Id, patient.FullName, patient.DateOfBirth,
            patient.Phone, patient.Email, recentAppointments);
    }
}