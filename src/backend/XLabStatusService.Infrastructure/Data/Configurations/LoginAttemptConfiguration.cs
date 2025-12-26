using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XLabStatusService.Core.Entities;

namespace XLabStatusService.Infrastructure.Data.Configurations;

public class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.ToTable("LoginAttempts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45); // IPv6 может быть до 45 символов

        builder.Property(a => a.Username)
            .HasMaxLength(100);

        builder.Property(a => a.IsSuccessful)
            .IsRequired();

        builder.Property(a => a.AttemptedAt)
            .IsRequired();

        builder.Property(a => a.FailureReason)
            .HasMaxLength(500);

        // Индексы для быстрого поиска
        builder.HasIndex(a => new { a.IpAddress, a.AttemptedAt });
        builder.HasIndex(a => new { a.Username, a.AttemptedAt });
        builder.HasIndex(a => a.AttemptedAt);
    }
}

