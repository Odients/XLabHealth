using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Enums;

namespace XLabStatusService.Core.Interfaces;

/// <summary>
/// Интерфейс для отправки уведомлений
/// </summary>
public interface INotificationService
{
    Task NotifyServiceStatusChangedAsync(
        Guid serviceId,
        HealthStatus status,
        HealthCheckResult? result = null,
        CancellationToken cancellationToken = default);

    Task NotifyServiceCheckedAsync(
        Guid serviceId,
        HealthCheckResult result,
        CancellationToken cancellationToken = default);
}

