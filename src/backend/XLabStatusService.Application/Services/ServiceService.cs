using AutoMapper;
using Microsoft.Extensions.Logging;
using XLabStatusService.Application.DTOs;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Interfaces;

namespace XLabStatusService.Application.Services;

/// <summary>
/// Сервис для управления сервисами
/// </summary>
public class ServiceService
{
    private readonly IServiceRepository _serviceRepository;
    private readonly IHealthCheckResultRepository _resultRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ServiceService> _logger;

    public ServiceService(
        IServiceRepository serviceRepository,
        IHealthCheckResultRepository resultRepository,
        IMapper mapper,
        ILogger<ServiceService> logger)
    {
        _serviceRepository = serviceRepository;
        _resultRepository = resultRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Получить все сервисы
    /// </summary>
    public async Task<IEnumerable<ServiceDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var services = await _serviceRepository.GetAllAsync(cancellationToken);
        var result = new List<ServiceDto>();

        foreach (var service in services)
        {
            var latestResult = await _resultRepository.GetLatestByServiceIdAsync(service.Id, cancellationToken);
            var serviceDto = _mapper.Map<ServiceDto>(service);
            serviceDto.LastStatus = latestResult?.Status;
            serviceDto.LastCheckedAt = latestResult?.CheckedAt;
            result.Add(serviceDto);
        }

        return result;
    }

    /// <summary>
    /// Получить сервис по ID
    /// </summary>
    public async Task<ServiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await _serviceRepository.GetByIdAsync(id, cancellationToken);
        if (service == null)
        {
            return null;
        }

        var latestResult = await _resultRepository.GetLatestByServiceIdAsync(id, cancellationToken);
        var serviceDto = _mapper.Map<ServiceDto>(service);
        serviceDto.LastStatus = latestResult?.Status;
        serviceDto.LastCheckedAt = latestResult?.CheckedAt;

