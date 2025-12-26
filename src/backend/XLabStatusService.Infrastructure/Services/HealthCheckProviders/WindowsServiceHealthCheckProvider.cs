using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;
using System.ServiceProcess;
using System.Text.Json;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Enums;
using XLabStatusService.Core.Interfaces;

namespace XLabStatusService.Infrastructure.Services.HealthCheckProviders;

/// <summary>
/// Провайдер для проверки Windows Services
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsServiceHealthCheckProvider : IHealthCheckProvider
{
    private readonly ILogger<WindowsServiceHealthCheckProvider> _logger;

    public WindowsServiceHealthCheckProvider(ILogger<WindowsServiceHealthCheckProvider> logger)
    {
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(Service service, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startTime = DateTime.UtcNow;
        var result = new HealthCheckResult
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            CheckedAt = startTime,
            Status = HealthStatus.Unknown
        };

        WindowsServiceConfig? config = null;
        
        try
        {
            if (service.Configuration == null || string.IsNullOrEmpty(service.Configuration.Parameters))
            {
                result.Status = HealthStatus.Unhealthy;
                result.Message = "Windows Service configuration is not set";
                return result;
            }

            config = JsonSerializer.Deserialize<WindowsServiceConfig>(service.Configuration.Parameters);
            if (config == null || string.IsNullOrEmpty(config.ServiceName))
            {
                result.Status = HealthStatus.Unhealthy;
                result.Message = "Service name is not configured";
                return result;
            }

            // Выполняем синхронные операции ServiceController в отдельном потоке
            // чтобы не блокировать текущий поток и поддерживать асинхронность
            var serviceInfo = await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var machineName = config!.MachineName ?? ".";
                var requestedServiceName = config.ServiceName;
                
                // Пытаемся создать ServiceController с указанным именем
                // Примечание: ServiceController может быть создан даже для несуществующей службы,
                // но исключение будет выброшено при попытке доступа к свойствам
                using var serviceController = new System.ServiceProcess.ServiceController(requestedServiceName, machineName);
                
                try
                {
                    // Попытка получить статус - это вызовет исключение, если служба не найдена
                    serviceController.Refresh();
                    var serviceStatus = serviceController.Status;
                    
                    // Если дошли сюда, служба существует - получаем остальные свойства
                    return new
                    {
                        ServiceName = serviceController.ServiceName,
                        DisplayName = serviceController.DisplayName,
                        Status = serviceStatus,
                        StartType = serviceController.StartType
                    };
                }
                catch
                {
                    // Исключение при попытке получить статус означает, что служба не найдена
                    // Пробрасываем исключение дальше для обработки во внешнем catch блоке
                    throw;
                }
            }, cancellationToken);

            var responseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            result.ResponseTime = responseTime;

            // Маппинг статуса службы на HealthStatus
            result.Status = MapServiceStatus(serviceInfo.Status);
            result.Message = $"Service status: {serviceInfo.Status}";

            // Дополнительная информация
            var metadata = new Dictionary<string, object>
            {
                ["ServiceName"] = serviceInfo.ServiceName,
                ["DisplayName"] = serviceInfo.DisplayName,
                ["Status"] = serviceInfo.Status.ToString(),
                ["StartType"] = serviceInfo.StartType.ToString()
            };

            // Проверка типа запуска (если включено)
            if (config.CheckStartType && !string.IsNullOrEmpty(config.ExpectedStartType))
            {
                var expectedStartType = Enum.Parse<ServiceStartMode>(config.ExpectedStartType, true);
                if (serviceInfo.StartType != expectedStartType)
                {
                    result.Status = HealthStatus.Degraded;
                    result.Message += $". Expected start type: {expectedStartType}, actual: {serviceInfo.StartType}";
                }
            }

            result.Metadata = JsonSerializer.Serialize(metadata);
        }
        catch (OperationCanceledException)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = "Health check was cancelled";
            result.ResponseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogWarning("Windows Service health check was cancelled for service {ServiceId}", service.Id);
        }
        catch (InvalidOperationException ex)
        {
            result.Status = HealthStatus.Unhealthy;
            
            // Более понятное сообщение об ошибке
            var serviceName = config?.ServiceName ?? "unknown";
            var machineName = config?.MachineName ?? ".";
            
            // Пытаемся найти похожие службы для подсказки
            List<string>? similarServices = null;
            try
            {
                var allServices = System.ServiceProcess.ServiceController.GetServices(machineName);
                similarServices = allServices
                    .Where(s => s.ServiceName.Contains(serviceName, StringComparison.OrdinalIgnoreCase) ||
                               s.DisplayName.Contains(serviceName, StringComparison.OrdinalIgnoreCase))
                    .Take(5)
                    .Select(s => $"{s.ServiceName} (DisplayName: {s.DisplayName})")
                    .ToList();
            }
            catch
            {
                // Игнорируем ошибки при поиске похожих служб
            }
            
            if (ex.Message.Contains("was not found", StringComparison.OrdinalIgnoreCase))
            {
                var message = $"Windows Service '{serviceName}' not found on computer '{machineName}'. " +
                             "Please verify that the service is installed and the service name is correct. " +
                             "Note: Use ServiceName (short name), not DisplayName. " +
                             "You can check service names using: 'sc query' or 'Get-Service' PowerShell command.";
                
                if (similarServices != null && similarServices.Count > 0)
                {
                    message += $" Similar services found: {string.Join(", ", similarServices)}";
                }
                
                result.Message = message;
            }
            else if (ex.Message.Contains("access", StringComparison.OrdinalIgnoreCase) || 
                     ex.Message.Contains("permission", StringComparison.OrdinalIgnoreCase))
            {
                result.Message = $"Access denied to Windows Service '{serviceName}' on computer '{machineName}'. " +
                                "The application may need administrator privileges or service read permissions.";
            }
            else
            {
                result.Message = $"Windows Service '{serviceName}' is not accessible on computer '{machineName}': {ex.Message}";
            }
            
            result.Exception = ex.ToString();
            result.ResponseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            // Добавляем полезную информацию в метаданные
            var errorMetadata = new Dictionary<string, object>
            {
                ["RequestedServiceName"] = serviceName,
                ["MachineName"] = machineName,
                ["ErrorType"] = "ServiceNotFound",
                ["Suggestion"] = "Verify service name using 'sc query <ServiceName>' or 'Get-Service | Where-Object {$_.Name -like '*XLab*'}' PowerShell command"
            };
            
            if (similarServices != null && similarServices.Count > 0)
            {
                errorMetadata["SimilarServices"] = similarServices;
            }
            
            result.Metadata = JsonSerializer.Serialize(errorMetadata);
            
            _logger.LogError(ex, 
                "Windows Service '{ServiceName}' not found or not accessible on computer '{MachineName}' for service {ServiceId}. " +
                "Similar services: {SimilarServices}", 
                serviceName, machineName, service.Id, 
                similarServices != null ? string.Join(", ", similarServices) : "none");
        }
        catch (Exception ex)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = ex.Message;
            result.Exception = ex.ToString();
            result.ResponseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Error checking Windows Service health for service {ServiceId}", service.Id);
        }

        return result;
    }

    private static HealthStatus MapServiceStatus(ServiceControllerStatus status)
    {
        return status switch
        {
            ServiceControllerStatus.Running => HealthStatus.Healthy,
            ServiceControllerStatus.Stopped => HealthStatus.Unhealthy,
            ServiceControllerStatus.Paused => HealthStatus.Degraded,
            ServiceControllerStatus.StartPending => HealthStatus.Degraded,
            ServiceControllerStatus.StopPending => HealthStatus.Degraded,
            ServiceControllerStatus.ContinuePending => HealthStatus.Degraded,
            ServiceControllerStatus.PausePending => HealthStatus.Degraded,
            _ => HealthStatus.Unknown
        };
    }

    private class WindowsServiceConfig
    {
        public string? ServiceName { get; set; }
        public string? MachineName { get; set; }
        public bool CheckStartType { get; set; }
        public string? ExpectedStartType { get; set; }
    }
}

