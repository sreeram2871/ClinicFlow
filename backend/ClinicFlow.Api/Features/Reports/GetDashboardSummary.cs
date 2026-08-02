using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Reports;

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryResponse>;

public record WeeklyDataPoint(string WeekLabel, decimal Value);

public record DashboardSummaryResponse(
    int AppointmentsToday,
    decimal RevenueThisMonth,
    int TotalPatients,
    List<WeeklyDataPoint> RevenueByWeek,
    List<WeeklyDataPoint> NewPatientsByWeek);

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

        var revenueByWeek = await BuildWeeklyRevenueAsync(today, cancellationToken);
        var newPatientsByWeek = await BuildWeeklyNewPatientsAsync(today, cancellationToken);

        return new DashboardSummaryResponse(
            appointmentsToday, revenueThisMonth, totalPatients, revenueByWeek, newPatientsByWeek);
    }

    private async Task<List<WeeklyDataPoint>> BuildWeeklyRevenueAsync(DateTime today, CancellationToken cancellationToken)
    {
        var results = new List<WeeklyDataPoint>();

        for (int weeksAgo = 5; weeksAgo >= 0; weeksAgo--)
        {
            var weekStart = today.AddDays(-7 * weeksAgo - (int)today.DayOfWeek);
            var weekEnd = weekStart.AddDays(7);

            var total = await _db.Payments
                .Where(p => p.PaidAt >= weekStart && p.PaidAt < weekEnd)
                .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0;

            results.Add(new WeeklyDataPoint($"W{6 - weeksAgo}", total));
        }

        return results;
    }

    private async Task<List<WeeklyDataPoint>> BuildWeeklyNewPatientsAsync(DateTime today, CancellationToken cancellationToken)
    {
        var results = new List<WeeklyDataPoint>();

        for (int weeksAgo = 5; weeksAgo >= 0; weeksAgo--)
        {
            var weekStart = today.AddDays(-7 * weeksAgo - (int)today.DayOfWeek);
            var weekEnd = weekStart.AddDays(7);

            var count = await _db.Patients
                .Where(p => p.CreatedAt >= weekStart && p.CreatedAt < weekEnd)
                .CountAsync(cancellationToken);

            results.Add(new WeeklyDataPoint($"W{6 - weeksAgo}", count));
        }

        return results;
    }
}