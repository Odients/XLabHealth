using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XLabStatusService.Application.DTOs;
using XLabStatusService.Core.Interfaces;

namespace XLabStatusService.Api.Controllers;

/// <summary>
/// Контроллер для управления режимом обслуживания
/// </summary>
[ApiController]
[Route("api/maintenance")]
[Authorize(Roles = "Admin")]
public class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceModeRepository _maintenanceModeRepository;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(
        IMaintenanceModeRepository maintenanceModeRepository,
        ILogger<MaintenanceController> logger)
    {
        _maintenanceModeRepository = maintenanceModeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Получить текущий статус режима обслуживания
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<ActionResult<MaintenanceModeDto>> GetStatus(CancellationToken cancellationToken)
    {
        var maintenanceMode = await _maintenanceModeRepository.GetCurrentAsync(cancellationToken);
        
        if (maintenanceMode == null)
        {
            return Ok(new MaintenanceModeDto
            {
                IsEnabled = false
            });
        }

        var dto = new MaintenanceModeDto
        {
            Id = maintenanceMode.Id,
            IsEnabled = maintenanceMode.IsEnabled,
            Message = maintenanceMode.Message,
            ScheduledStartTime = maintenanceMode.ScheduledStartTime,
            ScheduledEndTime = maintenanceMode.ScheduledEndTime,
            StartedAt = maintenanceMode.StartedAt,
            EndedAt = maintenanceMode.EndedAt,
            StartedByUserId = maintenanceMode.StartedByUserId,
            EndedByUserId = maintenanceMode.EndedByUserId,
            CreatedAt = maintenanceMode.CreatedAt,
            UpdatedAt = maintenanceMode.UpdatedAt
        };

        return Ok(dto);
    }

    /// <summary>
    /// Включить режим обслуживания
    /// </summary>
    [HttpPost("enable")]
    public async Task<ActionResult<MaintenanceModeDto>> Enable(
        [FromBody] MaintenanceModeEnableDto dto,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        var maintenanceMode = new Core.Entities.MaintenanceMode
        {
            IsEnabled = true,
            Message = dto.Message ?? "Система находится в режиме обслуживания. Пожалуйста, попробуйте позже.",
            ScheduledStartTime = dto.ScheduledStartTime,
            ScheduledEndTime = dto.ScheduledEndTime,
            StartedAt = dto.ScheduledStartTime.HasValue && dto.ScheduledStartTime.Value > now ? null : now,
            StartedByUserId = userId
        };

        var result = await _maintenanceModeRepository.CreateOrUpdateAsync(maintenanceMode, cancellationToken);

        _logger.LogInformation("Maintenance mode enabled by user {UserId}", userId);

        var response = new MaintenanceModeDto
        {
            Id = result.Id,
            IsEnabled = result.IsEnabled,
            Message = result.Message,
            ScheduledStartTime = result.ScheduledStartTime,
            ScheduledEndTime = result.ScheduledEndTime,
            StartedAt = result.StartedAt,
            StartedByUserId = result.StartedByUserId,
            CreatedAt = result.CreatedAt,
            UpdatedAt = result.UpdatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Выключить режим обслуживания
    /// </summary>
    [HttpPost("disable")]
    public async Task<ActionResult<MaintenanceModeDto>> Disable(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var current = await _maintenanceModeRepository.GetCurrentAsync(cancellationToken);

        if (current == null)
        {
            return NotFound(new { error = "Maintenance mode not found" });
        }

        current.IsEnabled = false;
        current.EndedAt = DateTime.UtcNow;
        current.EndedByUserId = userId;

        var result = await _maintenanceModeRepository.CreateOrUpdateAsync(current, cancellationToken);

        _logger.LogInformation("Maintenance mode disabled by user {UserId}", userId);

        var response = new MaintenanceModeDto
        {
            Id = result.Id,
            IsEnabled = result.IsEnabled,
            Message = result.Message,
            ScheduledStartTime = result.ScheduledStartTime,
            ScheduledEndTime = result.ScheduledEndTime,
            StartedAt = result.StartedAt,
            EndedAt = result.EndedAt,
            StartedByUserId = result.StartedByUserId,
            EndedByUserId = result.EndedByUserId,
            CreatedAt = result.CreatedAt,
            UpdatedAt = result.UpdatedAt
        };

        return Ok(response);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}

