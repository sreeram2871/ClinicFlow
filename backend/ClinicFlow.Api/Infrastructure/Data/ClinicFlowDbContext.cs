using ClinicFlow.Api.Domain.Entities;
using ClinicFlow.Api.Infrastructure.Multitenancy;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Infrastructure.Data;

/// <summary>
/// The single EF Core DbContext for the whole application. Applies a
/// global query filter to every tenant-scoped entity so that a query
/// can never accidentally return another clinic's data — isolation is
/// enforced here, once, rather than repeated in every feature handler.
/// </summary>
public class ClinicFlowDbContext : DbContext
{
    private readonly ICurrentTenantProvider _tenantProvider;
    private Guid CurrentTenantId => _tenantProvider.TenantId;

    public ClinicFlowDbContext(
        DbContextOptions<ClinicFlowDbContext> options,
        ICurrentTenantProvider tenantProvider) : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<MedicalRecordEntry> MedicalRecordEntries => Set<MedicalRecordEntry>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicFlowDbContext).Assembly);

        // Apply the TenantId filter to every entity that inherits
        // TenantScopedEntity, without listing each one by hand.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(TenantScopedEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(BuildTenantFilterExpression(entityType.ClrType));
            }
        }
    }

    private System.Linq.Expressions.LambdaExpression BuildTenantFilterExpression(Type entityType)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "e");
        var tenantIdProperty = System.Linq.Expressions.Expression.Property(parameter, nameof(TenantScopedEntity.TenantId));

        // Reference THIS DbContext instance's CurrentTenantId property —
        // evaluated fresh every time a query actually runs, not baked in
        // once when the model is first compiled.
        var dbContextInstance = System.Linq.Expressions.Expression.Constant(this);
        var currentTenantIdProperty = System.Linq.Expressions.Expression.Property(dbContextInstance, nameof(CurrentTenantId));

        var equals = System.Linq.Expressions.Expression.Equal(tenantIdProperty, currentTenantIdProperty);
        return System.Linq.Expressions.Expression.Lambda(equals, parameter);
    }
}