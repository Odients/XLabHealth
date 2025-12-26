using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XLabStatusService.Core.Entities;

namespace XLabStatusService.Infrastructure.Data.Configurations;

public class HealthCheckResultConfiguration : IEntityTypeConfiguration<HealthCheckResult>
{
    public void Configure(EntityTypeBuilder<HealthCheckResult> builder)
    {
        builder.ToTable("HealthCheckResults");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.ServiceId)
            .IsRequired();

        builder.Property(h => h.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(h => h.ResponseTime)
            .IsRequired();

        builder.Property(h => h.Message)
            .HasMaxLength(2000);

        builder.Property(h => h.Exception)
            .HasMaxLength(4000);

        builder.Property(h => h.CheckedAt)
            .IsRequired();

        builder.Property(h => h.Metadata)
            .HasColumnType("nvarchar(max)");

        // Relationships
        builder.HasOne(h => h.Service)
            .WithMany(s => s.HealthCheckResults)
            .HasForeignKey(h => h.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(h => h.ServiceId);
        builder.HasIndex(h => h.CheckedAt);
        builder.HasIndex(h => h.Status);
        builder.HasIndex(h => new { h.ServiceId, h.CheckedAt });
    }
}

