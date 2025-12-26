using XLabStatusService.Core.Entities;

namespace XLabStatusService.Core.Interfaces;

/// <summary>
/// Репозиторий для работы с webhooks
/// </summary>
public interface IWebhookRepository
{
    Task<Webhook?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Webhook>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Webhook>> GetEnabledAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Webhook>> GetByServiceIdAsync(Guid? serviceId, CancellationToken cancellationToken = default);
    Task<Webhook> CreateAsync(Webhook webhook, CancellationToken cancellationToken = default);
    Task<Webhook> UpdateAsync(Webhook webhook, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

