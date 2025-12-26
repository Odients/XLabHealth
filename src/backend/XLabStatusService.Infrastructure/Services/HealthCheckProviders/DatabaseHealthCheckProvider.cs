using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Enums;
using XLabStatusService.Core.Interfaces;

namespace XLabStatusService.Infrastructure.Services.HealthCheckProviders;

/// <summary>
/// Провайдер для проверки MS SQL Server базы данных
/// </summary>
public class DatabaseHealthCheckProvider : IHealthCheckProvider
{
    private readonly ILogger<DatabaseHealthCheckProvider> _logger;

    public DatabaseHealthCheckProvider(ILogger<DatabaseHealthCheckProvider> logger)
    {
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(Service service, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new HealthCheckResult
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            CheckedAt = startTime,
            Status = HealthStatus.Unknown
        };

        try
        {
            if (service.Configuration == null || string.IsNullOrEmpty(service.Configuration.Parameters))
            {
                result.Status = HealthStatus.Unhealthy;
                result.Message = "Database connection string is not configured";
                return result;
            }

            // Десериализуем JSON с учетом регистра (camelCase от фронтенда)
            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var config = System.Text.Json.JsonSerializer.Deserialize<DatabaseConfig>(service.Configuration.Parameters, jsonOptions);
            if (config == null || string.IsNullOrEmpty(config.ConnectionString))
            {
                result.Status = HealthStatus.Unhealthy;
                result.Message = "Database connection string is missing in service configuration";
                _logger.LogWarning("Database connection string is missing for service {ServiceId}. Parameters: {Parameters}", 
                    service.Id, service.Configuration.Parameters);
                return result;
            }

            // Используем строку подключения из настроек сервиса
            _logger.LogDebug("Using connection string from service configuration for service {ServiceId}", service.Id);
            using var connection = new SqlConnection(config.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var testQuery = config.TestQuery ?? "SELECT 1";
            using var command = new SqlCommand(testQuery, connection);
            // Convert milliseconds to seconds, minimum 1 second
            command.CommandTimeout = service.Timeout > 0 ? Math.Max(1, service.Timeout / 1000) : 30;

            await command.ExecuteScalarAsync(cancellationToken);

            var responseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            result.ResponseTime = responseTime;
            result.Status = HealthStatus.Healthy;
            result.Message = "Database connection successful";

            // Расширенные метрики (если включены)
            if (config.CheckDatabaseSize || config.CheckActiveConnections || config.CheckPerformance)
            {
                try
                {
                    var metrics = await CollectMetricsAsync(connection, config, cancellationToken);
                    if (metrics.Count > 0)
                    {
                        result.Metadata = System.Text.Json.JsonSerializer.Serialize(metrics);
                    }
                }
                catch (Exception ex)
                {
                    // Логируем ошибку сбора метрик, но не прерываем успешную проверку
                    _logger.LogWarning(ex, "Failed to collect extended metrics for service {ServiceId}", service.Id);
                }
            }
        }
        catch (SqlException ex)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = $"Database error: {ex.Message}";
            result.Exception = ex.ToString();
            result.ResponseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Database health check failed for service {ServiceId}", service.Id);
        }
        catch (Exception ex)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = ex.Message;
            result.Exception = ex.ToString();
            result.ResponseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Error checking database health for service {ServiceId}", service.Id);
        }

