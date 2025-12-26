using XLabStatusService.Core.Enums;

namespace XLabStatusService.Application.DTOs;

/// <summary>
/// DTO для аналитики системы
/// </summary>
public class AnalyticsDto
{
    /// <summary>
    /// Период аналитики
    /// </summary>
    public string Period { get; set; } = string.Empty; // "24h", "7d", "1y"

    /// <summary>
    /// Начало периода
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// Конец периода
    /// </summary>
    public DateTime ToDate { get; set; }

    /// <summary>
    /// Общая статистика системы
    /// </summary>
    public SystemStatisticsDto SystemStatistics { get; set; } = new();

    /// <summary>
    /// Статистика по сервисам
    /// </summary>
    public List<ServiceAnalyticsDto> Services { get; set; } = new();

    /// <summary>
    /// Временные ряды для графиков
    /// </summary>
    public TimeSeriesDataDto TimeSeries { get; set; } = new();

    /// <summary>
    /// Список инцидентов
    /// </summary>
    public List<IncidentDto> Incidents { get; set; } = new();

    /// <summary>
    /// Статистика по типам сервисов
    /// </summary>
    public List<ServiceTypeStatisticsDto> ServiceTypeStatistics { get; set; } = new();

    /// <summary>
    /// Топ сервисов
    /// </summary>
    public TopServicesDto TopServices { get; set; } = new();
}

/// <summary>
/// Общая статистика системы
/// </summary>
public class SystemStatisticsDto
{
    /// <summary>
    /// Общий процент доступности (%)
    /// </summary>
    public double UptimePercentage { get; set; }

    /// <summary>
    /// Общее время недоступности (минуты)
    /// </summary>
    public double TotalDowntimeMinutes { get; set; }

    /// <summary>
    /// Статистика по статусам
    /// </summary>
    public StatusStatisticsDto StatusStatistics { get; set; } = new();

    /// <summary>
    /// Статистика времени отклика
    /// </summary>
    public ResponseTimeStatisticsDto ResponseTimeStatistics { get; set; } = new();

    /// <summary>
    /// Статистика проверок
    /// </summary>
    public CheckStatisticsDto CheckStatistics { get; set; } = new();

    /// <summary>
    /// Статистика инцидентов
    /// </summary>
    public IncidentStatisticsDto IncidentStatistics { get; set; } = new();
}

/// <summary>
/// Статистика по статусам
/// </summary>
public class StatusStatisticsDto
{
    public int TotalChecks { get; set; }
    public int HealthyCount { get; set; }
    public int DegradedCount { get; set; }
    public int UnhealthyCount { get; set; }
    public int UnknownCount { get; set; }

    public double HealthyPercentage => TotalChecks > 0 ? (double)HealthyCount / TotalChecks * 100 : 0;
    public double DegradedPercentage => TotalChecks > 0 ? (double)DegradedCount / TotalChecks * 100 : 0;
    public double UnhealthyPercentage => TotalChecks > 0 ? (double)UnhealthyCount / TotalChecks * 100 : 0;
    public double UnknownPercentage => TotalChecks > 0 ? (double)UnknownCount / TotalChecks * 100 : 0;
}

/// <summary>
/// Статистика времени отклика
/// </summary>
public class ResponseTimeStatisticsDto
{
    public double Average { get; set; }
    public double Median { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }
    public double P95 { get; set; }
    public double P99 { get; set; }
}

/// <summary>
/// Статистика проверок
/// </summary>
public class CheckStatisticsDto
{
    public int TotalChecks { get; set; }
    public int SuccessfulChecks { get; set; }
    public int FailedChecks { get; set; }
    public double SuccessPercentage => TotalChecks > 0 ? (double)SuccessfulChecks / TotalChecks * 100 : 0;
}

/// <summary>
/// Статистика инцидентов
/// </summary>
public class IncidentStatisticsDto
{
    public int TotalIncidents { get; set; }
    public double TotalDowntimeMinutes { get; set; }
    public double AverageIncidentDurationMinutes { get; set; }
    public double MaxIncidentDurationMinutes { get; set; }
    public int CriticalIncidents { get; set; } // > 1 часа
}

/// <summary>
/// Аналитика по сервису
/// </summary>
public class ServiceAnalyticsDto
{
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public ServiceType ServiceType { get; set; }
    public HealthStatus? CurrentStatus { get; set; }
    public DateTime? LastCheckedAt { get; set; }

    /// <summary>
    /// Доступность за период (%)
    /// </summary>
    public double UptimePercentage { get; set; }

    /// <summary>
    /// Статистика времени отклика
    /// </summary>
    public ResponseTimeStatisticsDto ResponseTimeStatistics { get; set; } = new();

    /// <summary>
    /// Количество проверок
    /// </summary>
    public int TotalChecks { get; set; }

    /// <summary>
    /// Количество инцидентов
    /// </summary>
    public int IncidentCount { get; set; }

    /// <summary>
    /// Общее время недоступности (минуты)
    /// </summary>
    public double TotalDowntimeMinutes { get; set; }

    /// <summary>
    /// Метрики размера БД (только для Database сервисов)
    /// </summary>
    public DatabaseSizeMetricsDto? DatabaseSizeMetrics { get; set; }
}

/// <summary>
/// Метрики размера базы данных
/// </summary>
public class DatabaseSizeMetricsDto
{
    /// <summary>
    /// Общий размер БД (МБ)
    /// </summary>
    public double TotalSizeMB { get; set; }

    /// <summary>
    /// Размер данных (МБ)
    /// </summary>
    public double DataSizeMB { get; set; }

