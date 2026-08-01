using ClinicFlow.Api.Domain.Entities;
using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Infrastructure.Data;
using ClinicFlow.Api.Tests.Features.Appointments;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace ClinicFlow.Api.Tests.Infrastructure;

public class TenantIsolationTests
{
    [Test]
    public async Task Query_ForTenantA_NeverReturnsTenantBsData()
    {
        // Arrange: two separate tenants, sharing the same in-memory database name
        // (proving isolation happens at the query level, not by accident of
        // separate databases)
        var dbName = Guid.NewGuid().ToString();
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();

        var optionsA = new DbContextOptionsBuilder<ClinicFlowDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        // Seed data as Tenant A
        using (var dbForSeeding = new ClinicFlowDbContext(optionsA, new FakeTenantProvider(tenantAId)))
        {
            dbForSeeding.Patients.Add(new Patient
            {
                Id = Guid.NewGuid(),
                TenantId = tenantAId,
                FullName = "Tenant A Patient",
                DateOfBirth = new DateTime(1990, 1, 1)
            });
            dbForSeeding.SaveChanges();
        }

        // Seed data as Tenant B, into the SAME underlying in-memory database
        using (var dbForSeeding = new ClinicFlowDbContext(optionsA, new FakeTenantProvider(tenantBId)))
        {
            dbForSeeding.Patients.Add(new Patient
            {
                Id = Guid.NewGuid(),
                TenantId = tenantBId,
                FullName = "Tenant B Patient",
                DateOfBirth = new DateTime(1990, 1, 1)
            });
            dbForSeeding.SaveChanges();
        }

        // Act: query as Tenant A
        using var dbAsTenantA = new ClinicFlowDbContext(optionsA, new FakeTenantProvider(tenantAId));
        var visiblePatients = await dbAsTenantA.Patients.ToListAsync();

        // Assert: Tenant A sees exactly their own patient, never Tenant B's
        Assert.That(visiblePatients.Count, Is.EqualTo(1));
        Assert.That(visiblePatients[0].FullName, Is.EqualTo("Tenant A Patient"));
        Assert.That(visiblePatients.Any(p => p.TenantId == tenantBId), Is.False);
    }

    [Test]
    public async Task Query_SwitchingTenantContext_ReturnsCorrectDataEachTime()
    {
        // This test specifically targets the exact bug we just fixed:
        // querying with different tenant contexts, one after another, using
        // DIFFERENT DbContext instances each time — proving the filter is
        // evaluated fresh per-instance, not cached/baked in from the first
        // one ever created.
        var dbName = Guid.NewGuid().ToString();
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<ClinicFlowDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        using (var db = new ClinicFlowDbContext(options, new FakeTenantProvider(tenantAId)))
        {
            db.Patients.Add(new Patient { Id = Guid.NewGuid(), TenantId = tenantAId, FullName = "A", DateOfBirth = DateTime.UtcNow });
            db.SaveChanges();
        }

        using (var db = new ClinicFlowDbContext(options, new FakeTenantProvider(tenantBId)))
        {
            db.Patients.Add(new Patient { Id = Guid.NewGuid(), TenantId = tenantBId, FullName = "B", DateOfBirth = DateTime.UtcNow });
            db.SaveChanges();
        }

        // Query as A, then as B, then as A again — each with a fresh DbContext,
        // simulating three separate HTTP requests from different tenants
        using var dbQueryA1 = new ClinicFlowDbContext(options, new FakeTenantProvider(tenantAId));
        var resultA1 = await dbQueryA1.Patients.ToListAsync();

        using var dbQueryB = new ClinicFlowDbContext(options, new FakeTenantProvider(tenantBId));
        var resultB = await dbQueryB.Patients.ToListAsync();

        using var dbQueryA2 = new ClinicFlowDbContext(options, new FakeTenantProvider(tenantAId));
        var resultA2 = await dbQueryA2.Patients.ToListAsync();

        Assert.That(resultA1.Single().FullName, Is.EqualTo("A"));
        Assert.That(resultB.Single().FullName, Is.EqualTo("B"));
        Assert.That(resultA2.Single().FullName, Is.EqualTo("A")); // proves no staleness even after querying as B in between
    }
}