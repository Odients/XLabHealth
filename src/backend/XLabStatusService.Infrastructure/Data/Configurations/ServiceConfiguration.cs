using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XLabStatusService.Core.Entities;

namespace XLabStatusService.Infrastructure.Data.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Core.Entities.Service>
{
    public void Configure(EntityTypeBuilder<Core.Entities.Service> builder)
    {
        builder.ToTable("Services");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.Property(s => s.Url)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.CheckInterval)
            .IsRequired();

        builder.Property(s => s.Timeout)
            .IsRequired();

        builder.Property(s => s.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.IsPublic)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(s => s.Configuration)
            .WithOne(c => c.Service)
            .HasForeignKey<Core.Entities.ServiceConfiguration>(c => c.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.HealthCheckResults)
            .WithOne(h => h.Service)
            .HasForeignKey(h => h.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(s => s.IsEnabled);
        builder.HasIndex(s => s.IsPublic);
        builder.HasIndex(s => s.Type);
    }
}

