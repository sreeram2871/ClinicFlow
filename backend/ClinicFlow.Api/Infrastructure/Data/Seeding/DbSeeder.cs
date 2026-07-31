using Bogus;
using ClinicFlow.Api.Domain.Entities;
using ClinicFlow.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Infrastructure.Data.Seeding;

/// <summary>
/// Populates the database with realistic fake demo data on startup, but
/// only if it's empty — safe to run every time the app starts without
/// duplicating data on every restart.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(ClinicFlowDbContext db)
    {
        if (await db.Tenants.AnyAsync())
        {
            return; // already seeded, don't do it again
        }

        var tenant = new Tenant
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), // matches TemporaryTenantProvider
            ClinicName = "Apollo Family Clinic"
        };
        db.Tenants.Add(tenant);

        var admin = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            FullName = "Asha Admin",
            Email = "admin@apollo.test",
            PasswordHash = "TEMP_PLAINTEXT_Password123!", // real hashing comes with the Auth feature
            Role = UserRole.Admin
        };

        var doctor = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            FullName = "Dr. Kiran Rao",
            Email = "doctor@apollo.test",
            PasswordHash = "TEMP_PLAINTEXT_Password123!",
            Role = UserRole.Doctor
        };

        var receptionist = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            FullName = "Priya Reception",
            Email = "reception@apollo.test",
            PasswordHash = "TEMP_PLAINTEXT_Password123!",
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

        await db.SaveChangesAsync();
    }
}