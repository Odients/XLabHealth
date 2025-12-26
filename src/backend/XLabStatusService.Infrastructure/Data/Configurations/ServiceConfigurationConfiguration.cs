using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XLabStatusService.Core.Entities;

namespace XLabStatusService.Infrastructure.Data.Configurations;

public class ServiceConfigurationEntityConfiguration : IEntityTypeConfiguration<Core.Entities.ServiceConfiguration>
{
    public void Configure(EntityTypeBuilder<Core.Entities.ServiceConfiguration> builder)
    {
        builder.ToTable("ServiceConfigurations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ServiceId)
            .IsRequired();

        builder.Property(c => c.CheckType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Parameters)
            .HasColumnType("nvarchar(max)");

        builder.Property(c => c.Headers)
            .HasColumnType("nvarchar(max)");

        builder.Property(c => c.ExpectedResponse)
            .HasMaxLength(1000);

        // Relationships
        builder.HasOne(c => c.Service)
            .WithOne(s => s.Configuration)
            .HasForeignKey<Core.Entities.ServiceConfiguration>(c => c.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

