using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Enums;

namespace XLabStatusService.Core.Interfaces;

/// <summary>
/// Репозиторий для работы с результатами проверок здоровья
/// </summary>
public interface IHealthCheckResultRepository
{
    Task<HealthCheckResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<HealthCheckResult>> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<HealthCheckResult>> GetByServiceIdAndDateRangeAsync(
        Guid serviceId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
    Task<HealthCheckResult?> GetLatestByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<HealthCheckResult> CreateAsync(HealthCheckResult result, CancellationToken cancellationToken = default);
    Task<IEnumerable<HealthCheckResult>> GetByStatusAsync(HealthStatus status, CancellationToken cancellationToken = default);
    Task<int> DeleteOldResultsAsync(DateTime beforeDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Получить все результаты проверок в диапазоне дат
    /// </summary>
    Task<IEnumerable<HealthCheckResult>> GetByDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Получить результаты проверок для всех сервисов в диапазоне дат с группировкой по сервисам
    /// </summary>
    Task<Dictionary<Guid, List<HealthCheckResult>>> GetGroupedByServiceIdAndDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
}

