using Microsoft.EntityFrameworkCore;
using XLabStatusService.Core.Entities;

namespace XLabStatusService.Infrastructure.Data;

/// <summary>
/// Контекст базы данных xlab для работы с таблицей Org_BlockedIP
/// </summary>
public class XLabDbContext : DbContext
{
    public XLabDbContext(DbContextOptions<XLabDbContext> options)
        : base(options)
    {
    }

    public DbSet<BlockedIp> BlockedIps { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BlockedIp>(entity =>
        {
            entity.ToTable("Org_BlockedIP", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("newid()");
            entity.Property(e => e.IpAddress)
                .HasColumnName("ipAddress")
                .HasMaxLength(15);
            entity.Property(e => e.Date)
                .HasColumnName("date")
                .HasDefaultValueSql("sysutcdatetime()");
        });
    }
}

