using Microsoft.EntityFrameworkCore;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Interfaces;
using XLabStatusService.Infrastructure.Data;

namespace XLabStatusService.Infrastructure.Repositories;

/// <summary>
/// Репозиторий для отслеживания попыток входа
/// </summary>
public class LoginAttemptRepository : ILoginAttemptRepository
{
    private readonly ApplicationDbContext _context;

    public LoginAttemptRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LoginAttempt> CreateAsync(LoginAttempt attempt, CancellationToken cancellationToken = default)
    {
        attempt.Id = Guid.NewGuid();
        if (attempt.AttemptedAt == default)
        {
            attempt.AttemptedAt = DateTime.UtcNow;
        }

        _context.LoginAttempts.Add(attempt);
        await _context.SaveChangesAsync(cancellationToken);

        return attempt;
    }

    public async Task<int> GetFailedAttemptsCountByIpAsync(string ipAddress, DateTime since, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return 0;
        }

        return await _context.LoginAttempts
            .Where(a => a.IpAddress == ipAddress 
                && !a.IsSuccessful 
                && a.AttemptedAt >= since)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetFailedAttemptsCountByUsernameAsync(string username, DateTime since, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return 0;
        }

        return await _context.LoginAttempts
            .Where(a => a.Username == username 
                && !a.IsSuccessful 
                && a.AttemptedAt >= since)
            .CountAsync(cancellationToken);
    }

    public async Task<int> DeleteOldAttemptsAsync(DateTime before, CancellationToken cancellationToken = default)
    {
        var oldAttempts = await _context.LoginAttempts
            .Where(a => a.AttemptedAt < before)
            .ToListAsync(cancellationToken);

        if (oldAttempts.Count == 0)
        {
            return 0;
        }

        _context.LoginAttempts.RemoveRange(oldAttempts);
        await _context.SaveChangesAsync(cancellationToken);

        return oldAttempts.Count;
    }
}