    /// <summary>
    /// Размер логов (МБ)
    /// </summary>
    public double LogSizeMB { get; set; }

    /// <summary>
    /// Используемое пространство (МБ)
    /// </summary>
    public double UsedSpaceMB { get; set; }

    /// <summary>
    /// Свободное пространство (МБ)
    /// </summary>
    public double FreeSpaceMB { get; set; }

    /// <summary>
    /// Процент использования (%)
    /// </summary>
    public double UsagePercentage => TotalSizeMB > 0 ? (UsedSpaceMB / TotalSizeMB) * 100 : 0;

    /// <summary>
    /// Процент свободного пространства (%)
    /// </summary>
    public double FreeSpacePercentage => TotalSizeMB > 0 ? (FreeSpaceMB / TotalSizeMB) * 100 : 0;

    /// <summary>
    /// Изменение размера за период (МБ)
    /// </summary>
    public double? SizeChangeMB { get; set; }

    /// <summary>
    /// Изменение размера за период (%)
    /// </summary>
    public double? SizeChangePercentage { get; set; }

    /// <summary>
    /// Дата последнего обновления метрик
    /// </summary>
    public DateTime? LastUpdated { get; set; }
}

/// <summary>
/// Временные ряды для графиков
/// </summary>
public class TimeSeriesDataDto
{
    /// <summary>
    /// Данные доступности по времени
    /// </summary>
    public List<TimeSeriesPointDto> UptimeSeries { get; set; } = new();

    /// <summary>
    /// Данные времени отклика по времени
    /// </summary>
    public List<TimeSeriesPointDto> ResponseTimeSeries { get; set; } = new();

    /// <summary>
    /// Распределение статусов по времени
    /// </summary>
    public List<StatusDistributionPointDto> StatusDistributionSeries { get; set; } = new();

    /// <summary>
    /// Количество проверок по времени
    /// </summary>
    public List<TimeSeriesPointDto> CheckCountSeries { get; set; } = new();

    /// <summary>
    /// Данные размера БД по времени (для Database сервисов)
    /// </summary>
    public List<DatabaseSizeTimeSeriesPointDto> DatabaseSizeSeries { get; set; } = new();

    /// <summary>
    /// Прогноз роста БД (для Database сервисов)
    /// </summary>
    public List<DatabaseSizeForecastPointDto> DatabaseSizeForecast { get; set; } = new();
}

/// <summary>
/// Точка временного ряда
/// </summary>
public class TimeSeriesPointDto
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
}

/// <summary>
/// Точка распределения статусов
/// </summary>
public class StatusDistributionPointDto
{
    public DateTime Timestamp { get; set; }
    public int Healthy { get; set; }
    public int Degraded { get; set; }
    public int Unhealthy { get; set; }
    public int Unknown { get; set; }
}

/// <summary>
/// Точка временного ряда размера БД
/// </summary>
public class DatabaseSizeTimeSeriesPointDto
{
    public DateTime Timestamp { get; set; }
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public double TotalSizeMB { get; set; }
    public double DataSizeMB { get; set; }
    public double LogSizeMB { get; set; }
    public double UsedSpaceMB { get; set; }
    public double FreeSpaceMB { get; set; }
    public double UsagePercentage { get; set; }
}

/// <summary>
/// Точка прогноза роста БД
/// </summary>
public class DatabaseSizeForecastPointDto
{
    public DateTime Timestamp { get; set; }
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public double ForecastedTotalSizeMB { get; set; }
    public double ForecastedUsedSpaceMB { get; set; }
    public double ForecastedUsagePercentage { get; set; }
    public double? GrowthRateMBPerDay { get; set; }
    public DateTime? EstimatedFullDate { get; set; } // Дата, когда БД будет заполнена (если доступно)
}

/// <summary>
/// Инцидент
/// </summary>
public class IncidentDto
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double DurationMinutes { get; set; }
    public HealthStatus StatusBefore { get; set; }
    public HealthStatus StatusAfter { get; set; }
    public string? Reason { get; set; }
    public bool IsCritical => DurationMinutes > 60; // > 1 часа
}

/// <summary>
/// Статистика по типу сервиса
/// </summary>
public class ServiceTypeStatisticsDto
{
    public ServiceType ServiceType { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public int ServiceCount { get; set; }
    public double AverageUptimePercentage { get; set; }
    public double AverageResponseTime { get; set; }
    public int TotalIncidents { get; set; }
}

/// <summary>
/// Топ сервисов
/// </summary>
public class TopServicesDto
{
    /// <summary>
    /// Топ-10 сервисов по доступности
    /// </summary>
    public List<ServiceAnalyticsDto> TopByUptime { get; set; } = new();

    /// <summary>
    /// Топ-10 сервисов с наименьшей доступностью
    /// </summary>
    public List<ServiceAnalyticsDto> BottomByUptime { get; set; } = new();

    /// <summary>
    /// Топ-10 самых быстрых сервисов
    /// </summary>
    public List<ServiceAnalyticsDto> TopByResponseTime { get; set; } = new();

    /// <summary>
    /// Топ-10 самых медленных сервисов
    /// </summary>
    public List<ServiceAnalyticsDto> BottomByResponseTime { get; set; } = new();

    /// <summary>
    /// Топ-10 сервисов по количеству инцидентов
    /// </summary>
    public List<ServiceAnalyticsDto> TopByIncidents { get; set; } = new();

    /// <summary>
    /// Топ-10 Database сервисов по размеру
    /// </summary>
    public List<ServiceAnalyticsDto> TopDatabaseBySize { get; set; } = new();
}

