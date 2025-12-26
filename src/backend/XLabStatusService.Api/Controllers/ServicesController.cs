using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XLabStatusService.Application.DTOs;
using XLabStatusService.Application.Services;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Interfaces;
using XLabStatusService.Core.Enums;
using XLabStatusService.Infrastructure.Services;

namespace XLabStatusService.Api.Controllers;

/// <summary>
/// Приватные endpoints для управления сервисами (требуется авторизация)
/// </summary>
[ApiController]
[Route("api/services")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly IServiceRepository _serviceRepository;
    private readonly IHealthCheckResultRepository _resultRepository;
    private readonly QuartzJobService _quartzJobService;
    private readonly HealthCheckService _healthCheckService;
    private readonly ServiceService _serviceService;
    private readonly IMapper _mapper;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(
        IServiceRepository serviceRepository,
        IHealthCheckResultRepository resultRepository,
        QuartzJobService quartzJobService,
        HealthCheckService healthCheckService,
        ServiceService serviceService,
        IMapper mapper,
        ILogger<ServicesController> logger)
    {
        _serviceRepository = serviceRepository;
        _resultRepository = resultRepository;
        _quartzJobService = quartzJobService;
        _healthCheckService = healthCheckService;
        _serviceService = serviceService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Получить все сервисы
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Viewer")]
    public async Task<ActionResult<IEnumerable<ServiceDto>>> GetServices(CancellationToken cancellationToken)
    {
        var services = await _serviceService.GetAllAsync(cancellationToken);
        return Ok(services);
    }

    /// <summary>
    /// Получить сервис по ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Viewer")]
    public async Task<ActionResult<ServiceDto>> GetService(Guid id, CancellationToken cancellationToken)
    {
        var service = await _serviceService.GetByIdAsync(id, cancellationToken);
        if (service == null)
        {
            return NotFound();
        }

        return Ok(service);
    }

    /// <summary>
    /// Создать новый сервис
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ServiceDto>> CreateService(
        [FromBody] ServiceCreateDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var serviceDto = await _serviceService.CreateAsync(dto, cancellationToken);

            // Создаем Quartz.NET Job для нового сервиса, если он включен (инфраструктурная логика)
            var service = await _serviceRepository.GetByIdAsync(serviceDto.Id, cancellationToken);
            if (service != null && service.IsEnabled)
            {
                await _quartzJobService.CreateOrUpdateJobAsync(service, cancellationToken);
            }

            return CreatedAtAction(nameof(GetService), new { id = serviceDto.Id }, serviceDto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Обновить сервис
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ServiceDto>> UpdateService(Guid id, [FromBody] ServiceUpdateDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var serviceDto = await _serviceService.UpdateAsync(id, dto, cancellationToken);

            // Обновляем Quartz.NET Job для сервиса (инфраструктурная логика)
            var service = await _serviceRepository.GetByIdAsync(id, cancellationToken);
            if (service != null)
            {
                if (service.IsEnabled)
                {
                    await _quartzJobService.CreateOrUpdateJobAsync(service, cancellationToken);
                }
                else
                {
                    await _quartzJobService.PauseJobAsync(service.Id, cancellationToken);
                }
            }

            return Ok(serviceDto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Удалить сервис
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteService(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            // Удаляем Quartz.NET Job (инфраструктурная логика)
            await _quartzJobService.DeleteJobAsync(id, cancellationToken);

            await _serviceService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить историю проверок сервиса
    /// </summary>
    [HttpGet("{id}/history")]
    [Authorize(Roles = "Admin,Viewer")]
    public async Task<ActionResult<IEnumerable<HealthCheckResultDto>>> GetHistory(
        Guid id,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var results = await _serviceService.GetHistoryAsync(id, fromDate, toDate, cancellationToken);
            return Ok(results);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Запустить принудительную проверку всех сервисов
    /// </summary>
    [HttpPost("check-all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> CheckAllServices(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Manual health check triggered for all services by user");
            await _healthCheckService.CheckAllEnabledServicesAsync(cancellationToken);
            return Ok(new { message = "Проверка всех сервисов запущена" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual health check for all services");
            return StatusCode(500, new { error = "Ошибка при проверке сервисов", message = ex.Message });
        }
    }

    /// <summary>
    /// Запустить принудительную проверку сервиса
    /// </summary>
    [HttpPost("{id}/check")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<HealthCheckResultDto>> CheckService(
        Guid id,
        CancellationToken cancellationToken)
    {
        var service = await _serviceRepository.GetByIdAsync(id, cancellationToken);
        if (service == null)
        {
            return NotFound();
        }

        try
        {
            _logger.LogInformation("Manual health check triggered for service {ServiceId} by user", id);
            var result = await _healthCheckService.CheckServiceHealthAsync(id, cancellationToken);

            var dto = new HealthCheckResultDto
            {
                Id = result.Id,
                ServiceId = result.ServiceId,
                ServiceName = service.Name,
                Status = result.Status,
                ResponseTime = result.ResponseTime,
                Message = result.Message,
                Exception = result.Exception,
                CheckedAt = result.CheckedAt,
                Metadata = result.Metadata
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual health check for service {ServiceId}", id);
            return StatusCode(500, new { error = "Ошибка при проверке сервиса", message = ex.Message });
        }
    }
}

