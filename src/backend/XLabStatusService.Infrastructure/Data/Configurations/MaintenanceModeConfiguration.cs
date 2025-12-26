using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XLabStatusService.Core.Entities;

namespace XLabStatusService.Infrastructure.Data.Configurations;

public class MaintenanceModeConfiguration : IEntityTypeConfiguration<MaintenanceMode>
{
    public void Configure(EntityTypeBuilder<MaintenanceMode> builder)
    {
        builder.ToTable("MaintenanceModes");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.IsEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(m => m.Message)
            .HasMaxLength(2000);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(m => m.IsEnabled);
        builder.HasIndex(m => m.CreatedAt);
    }
}

