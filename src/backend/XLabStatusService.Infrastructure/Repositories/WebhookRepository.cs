using Microsoft.EntityFrameworkCore;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Interfaces;
using XLabStatusService.Infrastructure.Data;

namespace XLabStatusService.Infrastructure.Repositories;

public class WebhookRepository : IWebhookRepository
{
    private readonly ApplicationDbContext _context;

    public WebhookRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Webhook?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Webhooks
            .Include(w => w.Service)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Webhook>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Webhooks
            .Include(w => w.Service)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Webhook>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Webhooks
            .Where(w => w.IsEnabled)
            .Include(w => w.Service)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Webhook>> GetByServiceIdAsync(Guid? serviceId, CancellationToken cancellationToken = default)
    {
        if (serviceId.HasValue)
        {
            return await _context.Webhooks
                .Where(w => w.ServiceId == serviceId)
                .Include(w => w.Service)
                .ToListAsync(cancellationToken);
        }

        return await _context.Webhooks
            .Where(w => w.ServiceId == null)
            .Include(w => w.Service)
            .ToListAsync(cancellationToken);
    }

    public async Task<Webhook> CreateAsync(Webhook webhook, CancellationToken cancellationToken = default)
    {
        webhook.CreatedAt = DateTime.UtcNow;
        webhook.UpdatedAt = DateTime.UtcNow;
        _context.Webhooks.Add(webhook);
        await _context.SaveChangesAsync(cancellationToken);
        return webhook;
    }

    public async Task<Webhook> UpdateAsync(Webhook webhook, CancellationToken cancellationToken = default)
    {
        webhook.UpdatedAt = DateTime.UtcNow;
        _context.Webhooks.Update(webhook);
        await _context.SaveChangesAsync(cancellationToken);
        return webhook;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var webhook = await _context.Webhooks.FindAsync(new object[] { id }, cancellationToken);
        if (webhook != null)
        {
            _context.Webhooks.Remove(webhook);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

