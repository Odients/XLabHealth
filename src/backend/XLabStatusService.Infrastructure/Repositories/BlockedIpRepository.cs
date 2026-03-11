using Microsoft.EntityFrameworkCore;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Interfaces;
using XLabStatusService.Infrastructure.Data;

namespace XLabStatusService.Infrastructure.Repositories;

/// <summary>
/// Репозиторий для проверки заблокированных IP-адресов
/// </summary>
public class BlockedIpRepository : IBlockedIpRepository
{
    private readonly XLabDbContext _context;

    public BlockedIpRepository(XLabDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsBlockedAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return false;
        }

        var blocked = await _context.BlockedIps
            .AnyAsync(b => b.IpAddress == ipAddress, cancellationToken);

        return blocked;
    }

    public async Task<IEnumerable<BlockedIp>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.BlockedIps
            .OrderByDescending(b => b.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<BlockedIp?> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return null;
        }

        return await _context.BlockedIps
            .FirstOrDefaultAsync(b => b.IpAddress == ipAddress, cancellationToken);
    }

    public async Task<BlockedIp?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BlockedIps
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<BlockedIp> AddAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            throw new ArgumentException("IP address cannot be empty", nameof(ipAddress));
        }

        // Проверяем, не заблокирован ли уже этот IP
        var existing = await _context.BlockedIps
            .FirstOrDefaultAsync(b => b.IpAddress == ipAddress, cancellationToken);

        if (existing != null)
        {
            return existing;
        }

        var blockedIp = new BlockedIp
        {
            Id = Guid.NewGuid(),
            IpAddress = ipAddress,
            Date = DateTimeOffset.UtcNow
        };

        _context.BlockedIps.Add(blockedIp);
        await _context.SaveChangesAsync(cancellationToken);

        return blockedIp;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var blockedIp = await _context.BlockedIps
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (blockedIp == null)
        {
            return false;
        }

        _context.BlockedIps.Remove(blockedIp);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