        return serviceDto;
    }

    /// <summary>
    /// Создать новый сервис
    /// </summary>
    public async Task<ServiceDto> CreateAsync(ServiceCreateDto dto, CancellationToken cancellationToken = default)
    {
        var service = _mapper.Map<Service>(dto);
        service.Id = Guid.NewGuid();

        // Создаем конфигурацию, если она указана
        if (dto.Configuration != null)
        {
            service.Configuration = _mapper.Map<ServiceConfiguration>(dto.Configuration);
            service.Configuration.Id = Guid.NewGuid();
            service.Configuration.ServiceId = service.Id;
        }

        var createdService = await _serviceRepository.CreateAsync(service, cancellationToken);
        _logger.LogInformation("Service created: {ServiceName} ({ServiceId})", createdService.Name, createdService.Id);

        var serviceDto = _mapper.Map<ServiceDto>(createdService);
        return serviceDto;
    }

    /// <summary>
    /// Обновить сервис
    /// </summary>
    public async Task<ServiceDto> UpdateAsync(Guid id, ServiceUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var service = await _serviceRepository.GetByIdAsync(id, cancellationToken);
        if (service == null)
        {
            throw new KeyNotFoundException($"Service with ID '{id}' not found");
        }

        // Обновляем только те поля, которые были переданы (частичное обновление)
        if (dto.Name != null)
        {
            service.Name = dto.Name;
        }
        if (dto.Description != null)
        {
            service.Description = dto.Description;
        }
        if (dto.Url != null)
        {
            service.Url = dto.Url;
        }
        if (dto.Type.HasValue)
        {
            service.Type = dto.Type.Value;
        }
        if (dto.CheckInterval.HasValue)
        {
            service.CheckInterval = dto.CheckInterval.Value;
        }
        if (dto.Timeout.HasValue)
        {
            service.Timeout = dto.Timeout.Value;
        }
        if (dto.RetryCount.HasValue)
        {
            service.RetryCount = dto.RetryCount.Value;
        }
        if (dto.IsEnabled.HasValue)
        {
            service.IsEnabled = dto.IsEnabled.Value;
        }
        if (dto.IsPublic.HasValue)
        {
            service.IsPublic = dto.IsPublic.Value;
        }
        if (dto.IsCritical.HasValue)
        {
            service.IsCritical = dto.IsCritical.Value;
        }

        // Обновляем конфигурацию (частичное обновление)
        if (dto.Configuration != null)
        {
            if (service.Configuration == null)
            {
                // Создаем новую конфигурацию
                service.Configuration = new ServiceConfiguration
                {
                    Id = Guid.NewGuid(),
                    ServiceId = service.Id,
                    CheckType = dto.Configuration.CheckType ?? string.Empty,
                    Parameters = dto.Configuration.Parameters,
                    Headers = dto.Configuration.Headers,
                    ExpectedStatusCode = dto.Configuration.ExpectedStatusCode,
                    ExpectedResponse = dto.Configuration.ExpectedResponse
                };
            }
            else
            {
                // Обновляем только переданные поля конфигурации
                // Используем null-coalescing для строк, чтобы не перезаписывать существующие значения пустыми строками
                if (!string.IsNullOrEmpty(dto.Configuration.CheckType))
                {
                    service.Configuration.CheckType = dto.Configuration.CheckType;
                }
                if (dto.Configuration.Parameters != null)
                {
                    service.Configuration.Parameters = dto.Configuration.Parameters;
                }
                if (dto.Configuration.Headers != null)
                {
                    service.Configuration.Headers = dto.Configuration.Headers;
                }
                if (dto.Configuration.ExpectedStatusCode.HasValue)
                {
                    service.Configuration.ExpectedStatusCode = dto.Configuration.ExpectedStatusCode;
                }
                if (dto.Configuration.ExpectedResponse != null)
                {
                    service.Configuration.ExpectedResponse = dto.Configuration.ExpectedResponse;
                }
            }
        }

        var updatedService = await _serviceRepository.UpdateAsync(service, cancellationToken);
        _logger.LogInformation("Service updated: {ServiceName} ({ServiceId})", updatedService.Name, updatedService.Id);
        
        // Маппим в DTO с конфигурацией и последним статусом
        var serviceDto = _mapper.Map<ServiceDto>(updatedService);

        // Добавляем LastStatus и LastCheckedAt из результата проверки
        var latestResult = await _resultRepository.GetLatestByServiceIdAsync(id, cancellationToken);
        serviceDto.LastStatus = latestResult?.Status;
        serviceDto.LastCheckedAt = latestResult?.CheckedAt;

        return serviceDto;
    }

    /// <summary>
    /// Удалить сервис
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await _serviceRepository.GetByIdAsync(id, cancellationToken);
        if (service == null)
        {
            throw new KeyNotFoundException($"Service with ID '{id}' not found");
        }

        await _serviceRepository.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Service deleted: {ServiceName} ({ServiceId})", service.Name, service.Id);
    }

    /// <summary>
    /// Получить историю проверок сервиса
    /// </summary>
    public async Task<IEnumerable<HealthCheckResultDto>> GetHistoryAsync(
        Guid id,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var service = await _serviceRepository.GetByIdAsync(id, cancellationToken);
        if (service == null)
        {
            throw new KeyNotFoundException($"Service with ID '{id}' not found");
        }

        var from = fromDate ?? DateTime.UtcNow.AddDays(-7);
        var to = toDate ?? DateTime.UtcNow;

        var results = await _resultRepository.GetByServiceIdAndDateRangeAsync(id, from, to, cancellationToken);
        return results.Select(r => new HealthCheckResultDto
        {
            Id = r.Id,
            ServiceId = r.ServiceId,
            ServiceName = service.Name,
            Status = r.Status,
            ResponseTime = r.ResponseTime,
            Message = r.Message,
            Exception = r.Exception,
            CheckedAt = r.CheckedAt,
            Metadata = r.Metadata
        });
    }
}

