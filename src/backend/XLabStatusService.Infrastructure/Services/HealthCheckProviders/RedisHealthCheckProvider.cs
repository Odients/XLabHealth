using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Enums;
using XLabStatusService.Core.Interfaces;

namespace XLabStatusService.Infrastructure.Services.HealthCheckProviders;

/// <summary>
/// Провайдер для проверки Redis Server
/// </summary>
public class RedisHealthCheckProvider : IHealthCheckProvider
{
    private readonly ILogger<RedisHealthCheckProvider> _logger;

    public RedisHealthCheckProvider(ILogger<RedisHealthCheckProvider> logger)
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

        ConnectionMultiplexer? redis = null;

        try
        {
            if (service.Configuration == null || string.IsNullOrEmpty(service.Configuration.Parameters))
            {
                result.Status = HealthStatus.Unhealthy;
                result.Message = "Redis configuration is not set";
                return result;
            }

            // Десериализуем JSON с учетом регистра (camelCase от фронтенда)
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var config = JsonSerializer.Deserialize<RedisConfig>(service.Configuration.Parameters, jsonOptions);
            if (config == null)
            {
                result.Status = HealthStatus.Unhealthy;
                result.Message = "Invalid Redis configuration";
                return result;
            }

            // Формируем ConfigurationOptions
            ConfigurationOptions configurationOptions;
            
            if (!string.IsNullOrEmpty(config.RedisConnection))
            {
                // Если указана полная строка подключения, используем её
                configurationOptions = ConfigurationOptions.Parse(config.RedisConnection);
            }
            else if (!string.IsNullOrEmpty(config.Host))
            {
                // Если указаны Host и Port, формируем ConfigurationOptions напрямую
                configurationOptions = new ConfigurationOptions
                {
                    EndPoints = { { config.Host, config.Port } },
                    Ssl = config.UseSsl
                };
            }
            else
            {
                result.Status = HealthStatus.Unhealthy;
                result.Message = "Redis connection string or Host must be specified";
                return result;
            }

            // Устанавливаем таймауты
            configurationOptions.ConnectTimeout = service.Timeout;
            configurationOptions.SyncTimeout = service.Timeout;
            // Отключаем AbortOnConnectFail для более корректной обработки ошибок
            configurationOptions.AbortOnConnectFail = false;

            // Подключаемся к Redis с поддержкой cancellationToken
            redis = await ConnectionMultiplexer.ConnectAsync(configurationOptions).WaitAsync(cancellationToken);
            
            // Проверяем, что соединение действительно установлено
            if (!redis.IsConnected)
            {
                result.Status = HealthStatus.Unhealthy;
                result.Message = "Redis connection established but not connected";
                result.ResponseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                return result;
            }

            var database = redis.GetDatabase();

            // Выполняем PING для проверки доступности с поддержкой cancellationToken
            var pingResult = await database.PingAsync().WaitAsync(cancellationToken);
            var responseTime = (int)pingResult.TotalMilliseconds;

            result.ResponseTime = responseTime;
            result.Status = HealthStatus.Healthy;
            result.Message = "Redis server is available";

            // Собираем дополнительные метрики
            if (config.CheckMemoryUsage || config.CheckConnectedClients)
            {
                try
                {
                    var endpoints = redis.GetEndPoints();
                    if (endpoints.Length > 0)
                    {
                        var server = redis.GetServer(endpoints[0]);
                        var metrics = new Dictionary<string, object>();

                        if (config.CheckMemoryUsage)
                        {
                            var memoryInfo = await server.InfoAsync("memory").WaitAsync(cancellationToken);
                            if (memoryInfo != null)
                            {
                                metrics["Memory"] = memoryInfo;
                            }
                        }

                        if (config.CheckConnectedClients)
                        {
                            var clientsInfo = await server.InfoAsync("clients").WaitAsync(cancellationToken);
                            if (clientsInfo != null)
                            {
                                metrics["Clients"] = clientsInfo;
                            }
                        }

                        if (metrics.Count > 0)
                        {
                            result.Metadata = JsonSerializer.Serialize(metrics);
                        }
                    }
                }
                catch (RedisCommandException ex) when (ex.Message.Contains("admin mode"))
                {
                    // Redis INFO command requires admin mode, which may be disabled in managed Redis services
                    // This is expected behavior for some Redis configurations, so we log at Debug level
                    _logger.LogDebug(
                        "Extended metrics collection skipped for Redis service {ServiceId}: Admin mode is not enabled. " +
                        "This is normal for managed Redis services (Azure Redis Cache, AWS ElastiCache, etc.). " +
                        "Basic health check (PING) completed successfully.",
                        service.Id);
                }
                catch (Exception ex)
                {
                    // Логируем ошибку сбора метрик, но не прерываем успешную проверку
                    _logger.LogWarning(ex, "Failed to collect extended metrics for Redis service {ServiceId}", service.Id);
                }
            }
        }
        catch (OperationCanceledException)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = "Redis health check was cancelled";
            result.ResponseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogWarning("Redis health check cancelled for service {ServiceId}", service.Id);
        }
        catch (RedisConnectionException ex)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = $"Redis connection failed: {ex.Message}";
            result.Exception = ex.ToString();
            result.ResponseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Redis connection error for service {ServiceId}", service.Id);
        }
        catch (Exception ex)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = ex.Message;
            result.Exception = ex.ToString();
            result.ResponseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Error checking Redis health for service {ServiceId}", service.Id);
        }
        finally
        {
            // Используем DisposeAsync для правильного освобождения ресурсов
            if (redis != null)
            {
                try
                {
                    await redis.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing Redis connection for service {ServiceId}", service.Id);
                }
            }
        }

        return result;
    }

    private class RedisConfig
    {
        public string? Host { get; set; }
        public int Port { get; set; } = 6379;
        public string? RedisConnection { get; set; }
        public bool UseSsl { get; set; }
        public bool CheckMemoryUsage { get; set; }
        public bool CheckConnectedClients { get; set; }
    }
}