        return result;
    }

    private async Task<Dictionary<string, object>> CollectMetricsAsync(
        SqlConnection connection,
        DatabaseConfig config,
        CancellationToken cancellationToken)
    {
        var metrics = new Dictionary<string, object>();

        // Получаем имя текущей базы данных из connection string для логирования и проверки
        string? databaseName = null;
        try
        {
            using var dbNameCommand = new SqlCommand("SELECT DB_NAME()", connection);
            var dbNameResult = await dbNameCommand.ExecuteScalarAsync(cancellationToken);
            databaseName = dbNameResult?.ToString();
            _logger.LogDebug("Collecting metrics for database: {DatabaseName}", databaseName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get database name");
        }

        if (config.CheckDatabaseSize)
        {
            // Упрощенный запрос для получения размеров и использованного пространства
            // Используем только sys.database_files и FILEPROPERTY, которые не требуют специальных прав
            // sys.database_files автоматически работает с текущей БД подключения (указанной в connection string)
            var sizeQuery = @"
                SELECT 
                    -- Размеры файлов текущей базы данных
                    SUM(size) * 8 / 1024.0 AS TotalSizeMB,
                    SUM(CASE WHEN type_desc = 'ROWS' THEN size END) * 8 / 1024.0 AS DataSizeMB,
                    SUM(CASE WHEN type_desc = 'LOG' THEN size END) * 8 / 1024.0 AS LogSizeMB,
                    -- Использованное пространство через FILEPROPERTY (не требует специальных прав)
                    SUM(CASE WHEN type_desc = 'ROWS' THEN FILEPROPERTY(name, 'SpaceUsed') * 8 / 1024.0 ELSE 0 END) AS DataUsedMB,
                    SUM(CASE WHEN type_desc = 'LOG' THEN FILEPROPERTY(name, 'SpaceUsed') * 8 / 1024.0 ELSE 0 END) AS LogUsedMB
                FROM sys.database_files";
            
            try
            {
                double totalSizeMB = 0;
                double dataSizeMB = 0;
                double logSizeMB = 0;
                double dataUsedMB = 0;
                double logUsedMB = 0;
                
                using (var command = new SqlCommand(sizeQuery, connection))
                {
                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        totalSizeMB = reader.IsDBNull(0) ? 0 : (double)reader.GetDecimal(0);
                        dataSizeMB = reader.IsDBNull(1) ? 0 : (double)reader.GetDecimal(1);
                        logSizeMB = reader.IsDBNull(2) ? 0 : (double)reader.GetDecimal(2);
                        dataUsedMB = reader.IsDBNull(3) ? 0 : (double)reader.GetDecimal(3);
                        logUsedMB = reader.IsDBNull(4) ? 0 : (double)reader.GetDecimal(4);
                    }
                }
                
                // Вычисляем свободное пространство
                var dataFreeMB = dataSizeMB - dataUsedMB;
                var logFreeMB = logSizeMB - logUsedMB;
                var totalUsedMB = dataUsedMB + logUsedMB;
                var totalFreeMB = dataFreeMB + logFreeMB;
                
                // Вычисляем проценты использования
                var usagePercent = totalSizeMB > 0 ? (totalUsedMB / totalSizeMB) * 100 : 0;
                var dataUsagePercent = dataSizeMB > 0 ? (dataUsedMB / dataSizeMB) * 100 : 0;
                var logUsagePercent = logSizeMB > 0 ? (logUsedMB / logSizeMB) * 100 : 0;
                
                // Пытаемся получить дополнительные метрики из sys.dm_db_file_space_usage
                // Это требует VIEW DATABASE STATE, но не VIEW SERVER PERFORMANCE STATE
                double? allocatedMB = null;
                double? unallocatedMB = null;
                double? versionStoreMB = null;
                double? userObjectsMB = null;
                double? internalObjectsMB = null;
                
                try
                {
                    // sys.dm_db_file_space_usage работает с текущей БД подключения
                    // Требует VIEW DATABASE STATE, но не VIEW SERVER PERFORMANCE STATE
                    var spaceUsageQuery = @"
                        SELECT 
                            SUM(allocated_extent_page_count) * 8 / 1024.0 AS AllocatedMB,
                            SUM(unallocated_extent_page_count) * 8 / 1024.0 AS UnallocatedMB,
                            SUM(version_store_reserved_page_count) * 8 / 1024.0 AS VersionStoreMB,
                            SUM(user_object_reserved_page_count) * 8 / 1024.0 AS UserObjectsMB,
                            SUM(internal_object_reserved_page_count) * 8 / 1024.0 AS InternalObjectsMB
                        FROM sys.dm_db_file_space_usage
                        WHERE database_id = DB_ID()";
                    
                    using var spaceCommand = new SqlCommand(spaceUsageQuery, connection);
                    using var spaceReader = await spaceCommand.ExecuteReaderAsync(cancellationToken);
                    
                    if (await spaceReader.ReadAsync(cancellationToken))
                    {
                        allocatedMB = spaceReader.IsDBNull(0) ? null : (double?)spaceReader.GetDecimal(0);
                        unallocatedMB = spaceReader.IsDBNull(1) ? null : (double?)spaceReader.GetDecimal(1);
                        versionStoreMB = spaceReader.IsDBNull(2) ? null : (double?)spaceReader.GetDecimal(2);
                        userObjectsMB = spaceReader.IsDBNull(3) ? null : (double?)spaceReader.GetDecimal(3);
                        internalObjectsMB = spaceReader.IsDBNull(4) ? null : (double?)spaceReader.GetDecimal(4);
                    }
                }
                catch (Exception ex)
                {
                    // Если нет прав на sys.dm_db_file_space_usage, просто пропускаем эти метрики
                    _logger.LogDebug(ex, "Additional space usage metrics not available (requires VIEW DATABASE STATE)");
                }
                
                metrics["databaseSize"] = new
                {
                    // Имя базы данных для идентификации
                    DatabaseName = databaseName,
                    // Размеры в МБ
                    TotalSizeMB = Math.Round(totalSizeMB, 2),
                    DataSizeMB = Math.Round(dataSizeMB, 2),
                    LogSizeMB = Math.Round(logSizeMB, 2),
                    DataUsedMB = Math.Round(dataUsedMB, 2),
                    DataFreeMB = Math.Round(dataFreeMB, 2),
                    LogUsedMB = Math.Round(logUsedMB, 2),
                    LogFreeMB = Math.Round(logFreeMB, 2),
                    TotalUsedMB = Math.Round(totalUsedMB, 2),
                    TotalFreeMB = Math.Round(totalFreeMB, 2),
                    // Размеры в ГБ для удобства
                    TotalSizeGB = Math.Round(totalSizeMB / 1024.0, 2),
                    DataSizeGB = Math.Round(dataSizeMB / 1024.0, 2),
                    LogSizeGB = Math.Round(logSizeMB / 1024.0, 2),
                    TotalUsedGB = Math.Round(totalUsedMB / 1024.0, 2),
                    TotalFreeGB = Math.Round(totalFreeMB / 1024.0, 2),
                    // Проценты использования
                    UsagePercent = Math.Round(usagePercent, 2),
                    DataUsagePercent = Math.Round(dataUsagePercent, 2),
                    LogUsagePercent = Math.Round(logUsagePercent, 2),
                    // Дополнительные метрики (если доступны)
                    AllocatedMB = allocatedMB.HasValue ? Math.Round(allocatedMB.Value, 2) : (double?)null,
                    UnallocatedMB = unallocatedMB.HasValue ? Math.Round(unallocatedMB.Value, 2) : (double?)null,
                    VersionStoreMB = versionStoreMB.HasValue ? Math.Round(versionStoreMB.Value, 2) : (double?)null,
                    UserObjectsMB = userObjectsMB.HasValue ? Math.Round(userObjectsMB.Value, 2) : (double?)null,
                    InternalObjectsMB = internalObjectsMB.HasValue ? Math.Round(internalObjectsMB.Value, 2) : (double?)null
                };
                
                if (!string.IsNullOrEmpty(databaseName))
                {
                    _logger.LogDebug(
                        "Database size metrics collected for {DatabaseName}: Total={TotalSizeGB}GB, Used={TotalUsedGB}GB ({UsagePercent}%)",
                        databaseName, 
                        Math.Round(totalSizeMB / 1024.0, 2),
                        Math.Round(totalUsedMB / 1024.0, 2),
                        Math.Round(usagePercent, 2));
                }
            }
            catch (Exception ex)
            {
                // Если произошла ошибка, логируем и не добавляем метрики
                _logger.LogWarning(ex, "Error collecting database size metrics");
            }
        }

        if (config.CheckActiveConnections)
        {
            // Запрос для активных соединений требует VIEW SERVER STATE
            // Используем sys.dm_exec_sessions (требует VIEW SERVER STATE)
            // Если нет прав, просто пропускаем эту метрику
            try
            {
                // Запрос через sys.dm_exec_sessions (требует VIEW SERVER STATE)
                // Фильтруем по текущей базе данных и только пользовательские процессы
                var connectionsQuery = @"
                    SELECT COUNT(*) AS ActiveConnections
                    FROM sys.dm_exec_sessions 
                    WHERE database_id = DB_ID() AND is_user_process = 1";
                
                using var command = new SqlCommand(connectionsQuery, connection);
                var connections = await command.ExecuteScalarAsync(cancellationToken);
                metrics["ActiveConnections"] = connections ?? 0;
                
                if (!string.IsNullOrEmpty(databaseName))
                {
                    _logger.LogDebug("Active connections for database {DatabaseName}: {Connections}", databaseName, connections);
                }
            }
            catch (Exception ex)
            {
                // Если нет прав на sys.dm_exec_sessions, просто пропускаем эту метрику
                _logger.LogDebug(ex, "Active connections metric not available (requires VIEW SERVER STATE)");
            }
        }

        if (config.CheckPerformance)
        {
            // Запрос для метрик производительности требует VIEW SERVER STATE
            // Если нет прав, просто пропускаем эту метрику
            try
            {
                var performanceQuery = @"
                    SELECT 
                        AVG(avg_cpu_time) AS AvgCpuTime,
                        AVG(avg_logical_io_reads) AS AvgLogicalReads,
                        AVG(avg_logical_io_writes) AS AvgLogicalWrites,
                        SUM(execution_count) AS TotalExecutions
                    FROM sys.dm_exec_query_stats";
                
                using var command = new SqlCommand(performanceQuery, connection);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    metrics["Performance"] = new
                    {
                        AvgCpuTime = reader.IsDBNull(0) ? 0 : reader.GetDouble(0),
                        AvgLogicalReads = reader.IsDBNull(1) ? 0 : reader.GetDouble(1),
                        AvgLogicalWrites = reader.IsDBNull(2) ? 0 : reader.GetDouble(2),
                        TotalExecutions = reader.IsDBNull(3) ? 0 : reader.GetInt64(3)
                    };
                }
            }
            catch (Exception ex)
            {
                // Если нет прав на sys.dm_exec_query_stats, просто пропускаем
                // Это не критично для основной проверки здоровья
                _logger.LogDebug(ex, "Performance metrics not available (requires VIEW SERVER STATE)");
            }
        }

        return metrics;
    }

    private class DatabaseConfig
    {
        public string? ConnectionString { get; set; }
        public string? TestQuery { get; set; }
        public bool CheckDatabaseSize { get; set; }
        public bool CheckActiveConnections { get; set; }
        public bool CheckPerformance { get; set; }
    }
}

