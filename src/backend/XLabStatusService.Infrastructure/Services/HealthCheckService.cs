using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Enums;
using XLabStatusService.Core.Interfaces;

namespace XLabStatusService.Infrastructure.Services;

/// <summary>
/// Сервис для выполнения проверок здоровья сервисов
/// </summary>
public class HealthCheckService
{
    private readonly IServiceRepository _serviceRepository;
    private readonly IHealthCheckResultRepository _resultRepository;
    private readonly INotificationService? _notificationService;
    private readonly Dictionary<ServiceType, IHealthCheckProvider> _providers;
    private readonly ILogger<HealthCheckService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public HealthCheckService(
        IServiceRepository serviceRepository,
        IHealthCheckResultRepository resultRepository,
        IEnumerable<IHealthCheckProvider> providers,
        ILogger<HealthCheckService> logger,
        IServiceScopeFactory scopeFactory,
        INotificationService? notificationService = null)
    {
        _serviceRepository = serviceRepository;
        _resultRepository = resultRepository;
        _notificationService = notificationService;
        _logger = logger;
        _scopeFactory = scopeFactory;

        // Регистрируем провайдеры по типам
        _providers = new Dictionary<ServiceType, IHealthCheckProvider>();
        foreach (var provider in providers)
        {
            var providerType = provider.GetType();
            if (providerType.Name.Contains("Http"))
                _providers[ServiceType.Http] = provider;
            else if (providerType.Name.Contains("Database"))
                _providers[ServiceType.Database] = provider;
            else if (providerType.Name.Contains("Redis"))
                _providers[ServiceType.Redis] = provider;
            else if (providerType.Name.Contains("Kafka"))
                _providers[ServiceType.Kafka] = provider;
            else if (providerType.Name.Contains("WindowsService"))
                _providers[ServiceType.WindowsService] = provider;
        }
    }

    public async Task<HealthCheckResult> CheckServiceHealthAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var service = await _serviceRepository.GetByIdAsync(serviceId, cancellationToken);
        if (service == null)
        {
            throw new InvalidOperationException($"Service with id {serviceId} not found");
        }

        if (!service.IsEnabled)
        {
            _logger.LogInformation("Service {ServiceId} is disabled, skipping health check", serviceId);
            return new HealthCheckResult
            {
                Id = Guid.NewGuid(),
                ServiceId = serviceId,
                Status = HealthStatus.Unknown,
                Message = "Service is disabled",
                CheckedAt = DateTime.UtcNow
            };
        }

        if (!_providers.TryGetValue(service.Type, out var provider))
        {
            _logger.LogWarning("No health check provider found for service type {ServiceType}", service.Type);
            return new HealthCheckResult
            {
                Id = Guid.NewGuid(),
                ServiceId = serviceId,
                Status = HealthStatus.Unknown,
                Message = $"No provider available for service type {service.Type}",
                CheckedAt = DateTime.UtcNow
            };
        }

        try
        {
            var result = await provider.CheckHealthAsync(service, cancellationToken);
            
            // Получаем предыдущий результат для сравнения
            var previousResult = await _resultRepository.GetLatestByServiceIdAsync(serviceId, cancellationToken);
            var statusChanged = previousResult == null || previousResult.Status != result.Status;

            await _resultRepository.CreateAsync(result, cancellationToken);

            // Отправляем уведомление через SignalR, если статус изменился
            if (statusChanged && _notificationService != null)
            {
                await _notificationService.NotifyServiceStatusChangedAsync(serviceId, result.Status, result, cancellationToken);
            }

            // Всегда отправляем уведомление о проверке
            if (_notificationService != null)
            {
                await _notificationService.NotifyServiceCheckedAsync(serviceId, result, cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health check for service {ServiceId}", serviceId);
            var errorResult = new HealthCheckResult
            {
                Id = Guid.NewGuid(),
                ServiceId = serviceId,
                Status = HealthStatus.Unhealthy,
                Message = ex.Message,
                Exception = ex.ToString(),
                CheckedAt = DateTime.UtcNow
            };
            await _resultRepository.CreateAsync(errorResult, cancellationToken);
            return errorResult;
        }
    }

    public async Task CheckAllEnabledServicesAsync(CancellationToken cancellationToken = default)
    {
        // Получаем список сервисов в текущем контексте
        var services = await _serviceRepository.GetEnabledServicesAsync(cancellationToken);
        
        // Создаем задачи для параллельной обработки, каждая в своем scope
        var tasks = services.Select(service => CheckServiceHealthInScopeAsync(service.Id, cancellationToken));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Выполняет проверку здоровья сервиса в отдельном scope для обеспечения thread-safety
    /// </summary>
    private async Task CheckServiceHealthInScopeAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var serviceRepository = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        var resultRepository = scope.ServiceProvider.GetRequiredService<IHealthCheckResultRepository>();
        var providers = scope.ServiceProvider.GetServices<IHealthCheckProvider>();
        var notificationService = scope.ServiceProvider.GetService<INotificationService>();

        // Создаем словарь провайдеров для этого scope
        var scopeProviders = new Dictionary<ServiceType, IHealthCheckProvider>();
        foreach (var providerItem in providers)
        {
            var providerType = providerItem.GetType();
            if (providerType.Name.Contains("Http"))
                scopeProviders[ServiceType.Http] = providerItem;
            else if (providerType.Name.Contains("Database"))
                scopeProviders[ServiceType.Database] = providerItem;
            else if (providerType.Name.Contains("Redis"))
                scopeProviders[ServiceType.Redis] = providerItem;
            else if (providerType.Name.Contains("Kafka"))
                scopeProviders[ServiceType.Kafka] = providerItem;
            else if (providerType.Name.Contains("WindowsService"))
                scopeProviders[ServiceType.WindowsService] = providerItem;
        }

        var service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken);
        if (service == null)
        {
            _logger.LogWarning("Service with id {ServiceId} not found", serviceId);
            return;
        }

        if (!service.IsEnabled)
        {
            _logger.LogInformation("Service {ServiceId} is disabled, skipping health check", serviceId);
            return;
        }

        if (!scopeProviders.TryGetValue(service.Type, out var provider))
        {
            _logger.LogWarning("No health check provider found for service type {ServiceType}", service.Type);
            return;
        }

        try
        {
            var result = await provider.CheckHealthAsync(service, cancellationToken);
            
            // Получаем предыдущий результат для сравнения
            var previousResult = await resultRepository.GetLatestByServiceIdAsync(serviceId, cancellationToken);
            var statusChanged = previousResult == null || previousResult.Status != result.Status;

            await resultRepository.CreateAsync(result, cancellationToken);

            // Отправляем уведомление через SignalR, если статус изменился
            if (statusChanged && notificationService != null)
            {
                await notificationService.NotifyServiceStatusChangedAsync(serviceId, result.Status, result, cancellationToken);
            }

            // Всегда отправляем уведомление о проверке
            if (notificationService != null)
            {
                await notificationService.NotifyServiceCheckedAsync(serviceId, result, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health check for service {ServiceId}", serviceId);
            var errorResult = new HealthCheckResult
            {
                Id = Guid.NewGuid(),
                ServiceId = serviceId,
                Status = HealthStatus.Unhealthy,
                Message = ex.Message,
                Exception = ex.ToString(),
                CheckedAt = DateTime.UtcNow
            };
            await resultRepository.CreateAsync(errorResult, cancellationToken);
        }
    }
}

