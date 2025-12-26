namespace XLabStatusService.Core.Enums;

/// <summary>
/// Типы сервисов для мониторинга
/// </summary>
public enum ServiceType
{
    /// <summary>
    /// HTTP/HTTPS endpoints (X-Lab API, REST API)
    /// </summary>
    Http = 0,

    /// <summary>
    /// TCP соединения
    /// </summary>
    Tcp = 1,

    /// <summary>
    /// Базы данных (MS SQL Server, PostgreSQL, MySQL)
    /// </summary>
    Database = 2,

    /// <summary>
    /// Redis Server
    /// </summary>
    Redis = 3,

    /// <summary>
    /// Windows Services (XLabNotificationService, XLabSendService и др.)
    /// </summary>
    WindowsService = 4,

    /// <summary>
    /// Apache Kafka
    /// </summary>
    Kafka = 5,

    /// <summary>
    /// Кастомные проверки
    /// </summary>
    Custom = 6
}

