using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Reports;

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryResponse>;

public record DashboardSummaryResponse(int AppointmentsToday, decimal RevenueThisMonth, int TotalPatients);

public class GetDashboardSummaryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryResponse>
{
    private readonly ClinicFlowDbContext _db;

    public GetDashboardSummaryHandler(ClinicFlowDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSummaryResponse> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var appointmentsToday = await _db.Appointments
            .Where(a => a.ScheduledStart >= today && a.ScheduledStart < tomorrow)
            .Where(a => a.Status == AppointmentStatus.Requested || a.Status == AppointmentStatus.Confirmed)
            .CountAsync(cancellationToken);

        var monthStart = new DateTime(today.Year, today.Month, 1);
        var revenueThisMonth = await _db.Payments
            .Where(p => p.PaidAt >= monthStart)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0;

        var totalPatients = await _db.Patients.CountAsync(cancellationToken);

        return new DashboardSummaryResponse(appointmentsToday, revenueThisMonth, totalPatients);
    }
}