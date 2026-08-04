using Bogus;
using ClinicFlow.Api.Domain.Entities;
using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Infrastructure.Data.Seeding;

/// <summary>
/// Populates the database with realistic fake demo data on startup, but
/// only if it's empty — safe to run every time the app starts without
/// duplicating data on every restart.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(ClinicFlowDbContext db, IPasswordHasher hasher)
    {
        if (await db.Tenants.AnyAsync())
        {
            return;
        }

        var tenant = new Tenant
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ClinicName = "Apollo Family Clinic"
        };
        db.Tenants.Add(tenant);

        var demoPasswordHash = hasher.Hash("Password123!");

        var admin = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            FullName = "Asha Admin",
            Email = "admin@apollo.test",
            PasswordHash = demoPasswordHash,
            Role = UserRole.Admin
        };

        var doctor = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            FullName = "Dr. Kiran Rao",
            Email = "doctor@apollo.test",
            PasswordHash = demoPasswordHash,
            Role = UserRole.Doctor
        };

        var receptionist = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            FullName = "Priya Reception",
            Email = "reception@apollo.test",
            PasswordHash = demoPasswordHash,
            Role = UserRole.Receptionist
        };

        db.Users.AddRange(admin, doctor, receptionist);

        var patientFaker = new Faker<Patient>()
            .RuleFor(p => p.Id, f => Guid.NewGuid())
            .RuleFor(p => p.TenantId, tenant.Id)
            .RuleFor(p => p.FullName, f => f.Name.FullName())
            .RuleFor(p => p.DateOfBirth, f => f.Date.Past(60, DateTime.UtcNow.AddYears(-10)))
            .RuleFor(p => p.Phone, f => f.Phone.PhoneNumber("##########"))
            .RuleFor(p => p.Email, f => f.Internet.Email());

        var patients = patientFaker.Generate(20);
        db.Patients.AddRange(patients);

        var schedule = new List<DoctorSchedule>();
        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday })
        {
            schedule.Add(new DoctorSchedule
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                DoctorId = doctor.Id,
                DayOfWeek = day,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0)
            });
        }
        db.DoctorSchedules.AddRange(schedule);
        // Spread demo appointments + payments across the past 6 weeks so
        // dashboard charts show realistic variation instead of one lopsided
        // spike (all original seed data was created in a single sitting).
        var random = new Random();
        var historicalAppointments = new List<Appointment>();
        var historicalPayments = new List<Payment>();

        for (int weeksAgo = 5; weeksAgo >= 0; weeksAgo--)
        {
            var appointmentsThisWeek = random.Next(2, 6);

            for (int i = 0; i < appointmentsThisWeek; i++)
            {
                var randomPatient = patients[random.Next(patients.Count)];
                var dayOffset = random.Next(0, 7);
                var hourOffset = random.Next(9, 17);
                var appointmentDate = DateTime.SpecifyKind(
                                        DateTime.UtcNow.AddDays(-7 * weeksAgo - dayOffset).Date.AddHours(hourOffset),
                                        DateTimeKind.Utc);

                var appointment = new Appointment
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    PatientId = randomPatient.Id,
                    DoctorId = doctor.Id,
                    ScheduledStart = appointmentDate,
                    ScheduledEnd = appointmentDate.AddMinutes(30),
                    Status = AppointmentStatus.Completed
                };
                historicalAppointments.Add(appointment);

                historicalPayments.Add(new Payment
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    AppointmentId = appointment.Id,
                    Amount = random.Next(300, 1200),
                    Method = PaymentMethod.Cash,
                    PaidAt = appointmentDate
                });
            }
        }

        db.Appointments.AddRange(historicalAppointments);
        db.Payments.AddRange(historicalPayments);

        await db.SaveChangesAsync();
    }
}