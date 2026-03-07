using System.Text.Json;
using Microsoft.Extensions.Logging;
using XLabStatusService.Application.DTOs;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Enums;
using XLabStatusService.Core.Interfaces;

namespace XLabStatusService.Application.Services;

/// <summary>
/// Сервис для расчета аналитики
/// </summary>
public class AnalyticsService
{
    private readonly IServiceRepository _serviceRepository;
    private readonly IHealthCheckResultRepository _resultRepository;
    private readonly IMaintenanceModeRepository _maintenanceModeRepository;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        IServiceRepository serviceRepository,
        IHealthCheckResultRepository resultRepository,
        IMaintenanceModeRepository maintenanceModeRepository,
        ILogger<AnalyticsService> logger)
    {
        _serviceRepository = serviceRepository;
        _resultRepository = resultRepository;
        _maintenanceModeRepository = maintenanceModeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Получить аналитику за период
    /// </summary>
    public async Task<AnalyticsDto> GetAnalyticsAsync(
        string period,
        CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = GetPeriodDates(period);
        
        var analytics = new AnalyticsDto
        {
            Period = period,
            FromDate = fromDate,
            ToDate = toDate
        };

        // Получаем все сервисы
        var services = await _serviceRepository.GetAllAsync(cancellationToken);
        var serviceList = services.ToList();

        // Получаем все результаты проверок за период
        var allResults = await _resultRepository.GetByDateRangeAsync(fromDate, toDate, cancellationToken);
        var resultsList = allResults.ToList();

        // Получаем результаты сгруппированные по сервисам
        var groupedResults = await _resultRepository.GetGroupedByServiceIdAndDateRangeAsync(
            fromDate, toDate, cancellationToken);

        // Получаем периоды обслуживания за период
        var maintenancePeriods = await _maintenanceModeRepository.GetPeriodsAsync(fromDate, toDate, cancellationToken);

        // Рассчитываем общую статистику системы
        analytics.SystemStatistics = CalculateSystemStatistics(resultsList, fromDate, toDate, maintenancePeriods);

        // Рассчитываем аналитику по каждому сервису
        analytics.Services = await CalculateServicesAnalyticsAsync(
            serviceList, groupedResults, fromDate, toDate, maintenancePeriods, cancellationToken);

        // Рассчитываем временные ряды
        analytics.TimeSeries = CalculateTimeSeries(resultsList, fromDate, toDate, period);

        // Рассчитываем временные ряды для размера БД
        analytics.TimeSeries.DatabaseSizeSeries = CalculateDatabaseSizeTimeSeries(
            serviceList.Where(s => s.Type == ServiceType.Database).ToList(),
            groupedResults,
            fromDate,
            toDate,
            period);

        // Рассчитываем прогноз роста БД
        analytics.TimeSeries.DatabaseSizeForecast = CalculateDatabaseSizeForecast(
            analytics.TimeSeries.DatabaseSizeSeries);

        // Находим инциденты
        analytics.Incidents = CalculateIncidents(groupedResults, serviceList);

        // Статистика по типам сервисов
        analytics.ServiceTypeStatistics = CalculateServiceTypeStatistics(analytics.Services);

        // Топ сервисов
        analytics.TopServices = CalculateTopServices(analytics.Services);

        return analytics;
    }

    private (DateTime fromDate, DateTime toDate) GetPeriodDates(string period)
    {
        var toDate = DateTime.UtcNow;
        DateTime fromDate;

        switch (period.ToLower())
        {
            case "24h":
            case "24":
                fromDate = toDate.AddHours(-24);
                break;
            case "7d":
            case "7":
                fromDate = toDate.AddDays(-7);
                break;
            case "1y":
            case "365":
            case "year":
                fromDate = toDate.AddYears(-1);
                break;
            default:
                fromDate = toDate.AddDays(-7);
                break;
        }

        return (fromDate, toDate);
    }

    private SystemStatisticsDto CalculateSystemStatistics(
        List<HealthCheckResult> results,
        DateTime fromDate,
        DateTime toDate,
        List<MaintenanceMode> maintenancePeriods)
    {
        var stats = new SystemStatisticsDto();

        if (results.Count == 0)
        {
            return stats;
        }

        // Статистика по статусам
        stats.StatusStatistics = new StatusStatisticsDto
        {
            TotalChecks = results.Count,
            HealthyCount = results.Count(r => r.Status == HealthStatus.Healthy),
            DegradedCount = results.Count(r => r.Status == HealthStatus.Degraded),
            UnhealthyCount = results.Count(r => r.Status == HealthStatus.Unhealthy),
            UnknownCount = results.Count(r => r.Status == HealthStatus.Unknown)
        };

        // Статистика времени отклика
        var responseTimes = results.Where(r => r.ResponseTime > 0).Select(r => r.ResponseTime).ToList();
        if (responseTimes.Any())
        {
            var sorted = responseTimes.OrderBy(x => x).ToList();
            stats.ResponseTimeStatistics = new ResponseTimeStatisticsDto
            {
                Average = responseTimes.Average(),
                Median = sorted[sorted.Count / 2],
                Min = responseTimes.Min(),
                Max = responseTimes.Max(),
                P95 = sorted[(int)(sorted.Count * 0.95)],
                P99 = sorted[(int)(sorted.Count * 0.99)]
            };
        }

        // Статистика проверок
        stats.CheckStatistics = new CheckStatisticsDto
        {
            TotalChecks = results.Count,
            SuccessfulChecks = results.Count(r => r.Exception == null),
            FailedChecks = results.Count(r => r.Exception != null)
        };

        // Рассчитываем доступность (uptime)
        // Вычисляем время обслуживания, которое нужно исключить из расчета
        var maintenanceMinutes = CalculateMaintenanceMinutes(maintenancePeriods, fromDate, toDate);
        var totalMinutes = (toDate - fromDate).TotalMinutes - maintenanceMinutes;
        var healthyMinutes = 0.0;
        var degradedMinutes = 0.0;
        var unhealthyMinutes = 0.0;
        var unknownMinutes = 0.0;

        // Группируем результаты по сервисам и рассчитываем время в каждом статусе
        var serviceGroups = results
            .Where(r => r.Service != null)
            .GroupBy(r => r.ServiceId);
        
        foreach (var group in serviceGroups)
        {
            var serviceResults = group.OrderBy(r => r.CheckedAt).ToList();
            if (serviceResults.Count == 0) continue;
            
            var service = serviceResults.First().Service;
            if (service == null) continue;
            
            var checkIntervalMinutes = Math.Max(service.CheckInterval / 60.0, 1.0);

            for (int i = 0; i < serviceResults.Count; i++)
            {
                var result = serviceResults[i];
                var nextCheckTime = i < serviceResults.Count - 1
                    ? serviceResults[i + 1].CheckedAt
                    : toDate;
                var duration = Math.Max((nextCheckTime - result.CheckedAt).TotalMinutes, checkIntervalMinutes);

                // Исключаем время обслуживания из этого периода
                var effectiveDuration = ExcludeMaintenanceTime(
                    result.CheckedAt,
                    nextCheckTime,
                    maintenancePeriods);

                if (effectiveDuration <= 0)
                {
                    continue; // Весь период был в режиме обслуживания
                }

                switch (result.Status)
                {
                    case HealthStatus.Healthy:
                        healthyMinutes += effectiveDuration;
                        break;
                    case HealthStatus.Degraded:
                        degradedMinutes += effectiveDuration;
                        break;
                    case HealthStatus.Unhealthy:
                        unhealthyMinutes += effectiveDuration;
                        break;
                    case HealthStatus.Unknown:
                        unknownMinutes += effectiveDuration;
                        break;
                }
            }
        }

        // Вычисляем общее время недоступности системы
        // Система недоступна, когда хотя бы один сервис недоступен
        // Собираем все периоды недоступности всех сервисов и объединяем их
        var systemDowntimeMinutes = CalculateSystemDowntimeMinutes(
            results,
            fromDate,
            toDate,
            maintenancePeriods);

        // Рассчитываем доступность системы
        // Система доступна, когда все сервисы доступны (нет недоступности)
        stats.UptimePercentage = totalMinutes > 0
            ? ((totalMinutes - systemDowntimeMinutes) / totalMinutes) * 100
            : 100;

        // Статистика инцидентов
        var serviceIds = results.Select(r => r.ServiceId).Distinct().ToList();
        var servicesDict = results
            .Where(r => r.Service != null)
            .GroupBy(r => r.ServiceId)
            .ToDictionary(g => g.Key, g => g.First().Service!);
        
        var incidents = CalculateIncidents(
            results.GroupBy(r => r.ServiceId).ToDictionary(g => g.Key, g => g.ToList()),
            serviceIds.Select(id => servicesDict.ContainsKey(id) ? servicesDict[id] : null)
                .Where(s => s != null)
                .Cast<Service>()
                .ToList());
        
        // Инициализируем статистику инцидентов (даже если их нет)
        stats.IncidentStatistics = new IncidentStatisticsDto
        {
            TotalIncidents = incidents.Count,
            TotalDowntimeMinutes = incidents.Any() ? incidents.Sum(i => i.DurationMinutes) : 0,
            AverageIncidentDurationMinutes = incidents.Any() ? incidents.Average(i => i.DurationMinutes) : 0,
            MaxIncidentDurationMinutes = incidents.Any() ? incidents.Max(i => i.DurationMinutes) : 0,
            CriticalIncidents = incidents.Count(i => i.IsCritical)
        };
        
        // Используем вычисленное время недоступности системы
        // (система недоступна, когда хотя бы один сервис недоступен)
        stats.TotalDowntimeMinutes = systemDowntimeMinutes;

        return stats;
    }

    /// <summary>
    /// Вычисляет общее время обслуживания в указанном диапазоне (в минутах)
    /// </summary>
    private double CalculateMaintenanceMinutes(List<MaintenanceMode> maintenancePeriods, DateTime fromDate, DateTime toDate)
    {
        if (maintenancePeriods == null || !maintenancePeriods.Any())
        {
            return 0;
        }

        var totalMaintenanceMinutes = 0.0;
        var sortedPeriods = maintenancePeriods
            .Where(m => m.StartedAt.HasValue)
            .OrderBy(m => m.StartedAt)
            .ToList();

        foreach (var period in sortedPeriods)
        {
            var periodStart = period.StartedAt!.Value;
            var periodEnd = period.EndedAt ?? toDate;

            // Ограничиваем период указанным диапазоном
            if (periodEnd < fromDate || periodStart > toDate)
            {
                continue;
            }

            var effectiveStart = periodStart > fromDate ? periodStart : fromDate;
            var effectiveEnd = periodEnd < toDate ? periodEnd : toDate;

            if (effectiveStart < effectiveEnd)
            {
                totalMaintenanceMinutes += (effectiveEnd - effectiveStart).TotalMinutes;
            }
        }

        return totalMaintenanceMinutes;
    }

    /// <summary>
    /// Исключает время обслуживания из указанного периода и возвращает эффективное время
    /// </summary>
    private double ExcludeMaintenanceTime(
        DateTime periodStart,
        DateTime periodEnd,
        List<MaintenanceMode> maintenancePeriods)
    {
        if (maintenancePeriods == null || !maintenancePeriods.Any())
        {
            return (periodEnd - periodStart).TotalMinutes;
        }

        var totalMinutes = (periodEnd - periodStart).TotalMinutes;
        var maintenanceMinutes = 0.0;

        foreach (var period in maintenancePeriods)
        {
            if (!period.StartedAt.HasValue)
            {
                continue;
            }

            var maintenanceStart = period.StartedAt.Value;
            var maintenanceEnd = period.EndedAt ?? periodEnd;

            // Проверяем пересечение периодов
            if (maintenanceEnd < periodStart || maintenanceStart > periodEnd)
            {
                continue;
            }

            var overlapStart = maintenanceStart > periodStart ? maintenanceStart : periodStart;
            var overlapEnd = maintenanceEnd < periodEnd ? maintenanceEnd : periodEnd;

            if (overlapStart < overlapEnd)
            {
                maintenanceMinutes += (overlapEnd - overlapStart).TotalMinutes;
            }
        }

        return Math.Max(0, totalMinutes - maintenanceMinutes);
    }

    /// <summary>
    /// Вычисляет время недоступности системы (когда хотя бы один сервис недоступен)
    /// Объединяет периоды недоступности всех сервисов и исключает периоды обслуживания
    /// </summary>
    private double CalculateSystemDowntimeMinutes(
        List<HealthCheckResult> results,
        DateTime fromDate,
        DateTime toDate,
        List<MaintenanceMode> maintenancePeriods)
    {
        if (results == null || !results.Any())
        {
            return 0;
        }

        // Собираем все периоды недоступности для каждого сервиса
        var downtimePeriods = new List<(DateTime start, DateTime end)>();
        var serviceGroups = results
            .Where(r => r.Service != null)
            .GroupBy(r => r.ServiceId);

        foreach (var group in serviceGroups)
        {
            var serviceResults = group.OrderBy(r => r.CheckedAt).ToList();
            if (serviceResults.Count == 0) continue;

            var service = serviceResults.First().Service;
            if (service == null) continue;

            var checkIntervalMinutes = Math.Max(service.CheckInterval / 60.0, 1.0);

            for (int i = 0; i < serviceResults.Count; i++)
            {
                var result = serviceResults[i];
                
                // Учитываем только периоды, когда сервис был недоступен (Unhealthy или Unknown)
                if (result.Status != HealthStatus.Unhealthy && result.Status != HealthStatus.Unknown)
                {
                    continue;
                }

                var periodStart = result.CheckedAt;
                var periodEnd = i < serviceResults.Count - 1
                    ? serviceResults[i + 1].CheckedAt
                    : toDate;

                // Ограничиваем период указанным диапазоном
                if (periodEnd < fromDate || periodStart > toDate)
                {
                    continue;
                }

                var effectiveStart = periodStart > fromDate ? periodStart : fromDate;
                var effectiveEnd = periodEnd < toDate ? periodEnd : toDate;

                if (effectiveStart < effectiveEnd)
                {
                    downtimePeriods.Add((effectiveStart, effectiveEnd));
                }
            }
        }

        if (!downtimePeriods.Any())
        {
            return 0;
        }

        // Объединяем пересекающиеся периоды
        var mergedPeriods = MergeTimePeriods(downtimePeriods);

        // Вычисляем общее время недоступности, исключая периоды обслуживания
        var effectiveDowntimeMinutes = 0.0;
        foreach (var period in mergedPeriods)
        {
            var effectiveDuration = ExcludeMaintenanceTime(
                period.start,
                period.end,
                maintenancePeriods);
            effectiveDowntimeMinutes += effectiveDuration;
        }

        return effectiveDowntimeMinutes;
    }

    /// <summary>
    /// Объединяет пересекающиеся временные периоды
    /// </summary>
    private List<(DateTime start, DateTime end)> MergeTimePeriods(List<(DateTime start, DateTime end)> periods)
    {
        if (periods == null || !periods.Any())
        {
            return new List<(DateTime start, DateTime end)>();
        }

        // Сортируем периоды по времени начала
        var sorted = periods.OrderBy(p => p.start).ToList();
        var merged = new List<(DateTime start, DateTime end)>();

        var current = sorted[0];

        for (int i = 1; i < sorted.Count; i++)
        {
            var next = sorted[i];

            // Если периоды пересекаются или соприкасаются, объединяем их
            if (next.start <= current.end)
            {
                // Объединяем периоды
                current = (current.start, next.end > current.end ? next.end : current.end);
            }
            else
            {
                // Периоды не пересекаются, добавляем текущий и переходим к следующему
                merged.Add(current);
                current = next;
            }
        }

        // Добавляем последний период
        merged.Add(current);

        return merged;
    }

    private async Task<List<ServiceAnalyticsDto>> CalculateServicesAnalyticsAsync(
        List<Service> services,
        Dictionary<Guid, List<HealthCheckResult>> groupedResults,
        DateTime fromDate,
        DateTime toDate,
        List<MaintenanceMode> maintenancePeriods,
        CancellationToken cancellationToken)
    {
        var servicesAnalytics = new List<ServiceAnalyticsDto>();

        foreach (var service in services)
        {
            var serviceResults = groupedResults.ContainsKey(service.Id)
                ? groupedResults[service.Id]
                : new List<HealthCheckResult>();

            var latestResult = await _resultRepository.GetLatestByServiceIdAsync(service.Id, cancellationToken);

            var analytics = new ServiceAnalyticsDto
            {
                ServiceId = service.Id,
                ServiceName = service.Name,
                ServiceType = service.Type,
                CurrentStatus = latestResult?.Status,
                LastCheckedAt = latestResult?.CheckedAt,
                TotalChecks = serviceResults.Count
            };

            if (serviceResults.Any())
            {
                // Рассчитываем доступность
                // Вычисляем время обслуживания, которое нужно исключить из расчета
                var maintenanceMinutes = CalculateMaintenanceMinutes(maintenancePeriods, fromDate, toDate);
                var totalMinutes = (toDate - fromDate).TotalMinutes - maintenanceMinutes;
                var healthyMinutes = 0.0;
                var degradedMinutes = 0.0;
                var unhealthyMinutes = 0.0;
                var unknownMinutes = 0.0;

                var sortedResults = serviceResults.OrderBy(r => r.CheckedAt).ToList();
                
                // Обрабатываем периоды между проверками
                // Каждая проверка определяет статус до следующей проверки
                var periodStart = fromDate;
                
                for (int i = 0; i < sortedResults.Count; i++)
                {
                    var result = sortedResults[i];
                    var periodEnd = i < sortedResults.Count - 1
                        ? sortedResults[i + 1].CheckedAt
                        : toDate;
                    
                    // Исключаем время обслуживания из этого периода
                    var effectiveDuration = ExcludeMaintenanceTime(
                        periodStart,
                        periodEnd,
                        maintenancePeriods);

                    if (effectiveDuration > 0)
                    {
                        switch (result.Status)
                        {
                            case HealthStatus.Healthy:
                                healthyMinutes += effectiveDuration;
                                break;
                            case HealthStatus.Degraded:
                                degradedMinutes += effectiveDuration;
                                break;
                            case HealthStatus.Unhealthy:
                                unhealthyMinutes += effectiveDuration;
                                break;
                            case HealthStatus.Unknown:
                                unknownMinutes += effectiveDuration;
                                break;
                        }
                    }
                    
                    periodStart = periodEnd;
                }

                // Проверяем, что сумма всех минут не превышает totalMinutes
                var totalAccountedMinutes = healthyMinutes + degradedMinutes + unhealthyMinutes + unknownMinutes;
                
                // Если сумма не совпадает с totalMinutes (из-за округлений или исключения обслуживания),
                // нормализуем значения пропорционально
                if (totalAccountedMinutes > 0 && Math.Abs(totalAccountedMinutes - totalMinutes) > 0.01)
                {
                    var ratio = totalMinutes / totalAccountedMinutes;
                    healthyMinutes *= ratio;
                    degradedMinutes *= ratio;
                    unhealthyMinutes *= ratio;
                    unknownMinutes *= ratio;
                }
                else if (totalAccountedMinutes == 0)
                {
                    // Если нет данных вообще, считаем как "неизвестно"
                    unknownMinutes = totalMinutes;
                }

                analytics.UptimePercentage = totalMinutes > 0
                    ? ((healthyMinutes + degradedMinutes * 0.5) / totalMinutes) * 100
                    : 100;

                // Статистика времени отклика
                var responseTimes = serviceResults.Where(r => r.ResponseTime > 0).Select(r => r.ResponseTime).ToList();
                if (responseTimes.Any())
                {
                    var sorted = responseTimes.OrderBy(x => x).ToList();
                    analytics.ResponseTimeStatistics = new ResponseTimeStatisticsDto
                    {
                        Average = responseTimes.Average(),
                        Median = sorted[sorted.Count / 2],
                        Min = responseTimes.Min(),
                        Max = responseTimes.Max(),
                        P95 = sorted.Count > 0 ? sorted[(int)(sorted.Count * 0.95)] : 0,
                        P99 = sorted.Count > 0 ? sorted[(int)(sorted.Count * 0.99)] : 0
                    };
                }

                // Инциденты
                var serviceIncidents = CalculateServiceIncidents(serviceResults, service);
                analytics.IncidentCount = serviceIncidents.Count;
                // Исключаем время обслуживания из времени недоступности
                var serviceDowntimeMinutes = unhealthyMinutes + unknownMinutes;
                var serviceMaintenanceMinutes = CalculateMaintenanceMinutes(maintenancePeriods, fromDate, toDate);
                analytics.TotalDowntimeMinutes = Math.Max(0, serviceDowntimeMinutes - serviceMaintenanceMinutes);

                // Метрики размера БД (только для Database сервисов)
                if (service.Type == ServiceType.Database)
                {
                    analytics.DatabaseSizeMetrics = ExtractDatabaseSizeMetrics(serviceResults, service);
                }
            }

            servicesAnalytics.Add(analytics);
        }

        return servicesAnalytics;
    }

    private DatabaseSizeMetricsDto? ExtractDatabaseSizeMetrics(
        List<HealthCheckResult> results,
        Service service)
    {
        // Ищем последний результат с метриками размера БД
        var latestWithMetrics = results
            .Where(r => !string.IsNullOrEmpty(r.Metadata))
            .OrderByDescending(r => r.CheckedAt)
            .FirstOrDefault();

        if (latestWithMetrics == null || string.IsNullOrEmpty(latestWithMetrics.Metadata))
        {
            return null;
        }

        try
        {
            var metadata = JsonSerializer.Deserialize<JsonElement>(latestWithMetrics.Metadata);
            
            var metrics = new DatabaseSizeMetricsDto
            {
                LastUpdated = latestWithMetrics.CheckedAt
            };

            // Извлекаем метрики из Metadata
            // DatabaseHealthCheckProvider сохраняет данные в PascalCase (по умолчанию System.Text.Json)
            if (!metadata.TryGetProperty("databaseSize", out var dbSize))
            {
                _logger.LogDebug("No databaseSize property found in metadata for service {ServiceId}. Metadata keys: {Keys}",
                    service.Id, string.Join(", ", metadata.EnumerateObject().Select(p => p.Name)));
                return null;
            }
            
            if (dbSize.ValueKind == System.Text.Json.JsonValueKind.Null)
            {
                _logger.LogDebug("databaseSize is null for service {ServiceId}", service.Id);
                return null;
            }
            
            // Логируем доступные свойства для отладки
            if (dbSize.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                _logger.LogDebug("Extracting database size metrics for service {ServiceId}. Available properties: {Properties}",
                    service.Id, string.Join(", ", dbSize.EnumerateObject().Select(p => p.Name)));
            }
            
            // Используем правильные имена полей из DatabaseHealthCheckProvider (PascalCase)
                if (dbSize.TryGetProperty("TotalSizeMB", out var totalSize))
                    metrics.TotalSizeMB = totalSize.GetDouble();
                else if (dbSize.TryGetProperty("totalSizeMB", out var totalSizeLower))
                    metrics.TotalSizeMB = totalSizeLower.GetDouble();
                
                if (dbSize.TryGetProperty("DataSizeMB", out var dataSize))
                    metrics.DataSizeMB = dataSize.GetDouble();
                else if (dbSize.TryGetProperty("dataSizeMB", out var dataSizeLower))
                    metrics.DataSizeMB = dataSizeLower.GetDouble();
                
                if (dbSize.TryGetProperty("LogSizeMB", out var logSize))
                    metrics.LogSizeMB = logSize.GetDouble();
                else if (dbSize.TryGetProperty("logSizeMB", out var logSizeLower))
                    metrics.LogSizeMB = logSizeLower.GetDouble();
                
                // DatabaseHealthCheckProvider использует TotalUsedMB и TotalFreeMB
                if (dbSize.TryGetProperty("TotalUsedMB", out var usedSpace))
                    metrics.UsedSpaceMB = usedSpace.GetDouble();
                else if (dbSize.TryGetProperty("totalUsedMB", out var usedSpaceLower))
                    metrics.UsedSpaceMB = usedSpaceLower.GetDouble();
                else if (dbSize.TryGetProperty("usedSpaceMB", out var usedSpaceAlt))
                    metrics.UsedSpaceMB = usedSpaceAlt.GetDouble();
                
                if (dbSize.TryGetProperty("TotalFreeMB", out var freeSpace))
                    metrics.FreeSpaceMB = freeSpace.GetDouble();
                else if (dbSize.TryGetProperty("totalFreeMB", out var freeSpaceLower))
                    metrics.FreeSpaceMB = freeSpaceLower.GetDouble();
                else if (dbSize.TryGetProperty("freeSpaceMB", out var freeSpaceAlt))
                    metrics.FreeSpaceMB = freeSpaceAlt.GetDouble();
            
            // Логируем извлеченные метрики
            if (metrics.TotalSizeMB > 0)
            {
                _logger.LogDebug("Extracted database size metrics for service {ServiceId}: Total={TotalSizeMB}MB, Used={UsedSpaceMB}MB, Free={FreeSpaceMB}MB",
                    service.Id, metrics.TotalSizeMB, metrics.UsedSpaceMB, metrics.FreeSpaceMB);
            }
            else
            {
                _logger.LogWarning("Failed to extract database size metrics for service {ServiceId}. TotalSizeMB is 0 or not found",
                    service.Id);
                return null;
            }

            // Рассчитываем изменение размера (сравниваем с первым результатом в периоде)
            var firstWithMetrics = results
                .Where(r => !string.IsNullOrEmpty(r.Metadata))
                .OrderBy(r => r.CheckedAt)
                .FirstOrDefault();

            if (firstWithMetrics != null && !string.IsNullOrEmpty(firstWithMetrics.Metadata))
            {
                try
                {
                    var firstMetadata = JsonSerializer.Deserialize<JsonElement>(firstWithMetrics.Metadata);
                    if (firstMetadata.TryGetProperty("databaseSize", out var firstDbSize))
                    {
                        double? firstTotal = null;
                        if (firstDbSize.TryGetProperty("TotalSizeMB", out var firstTotalSize))
                            firstTotal = firstTotalSize.GetDouble();
                        else if (firstDbSize.TryGetProperty("totalSizeMB", out var firstTotalSizeLower))
                            firstTotal = firstTotalSizeLower.GetDouble();
                        
                        if (firstTotal.HasValue && metrics.TotalSizeMB > 0 && firstTotal.Value > 0)
                        {
                            metrics.SizeChangeMB = metrics.TotalSizeMB - firstTotal.Value;
                            metrics.SizeChangePercentage = ((metrics.TotalSizeMB - firstTotal.Value) / firstTotal.Value) * 100;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse first database size metrics for service {ServiceId}", service.Id);
                }
            }

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract database size metrics for service {ServiceId}", service.Id);
            return null;
        }
    }

    private TimeSeriesDataDto CalculateTimeSeries(
        List<HealthCheckResult> results,
        DateTime fromDate,
        DateTime toDate,
        string period)
    {
        var timeSeries = new TimeSeriesDataDto();

        if (!results.Any())
        {
            return timeSeries;
        }

        // Определяем интервал агрегации
        TimeSpan interval;
        switch (period.ToLower())
        {
            case "24h":
                interval = TimeSpan.FromHours(1);
                break;
            case "7d":
                interval = TimeSpan.FromDays(1);
                break;
            case "1y":
                interval = TimeSpan.FromDays(30); // месяцы
                break;
            default:
                interval = TimeSpan.FromHours(1);
                break;
        }

        // Группируем по интервалам
        var grouped = results
            .GroupBy(r => new DateTime(
                r.CheckedAt.Year,
                r.CheckedAt.Month,
                r.CheckedAt.Day,
                period == "24h" ? r.CheckedAt.Hour : 0,
                0, 0))
            .OrderBy(g => g.Key)
            .ToList();

        // Доступность по времени
        foreach (var group in grouped)
        {
            var timestamp = group.Key;
            var groupResults = group.ToList();
            var healthyCount = groupResults.Count(r => r.Status == HealthStatus.Healthy);
            var degradedCount = groupResults.Count(r => r.Status == HealthStatus.Degraded);
            var total = groupResults.Count;
            
            var uptime = total > 0
                ? ((healthyCount + degradedCount * 0.5) / total) * 100
                : 100;

            timeSeries.UptimeSeries.Add(new TimeSeriesPointDto
            {
                Timestamp = timestamp,
                Value = uptime
            });
        }

        // Время отклика по времени
        foreach (var group in grouped)
        {
            var timestamp = group.Key;
            var responseTimes = group
                .Where(r => r.ResponseTime > 0)
                .Select(r => r.ResponseTime)
                .ToList();

            if (responseTimes.Any())
            {
                timeSeries.ResponseTimeSeries.Add(new TimeSeriesPointDto
                {
                    Timestamp = timestamp,
                    Value = responseTimes.Average()
                });
            }
        }

        // Распределение статусов
        foreach (var group in grouped)
        {
            var timestamp = group.Key;
            var groupResults = group.ToList();

            timeSeries.StatusDistributionSeries.Add(new StatusDistributionPointDto
            {
                Timestamp = timestamp,
                Healthy = groupResults.Count(r => r.Status == HealthStatus.Healthy),
                Degraded = groupResults.Count(r => r.Status == HealthStatus.Degraded),
                Unhealthy = groupResults.Count(r => r.Status == HealthStatus.Unhealthy),
                Unknown = groupResults.Count(r => r.Status == HealthStatus.Unknown)
            });
        }

        // Количество проверок
        foreach (var group in grouped)
        {
            timeSeries.CheckCountSeries.Add(new TimeSeriesPointDto
            {
                Timestamp = group.Key,
                Value = group.Count()
            });
        }

        return timeSeries;
    }

    private List<IncidentDto> CalculateIncidents(
        Dictionary<Guid, List<HealthCheckResult>> groupedResults,
        List<Service> services)
    {
        var incidents = new List<IncidentDto>();

        foreach (var kvp in groupedResults)
        {
            var serviceId = kvp.Key;
            var results = kvp.Value.OrderBy(r => r.CheckedAt).ToList();
            var service = services.FirstOrDefault(s => s.Id == serviceId);
            if (service == null) continue;

            var serviceIncidents = CalculateServiceIncidents(results, service);
            incidents.AddRange(serviceIncidents);
        }

        return incidents.OrderByDescending(i => i.StartTime).ToList();
    }

    private List<IncidentDto> CalculateServiceIncidents(
        List<HealthCheckResult> results,
        Service service)
    {
        var incidents = new List<IncidentDto>();

        if (results.Count == 0)
        {
            return incidents;
        }

        var sortedResults = results.OrderBy(r => r.CheckedAt).ToList();
        HealthStatus? previousStatus = null;
        DateTime? incidentStart = null;
        HealthStatus? statusBefore = null;
        string? incidentStartReason = null; // Причина начала инцидента

        foreach (var result in sortedResults)
        {
            // Начало инцидента (переход в Unhealthy или Degraded)
            if (result.Status == HealthStatus.Unhealthy || result.Status == HealthStatus.Degraded)
            {
                if (incidentStart == null)
                {
                    incidentStart = result.CheckedAt;
                    // Сохраняем статус предыдущего результата как статус "до" инцидента
                    // previousStatus содержит статус ДО текущего результата
                    statusBefore = previousStatus ?? HealthStatus.Healthy;
                    // Сохраняем причину начала инцидента из результата, который начал инцидент
                    incidentStartReason = result.Exception ?? result.Message;
                }
            }
            // Конец инцидента (восстановление)
            else if (incidentStart != null && 
                     (result.Status == HealthStatus.Healthy || result.Status == HealthStatus.Unknown))
            {
                var duration = (result.CheckedAt - incidentStart.Value).TotalMinutes;
                incidents.Add(new IncidentDto
                {
                    Id = Guid.NewGuid(),
                    ServiceId = service.Id,
                    ServiceName = service.Name,
                    StartTime = incidentStart.Value,
                    EndTime = result.CheckedAt,
                    DurationMinutes = duration,
                    StatusBefore = statusBefore ?? HealthStatus.Healthy,
                    StatusAfter = result.Status,
                    Reason = incidentStartReason // Используем причину начала инцидента
                });

                incidentStart = null;
                statusBefore = null;
                incidentStartReason = null;
            }

            // Всегда обновляем previousStatus в конце обработки каждого результата
            // Это гарантирует, что при следующей итерации previousStatus будет содержать
            // статус предыдущего результата
            previousStatus = result.Status;
        }

        // Если инцидент еще не закончился
        if (incidentStart != null)
        {
            var lastResult = sortedResults.Last();
            var duration = (DateTime.UtcNow - incidentStart.Value).TotalMinutes;
            incidents.Add(new IncidentDto
            {
                Id = Guid.NewGuid(),
                ServiceId = service.Id,
                ServiceName = service.Name,
                StartTime = incidentStart.Value,
                EndTime = null,
                DurationMinutes = duration,
                StatusBefore = statusBefore ?? HealthStatus.Healthy,
                StatusAfter = lastResult.Status,
                Reason = incidentStartReason // Используем причину начала инцидента
            });
        }

        return incidents;
    }

    private List<ServiceTypeStatisticsDto> CalculateServiceTypeStatistics(
        List<ServiceAnalyticsDto> services)
    {
        var grouped = services
            .GroupBy(s => s.ServiceType)
            .Select(g => new ServiceTypeStatisticsDto
            {
                ServiceType = g.Key,
                ServiceTypeName = g.Key.ToString(),
                ServiceCount = g.Count(),
                AverageUptimePercentage = g.Average(s => s.UptimePercentage),
                AverageResponseTime = g.Where(s => s.ResponseTimeStatistics.Average > 0)
                    .DefaultIfEmpty()
                    .Average(s => s?.ResponseTimeStatistics.Average ?? 0),
                TotalIncidents = g.Sum(s => s.IncidentCount)
            })
            .ToList();

        return grouped;
    }

    private List<DatabaseSizeTimeSeriesPointDto> CalculateDatabaseSizeTimeSeries(
        List<Service> databaseServices,
        Dictionary<Guid, List<HealthCheckResult>> groupedResults,
        DateTime fromDate,
        DateTime toDate,
        string period)
    {
        var timeSeries = new List<DatabaseSizeTimeSeriesPointDto>();

        if (databaseServices.Count == 0)
        {
            return timeSeries;
        }

        // Определяем интервал агрегации
        TimeSpan interval;
        switch (period.ToLower())
        {
            case "24h":
                interval = TimeSpan.FromHours(1);
                break;
            case "7d":
                interval = TimeSpan.FromDays(1);
                break;
            case "1y":
                interval = TimeSpan.FromDays(30); // месяцы
                break;
            default:
                interval = TimeSpan.FromHours(1);
                break;
        }

        // Для каждого Database сервиса извлекаем метрики размера из всех результатов
        foreach (var service in databaseServices)
        {
            if (!groupedResults.ContainsKey(service.Id))
            {
                continue;
            }

            var serviceResults = groupedResults[service.Id]
                .Where(r => !string.IsNullOrEmpty(r.Metadata))
                .OrderBy(r => r.CheckedAt)
                .ToList();

            foreach (var result in serviceResults)
            {
                try
                {
                    if (string.IsNullOrEmpty(result.Metadata))
                        continue;

                    var metadata = JsonSerializer.Deserialize<JsonElement>(result.Metadata!);
                    if (!metadata.TryGetProperty("databaseSize", out var dbSize) || dbSize.ValueKind == JsonValueKind.Null)
                    {
                        continue;
                    }

                    var point = new DatabaseSizeTimeSeriesPointDto
                    {
                        Timestamp = result.CheckedAt,
                        ServiceId = service.Id.ToString(),
                        ServiceName = service.Name
                    };

                    // Используем правильные имена полей из DatabaseHealthCheckProvider (PascalCase по умолчанию)
                    if (dbSize.TryGetProperty("TotalSizeMB", out var totalSize))
                        point.TotalSizeMB = totalSize.GetDouble();
                    else if (dbSize.TryGetProperty("totalSizeMB", out var totalSizeLower))
                        point.TotalSizeMB = totalSizeLower.GetDouble();

                    if (dbSize.TryGetProperty("DataSizeMB", out var dataSize))
                        point.DataSizeMB = dataSize.GetDouble();
                    else if (dbSize.TryGetProperty("dataSizeMB", out var dataSizeLower))
                        point.DataSizeMB = dataSizeLower.GetDouble();

                    if (dbSize.TryGetProperty("LogSizeMB", out var logSize))
                        point.LogSizeMB = logSize.GetDouble();
                    else if (dbSize.TryGetProperty("logSizeMB", out var logSizeLower))
                        point.LogSizeMB = logSizeLower.GetDouble();

                    // DatabaseHealthCheckProvider использует TotalUsedMB и TotalFreeMB
                    if (dbSize.TryGetProperty("TotalUsedMB", out var usedSpace))
                        point.UsedSpaceMB = usedSpace.GetDouble();
                    else if (dbSize.TryGetProperty("totalUsedMB", out var usedSpaceLower))
                        point.UsedSpaceMB = usedSpaceLower.GetDouble();
                    else if (dbSize.TryGetProperty("usedSpaceMB", out var usedSpaceAlt))
                        point.UsedSpaceMB = usedSpaceAlt.GetDouble();

                    if (dbSize.TryGetProperty("TotalFreeMB", out var freeSpace))
                        point.FreeSpaceMB = freeSpace.GetDouble();
                    else if (dbSize.TryGetProperty("totalFreeMB", out var freeSpaceLower))
                        point.FreeSpaceMB = freeSpaceLower.GetDouble();
                    else if (dbSize.TryGetProperty("freeSpaceMB", out var freeSpaceAlt))
                        point.FreeSpaceMB = freeSpaceAlt.GetDouble();

                    // Пропускаем точки с некорректными данными (размер должен быть > 0)
                    if (point.TotalSizeMB <= 0 || point.UsedSpaceMB < 0)
                    {
                        _logger.LogDebug("Skipping invalid DB size point for service {ServiceId} at {CheckedAt}: TotalSizeMB={Total}, UsedSpaceMB={Used}",
                            service.Id, result.CheckedAt, point.TotalSizeMB, point.UsedSpaceMB);
                        continue;
                    }

                    point.UsagePercentage = point.TotalSizeMB > 0
                        ? (point.UsedSpaceMB / point.TotalSizeMB) * 100
                        : 0;

                    timeSeries.Add(point);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to extract database size from metadata for service {ServiceId} at {CheckedAt}",
                        service.Id, result.CheckedAt);
                }
            }
        }

        // Сортируем по времени
        return timeSeries.OrderBy(p => p.Timestamp).ToList();
    }

    private List<DatabaseSizeForecastPointDto> CalculateDatabaseSizeForecast(
        List<DatabaseSizeTimeSeriesPointDto> historicalData)
    {
        var forecasts = new List<DatabaseSizeForecastPointDto>();

        if (historicalData.Count < 2)
        {
            // Недостаточно данных для прогноза
            return forecasts;
        }

        // Группируем данные по сервисам
        var byService = historicalData
            .GroupBy(p => p.ServiceId)
            .ToList();

        foreach (var serviceGroup in byService)
        {
            var servicePoints = serviceGroup.OrderBy(p => p.Timestamp).ToList();
            var serviceName = servicePoints.First().ServiceName;
            var serviceId = servicePoints.First().ServiceId;

            // Используем последние 90*1440 точек для расчёта тренда (90 дней при 1 точке/мин, или все, если меньше)
            const int maxPointsForTrend = 90 * 1440;
            var pointsForTrend = servicePoints
                .TakeLast(Math.Min(maxPointsForTrend, servicePoints.Count))
                .ToList();

            if (pointsForTrend.Count < 2)
            {
                continue;
            }

            // Линейная регрессия для прогноза
            var (slope, intercept) = CalculateLinearRegression(
                pointsForTrend.Select(p => (double)(p.Timestamp - pointsForTrend.First().Timestamp).TotalDays).ToList(),
                pointsForTrend.Select(p => p.TotalSizeMB).ToList());

            var (slopeUsed, interceptUsed) = CalculateLinearRegression(
                pointsForTrend.Select(p => (double)(p.Timestamp - pointsForTrend.First().Timestamp).TotalDays).ToList(),
                pointsForTrend.Select(p => p.UsedSpaceMB).ToList());

            // Рассчитываем скорость роста (МБ/день)
            var growthRateMBPerDay = slope;

            // Последняя точка данных
            var lastPoint = servicePoints.Last();
            var lastDate = lastPoint.Timestamp;

            // Прогноз на 7, 14, 30, 60, 90 дней — больше точек для плавной линии тренда
            var forecastDays = new[] { 7, 14, 30, 60, 90 };

            foreach (var daysAhead in forecastDays)
            {
                var forecastDate = lastDate.AddDays(daysAhead);

                // Прогноз = последнее значение + скорость роста * дней вперёд
                var forecastedTotal = lastPoint.TotalSizeMB + slope * daysAhead;
                var forecastedUsed = lastPoint.UsedSpaceMB + slopeUsed * daysAhead;

                // Не позволяем прогнозу быть меньше текущего значения
                if (forecastedTotal < lastPoint.TotalSizeMB)
                {
                    forecastedTotal = lastPoint.TotalSizeMB;
                }
                if (forecastedUsed < lastPoint.UsedSpaceMB)
                {
                    forecastedUsed = lastPoint.UsedSpaceMB;
                }

                var forecastedUsage = forecastedTotal > 0
                    ? (forecastedUsed / forecastedTotal) * 100
                    : 0;

                // Рассчитываем дату заполнения БД (если скорость роста положительная)
                DateTime? estimatedFullDate = null;
                if (slopeUsed > 0 && lastPoint.TotalSizeMB > 0)
                {
                    var daysToFull = (lastPoint.TotalSizeMB - lastPoint.UsedSpaceMB) / slopeUsed;
                    if (daysToFull > 0 && daysToFull < 3650) // Не более 10 лет
                    {
                        estimatedFullDate = lastDate.AddDays(daysToFull);
                    }
                }

                forecasts.Add(new DatabaseSizeForecastPointDto
                {
                    Timestamp = forecastDate,
                    ServiceId = serviceId,
                    ServiceName = serviceName,
                    ForecastedTotalSizeMB = Math.Round(forecastedTotal, 2),
                    ForecastedUsedSpaceMB = Math.Round(forecastedUsed, 2),
                    ForecastedUsagePercentage = Math.Round(forecastedUsage, 2),
                    GrowthRateMBPerDay = Math.Round(growthRateMBPerDay, 2),
                    EstimatedFullDate = estimatedFullDate
                });
            }
        }

        return forecasts.OrderBy(f => f.Timestamp).ToList();
    }

    /// <summary>
    /// Вычисляет линейную регрессию (y = slope * x + intercept)
    /// </summary>
    private (double slope, double intercept) CalculateLinearRegression(
        List<double> xValues,
        List<double> yValues)
    {
        if (xValues.Count != yValues.Count || xValues.Count < 2)
        {
            return (0, 0);
        }

        var n = xValues.Count;
        var sumX = xValues.Sum();
        var sumY = yValues.Sum();
        var sumXY = xValues.Zip(yValues, (x, y) => x * y).Sum();
        var sumX2 = xValues.Sum(x => x * x);

        var denominator = n * sumX2 - sumX * sumX;
        if (Math.Abs(denominator) < 1e-10)
        {
            // Все x одинаковы — нет вариации, возвращаем горизонтальную линию на уровне среднего
            return (0, sumY / n);
        }

        var slope = (n * sumXY - sumX * sumY) / denominator;
        var intercept = (sumY - slope * sumX) / n;

        return (slope, intercept);
    }

    private TopServicesDto CalculateTopServices(List<ServiceAnalyticsDto> services)
    {
        return new TopServicesDto
        {
            TopByUptime = services
                .OrderByDescending(s => s.UptimePercentage)
                .Take(10)
                .ToList(),
            BottomByUptime = services
                .OrderBy(s => s.UptimePercentage)
                .Take(10)
                .ToList(),
            TopByResponseTime = services
                .Where(s => s.ResponseTimeStatistics.Average > 0)
                .OrderBy(s => s.ResponseTimeStatistics.Average)
                .Take(10)
                .ToList(),
            BottomByResponseTime = services
                .Where(s => s.ResponseTimeStatistics.Average > 0)
                .OrderByDescending(s => s.ResponseTimeStatistics.Average)
                .Take(10)
                .ToList(),
            TopByIncidents = services
                .OrderByDescending(s => s.IncidentCount)
                .Take(10)
                .ToList(),
            TopDatabaseBySize = services
                .Where(s => s.ServiceType == ServiceType.Database && s.DatabaseSizeMetrics != null)
                .OrderByDescending(s => s.DatabaseSizeMetrics!.TotalSizeMB)
                .Take(10)
                .ToList()
        };
    }
}

