using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Enums;
using XLabStatusService.Core.Interfaces;

namespace XLabStatusService.Infrastructure.Services.HealthCheckProviders;

/// <summary>
/// Провайдер для проверки HTTP/HTTPS endpoints
/// </summary>
public class HttpHealthCheckProvider : IHealthCheckProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpHealthCheckProvider> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public HttpHealthCheckProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<HttpHealthCheckProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
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
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMilliseconds(service.Timeout);

            // Настройка заголовков из конфигурации
            if (service.Configuration != null && !string.IsNullOrEmpty(service.Configuration.Headers))
            {
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(service.Configuration.Headers);
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }

            var response = await client.GetAsync(service.Url, cancellationToken);
            var responseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            result.ResponseTime = responseTime;

            // Проверка статус кода
            var expectedStatusCode = service.Configuration?.ExpectedStatusCode ?? 200;
            var statusCodeMatches = (int)response.StatusCode == expectedStatusCode;

            // Парсинг ответа для X-Lab API (если ParseModules = true)
            var parseModules = false;
            var criticalModules = new List<string>();
            
            if (service.Configuration != null && !string.IsNullOrEmpty(service.Configuration.Parameters))
            {
                try
                {
                    var configParams = JsonSerializer.Deserialize<HttpHealthCheckConfig>(service.Configuration.Parameters, JsonOptions);
                    parseModules = configParams?.ParseModules ?? false;
                    criticalModules = configParams?.CriticalModules ?? new List<string>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse configuration parameters for service {ServiceId}", service.Id);
                }
            }

            // Если ParseModules = true, пытаемся распарсить ответ независимо от статус кода
            if (parseModules)
            {
                try
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var healthResponse = JsonSerializer.Deserialize<HealthCheckResponse>(content, JsonOptions);

                    if (healthResponse != null)
                    {
                        // Определяем статус на основе ответа и критических модулей
                        result.Status = DetermineStatus(healthResponse, criticalModules);
                        
                        // Вычисляем максимальное время отклика среди всех модулей
                        var maxModuleResponseTime = CalculateMaxModuleResponseTime(healthResponse);
                        if (maxModuleResponseTime > 0)
                        {
                            result.ResponseTime = Math.Max(result.ResponseTime, maxModuleResponseTime);
                        }

                        // Собираем ошибки из модулей
                        var moduleErrors = CollectModuleErrors(healthResponse);
                        if (moduleErrors.Any())
                        {
                            result.Exception = string.Join("; ", moduleErrors);
                        }

                        // Сохраняем детальную информацию в Metadata
                        result.Metadata = JsonSerializer.Serialize(new
                        {
                            Version = healthResponse.Version,
                            Timestamp = healthResponse.Timestamp,
                            Modules = healthResponse.Modules,
                            ModuleResponseTimes = ExtractModuleResponseTimes(healthResponse),
                            CriticalModulesStatus = GetCriticalModulesStatus(healthResponse, criticalModules)
                        }, JsonOptions);

                        result.Message = $"Service status: {healthResponse.Status}. " +
                                       (statusCodeMatches 
                                           ? $"HTTP status code: {response.StatusCode}" 
                                           : $"Unexpected HTTP status code: {response.StatusCode}");
                    }
                    else if (statusCodeMatches)
                    {
                        result.Status = HealthStatus.Healthy;
                        result.Message = $"Service responded with status code {response.StatusCode}";
                    }
                    else
                    {
                        result.Status = HealthStatus.Unhealthy;
                        result.Message = $"Service responded with unexpected status code {response.StatusCode}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse health check response for service {ServiceId}", service.Id);
                    
                    // Если не удалось распарсить, используем статус код
                    if (statusCodeMatches)
                    {
                        result.Status = HealthStatus.Healthy;
                        result.Message = $"Service responded with status code {response.StatusCode}";
                    }
                    else
                    {
                        result.Status = HealthStatus.Unhealthy;
                        result.Message = $"Service responded with unexpected status code {response.StatusCode}";
                    }
                }
            }
            else
            {
                // Если ParseModules = false, используем только статус код
                if (statusCodeMatches)
                {
                    result.Status = HealthStatus.Healthy;
                    result.Message = $"Service responded with status code {response.StatusCode}";
                }
                else
                {
                    result.Status = HealthStatus.Unhealthy;
                    result.Message = $"Service responded with unexpected status code {response.StatusCode}";
                }
            }
        }
        catch (TaskCanceledException)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = "Request timeout";
            result.ResponseTime = service.Timeout;
        }
        catch (Exception ex)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = ex.Message;
            result.Exception = ex.ToString();
            result.ResponseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Error checking health for service {ServiceId}", service.Id);
        }

        return result;
    }

    private static HealthStatus DetermineStatus(HealthCheckResponse response, List<string> criticalModules)
    {
        // Сначала используем общий статус из ответа
        var status = MapStatus(response.Status);

        // Если указаны критические модули, проверяем их статус
        if (criticalModules.Any() && response.Modules != null)
        {
            foreach (var criticalModule in criticalModules)
            {
                var moduleStatus = GetModuleCategoryStatus(response.Modules, criticalModule);
                if (moduleStatus == HealthStatus.Unhealthy)
                {
                    return HealthStatus.Unhealthy;
                }
                if (moduleStatus == HealthStatus.Degraded && status == HealthStatus.Healthy)
                {
                    status = HealthStatus.Degraded;
                }
            }
        }

        return status;
    }

    private static HealthStatus GetModuleCategoryStatus(Dictionary<string, JsonElement> modules, string categoryName)
    {
        if (!modules.TryGetValue(categoryName, out var category))
        {
            return HealthStatus.Unknown;
        }

        if (category.ValueKind != JsonValueKind.Object)
        {
            return HealthStatus.Unknown;
        }

        var hasUnhealthy = false;
        var hasDegraded = false;

        foreach (var module in category.EnumerateObject())
        {
            if (module.Value.ValueKind == JsonValueKind.Object)
            {
                if (module.Value.TryGetProperty("status", out var statusProp))
                {
                    var moduleStatus = statusProp.GetString()?.ToLowerInvariant();
                    if (moduleStatus == "unhealthy")
                    {
                        hasUnhealthy = true;
                    }
                    else if (moduleStatus == "degraded")
                    {
                        hasDegraded = true;
                    }
                }
            }
        }

        if (hasUnhealthy)
        {
            return HealthStatus.Unhealthy;
        }
        if (hasDegraded)
        {
            return HealthStatus.Degraded;
        }

        return HealthStatus.Healthy;
    }

    private static int CalculateMaxModuleResponseTime(HealthCheckResponse response)
    {
        var maxTime = 0;

        if (response.Modules == null)
        {
            return maxTime;
        }

        foreach (var category in response.Modules.Values)
        {
            if (category.ValueKind == JsonValueKind.Object)
            {
                foreach (var module in category.EnumerateObject())
                {
                    if (module.Value.ValueKind == JsonValueKind.Object &&
                        module.Value.TryGetProperty("responseTime", out var responseTimeProp))
                    {
                        if (responseTimeProp.ValueKind == JsonValueKind.Number)
                        {
                            var time = responseTimeProp.GetInt32();
                            if (time > maxTime)
                            {
                                maxTime = time;
                            }
                        }
                    }
                }
            }
        }

        return maxTime;
    }

    private static Dictionary<string, Dictionary<string, int>> ExtractModuleResponseTimes(HealthCheckResponse response)
    {
        var result = new Dictionary<string, Dictionary<string, int>>();

        if (response.Modules == null)
        {
            return result;
        }

        foreach (var category in response.Modules)
        {
            var categoryName = category.Key;
            var categoryValue = category.Value;

            if (categoryValue.ValueKind == JsonValueKind.Object)
            {
                var moduleTimes = new Dictionary<string, int>();

                foreach (var module in categoryValue.EnumerateObject())
                {
                    if (module.Value.ValueKind == JsonValueKind.Object &&
                        module.Value.TryGetProperty("responseTime", out var responseTimeProp))
                    {
                        if (responseTimeProp.ValueKind == JsonValueKind.Number)
                        {
                            moduleTimes[module.Name] = responseTimeProp.GetInt32();
                        }
                    }
                }

                if (moduleTimes.Any())
                {
                    result[categoryName] = moduleTimes;
                }
            }
        }

        return result;
    }

    private static Dictionary<string, string> GetCriticalModulesStatus(HealthCheckResponse response, List<string> criticalModules)
    {
        var result = new Dictionary<string, string>();

        if (!criticalModules.Any() || response.Modules == null)
        {
            return result;
        }

        foreach (var criticalModule in criticalModules)
        {
            if (response.Modules.TryGetValue(criticalModule, out var category))
            {
                var status = GetModuleCategoryStatus(response.Modules, criticalModule);
                result[criticalModule] = status.ToString();
            }
        }

        return result;
    }

    private static List<string> CollectModuleErrors(HealthCheckResponse response)
    {
        var errors = new List<string>();

        if (response.Modules == null)
        {
            return errors;
        }

        foreach (var category in response.Modules)
        {
            var categoryValue = category.Value;

            if (categoryValue.ValueKind == JsonValueKind.Object)
            {
                foreach (var module in categoryValue.EnumerateObject())
                {
                    if (module.Value.ValueKind == JsonValueKind.Object &&
                        module.Value.TryGetProperty("error", out var errorProp))
                    {
                        // Обрабатываем как строку, так и null значения
                        if (errorProp.ValueKind == JsonValueKind.String)
                        {
                            var error = errorProp.GetString();
                            if (!string.IsNullOrEmpty(error))
                            {
                                errors.Add($"{category.Key}.{module.Name}: {error}");
                            }
                        }
                        // Если error не null, но и не строка (например, объект), пытаемся получить строковое представление
                        else if (errorProp.ValueKind != JsonValueKind.Null)
                        {
                            var error = errorProp.ToString();
                            if (!string.IsNullOrEmpty(error))
                            {
                                errors.Add($"{category.Key}.{module.Name}: {error}");
                            }
                        }
                    }
                }
            }
        }

        return errors;
    }

    private static HealthStatus MapStatus(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "healthy" => HealthStatus.Healthy,
            "degraded" => HealthStatus.Degraded,
            "unhealthy" => HealthStatus.Unhealthy,
            _ => HealthStatus.Unknown
        };
    }

    private class HttpHealthCheckConfig
    {
        [JsonPropertyName("ParseModules")]
        public bool ParseModules { get; set; }

        [JsonPropertyName("CriticalModules")]
        public List<string>? CriticalModules { get; set; }
    }

    private class HealthCheckResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("modules")]
        public Dictionary<string, JsonElement>? Modules { get; set; }
    }
}

