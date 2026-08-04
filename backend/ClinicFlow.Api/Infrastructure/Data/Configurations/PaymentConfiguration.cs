using ClinicFlow.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicFlow.Api.Infrastructure.Data.Configurations;

/// <summary>
/// decimal(10,2) pins down exactly how SQL Server stores money: up to
/// 10 total digits, 2 after the decimal point (e.g. 12345678.99).
/// Without this, EF Core picks a default precision that isn't always
/// right for currency — being explicit avoids a subtle rounding bug.
/// </summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(p => p.Amount)
      .IsRequired()
      .HasColumnType("numeric(10,2)");
    }
}