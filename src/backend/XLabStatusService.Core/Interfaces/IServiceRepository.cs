using XLabStatusService.Core.Entities;

namespace XLabStatusService.Core.Interfaces;

/// <summary>
/// Репозиторий для работы с сервисами
/// </summary>
public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Service>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Service>> GetPublicServicesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Service>> GetEnabledServicesAsync(CancellationToken cancellationToken = default);
    Task<Service> CreateAsync(Service service, CancellationToken cancellationToken = default);
    Task<Service> UpdateAsync(Service service, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

