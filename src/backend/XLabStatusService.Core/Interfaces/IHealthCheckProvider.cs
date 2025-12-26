using XLabStatusService.Core.Entities;

namespace XLabStatusService.Core.Interfaces;

/// <summary>
/// Провайдер для проверки здоровья сервиса
/// </summary>
public interface IHealthCheckProvider
{
    /// <summary>
    /// Проверяет здоровье сервиса
    /// </summary>
    Task<HealthCheckResult> CheckHealthAsync(Service service, CancellationToken cancellationToken = default);
}

