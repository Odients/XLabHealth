using Microsoft.EntityFrameworkCore;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Enums;
using XLabStatusService.Core.Interfaces;
using XLabStatusService.Infrastructure.Data;

namespace XLabStatusService.Infrastructure.Repositories;

public class HealthCheckResultRepository : IHealthCheckResultRepository
{
    private readonly ApplicationDbContext _context;

    public HealthCheckResultRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.HealthCheckResults
            .Include(h => h.Service)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<HealthCheckResult>> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        return await _context.HealthCheckResults
            .Where(h => h.ServiceId == serviceId)
            .OrderByDescending(h => h.CheckedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<HealthCheckResult>> GetByServiceIdAndDateRangeAsync(
        Guid serviceId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.HealthCheckResults
            .Where(h => h.ServiceId == serviceId && h.CheckedAt >= fromDate && h.CheckedAt <= toDate)
            .OrderByDescending(h => h.CheckedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<HealthCheckResult?> GetLatestByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        return await _context.HealthCheckResults
            .Where(h => h.ServiceId == serviceId)
            .OrderByDescending(h => h.CheckedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<HealthCheckResult> CreateAsync(HealthCheckResult result, CancellationToken cancellationToken = default)
    {
        _context.HealthCheckResults.Add(result);
        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<IEnumerable<HealthCheckResult>> GetByStatusAsync(HealthStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.HealthCheckResults
            .Where(h => h.Status == status)
            .OrderByDescending(h => h.CheckedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> DeleteOldResultsAsync(DateTime beforeDate, CancellationToken cancellationToken = default)
    {
        var results = await _context.HealthCheckResults
            .Where(h => h.CheckedAt < beforeDate)
            .ToListAsync(cancellationToken);

        _context.HealthCheckResults.RemoveRange(results);
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<HealthCheckResult>> GetByDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.HealthCheckResults
            .Include(h => h.Service)
            .Where(h => h.CheckedAt >= fromDate && h.CheckedAt <= toDate)
            .OrderBy(h => h.CheckedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, List<HealthCheckResult>>> GetGroupedByServiceIdAndDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var results = await _context.HealthCheckResults
            .Include(h => h.Service)
            .Where(h => h.CheckedAt >= fromDate && h.CheckedAt <= toDate)
            .OrderBy(h => h.CheckedAt)
            .ToListAsync(cancellationToken);

        return results
            .GroupBy(h => h.ServiceId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}

