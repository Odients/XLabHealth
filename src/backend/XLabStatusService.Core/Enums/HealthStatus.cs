namespace XLabStatusService.Core.Enums;

/// <summary>
/// Статусы здоровья сервиса
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// Здоров - сервис работает корректно
    /// </summary>
    Healthy = 0,

    /// <summary>
    /// Деградирован - сервис работает, но с ограниченной функциональностью
    /// </summary>
    Degraded = 1,

    /// <summary>
    /// Не здоров - сервис не работает или недоступен
    /// </summary>
    Unhealthy = 2,

    /// <summary>
    /// Неизвестно - состояние не может быть определено
    /// </summary>
    Unknown = 3
}

