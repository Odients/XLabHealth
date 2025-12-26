namespace XLabStatusService.Core.Interfaces;

/// <summary>
/// Репозиторий для управления режимом обслуживания
/// </summary>
public interface IMaintenanceModeRepository
{
    /// <summary>
    /// Получить текущий режим обслуживания
    /// </summary>
    Task<Entities.MaintenanceMode?> GetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Создать или обновить режим обслуживания
    /// </summary>
    Task<Entities.MaintenanceMode> CreateOrUpdateAsync(Entities.MaintenanceMode maintenanceMode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверить, включен ли режим обслуживания
    /// </summary>
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить периоды обслуживания за указанный период времени
    /// </summary>
    Task<List<Entities.MaintenanceMode>> GetPeriodsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}

