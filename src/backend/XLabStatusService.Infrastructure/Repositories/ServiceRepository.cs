using Microsoft.EntityFrameworkCore;
using XLabStatusService.Application.DTOs;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Interfaces;
using XLabStatusService.Infrastructure.Data;

namespace XLabStatusService.Infrastructure.Repositories;

public class ServiceRepository : IServiceRepository
{
    private readonly ApplicationDbContext _context;

    public ServiceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Services
            .Include(s => s.Configuration)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Service>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Services
            .Include(s => s.Configuration)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Service>> GetPublicServicesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Services
            .Where(s => s.IsPublic && s.IsEnabled)
            .Include(s => s.Configuration)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Service>> GetEnabledServicesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Services
            .Where(s => s.IsEnabled)
            .Include(s => s.Configuration)
            .ToListAsync(cancellationToken);
    }

    public async Task<Service> CreateAsync(Service service, CancellationToken cancellationToken = default)
    {
        service.CreatedAt = DateTime.UtcNow;
        service.UpdatedAt = DateTime.UtcNow;
        _context.Services.Add(service);
        await _context.SaveChangesAsync(cancellationToken);
        return service;
    }

    public async Task<Service> UpdateAsync(Service service, CancellationToken cancellationToken = default)
    {
        // Обновляем время изменения
        service.UpdatedAt = DateTime.UtcNow;

        // Attach and mark as modified
        _context.Entry(service).State = EntityState.Modified;

        // Handle Configuration separately if it exists
        if (service.Configuration != null)
        {
            _context.Entry(service.Configuration).State = EntityState.Modified;
        }
        _context.Services.Update(service);
        await _context.SaveChangesAsync(cancellationToken);
        return service;        
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await _context.Services.FindAsync(new object[] { id }, cancellationToken);
        if (service != null)
        {
            _context.Services.Remove(service);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Services.AnyAsync(s => s.Id == id, cancellationToken);
    }
}

