using Microsoft.EntityFrameworkCore;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Interfaces;
using XLabStatusService.Infrastructure.Data;

namespace XLabStatusService.Infrastructure.Repositories;

public class MaintenanceModeRepository : IMaintenanceModeRepository
{
    private readonly ApplicationDbContext _context;

    public MaintenanceModeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MaintenanceMode?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        return await _context.MaintenanceModes
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<MaintenanceMode> CreateOrUpdateAsync(MaintenanceMode maintenanceMode, CancellationToken cancellationToken = default)
    {
        var existing = await GetCurrentAsync(cancellationToken);
        
        if (existing != null)
        {
            existing.IsEnabled = maintenanceMode.IsEnabled;
            existing.Message = maintenanceMode.Message;
            existing.ScheduledStartTime = maintenanceMode.ScheduledStartTime;
            existing.ScheduledEndTime = maintenanceMode.ScheduledEndTime;
            existing.StartedAt = maintenanceMode.StartedAt;
            existing.EndedAt = maintenanceMode.EndedAt;
            existing.StartedByUserId = maintenanceMode.StartedByUserId;
            existing.EndedByUserId = maintenanceMode.EndedByUserId;
            existing.UpdatedAt = DateTime.UtcNow;
            
            _context.MaintenanceModes.Update(existing);
            await _context.SaveChangesAsync(cancellationToken);
            return existing;
        }
        else
        {
            maintenanceMode.Id = Guid.NewGuid();
            maintenanceMode.CreatedAt = DateTime.UtcNow;
            maintenanceMode.UpdatedAt = DateTime.UtcNow;
            
            _context.MaintenanceModes.Add(maintenanceMode);
            await _context.SaveChangesAsync(cancellationToken);
            return maintenanceMode;
        }
    }

    public async Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
    {
        var current = await GetCurrentAsync(cancellationToken);
        if (current == null || !current.IsEnabled)
        {
            return false;
        }

        // Проверяем запланированное время начала
        if (current.ScheduledStartTime.HasValue && current.ScheduledStartTime.Value > DateTime.UtcNow)
        {
            return false;
        }

        // Проверяем запланированное время окончания
        if (current.ScheduledEndTime.HasValue && current.ScheduledEndTime.Value <= DateTime.UtcNow)
        {
            return false;
        }

        return true;
    }

    public async Task<List<MaintenanceMode>> GetPeriodsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _context.MaintenanceModes
            .Where(m => 
                // Период обслуживания пересекается с указанным диапазоном
                // Период пересекается, если: maintenanceStart <= toDate AND maintenanceEnd >= fromDate
                m.StartedAt.HasValue && 
                m.StartedAt.Value <= toDate && 
                (m.EndedAt == null || m.EndedAt.Value >= fromDate)
            )
            .OrderBy(m => m.StartedAt)
            .ToListAsync(cancellationToken);
    }
}

