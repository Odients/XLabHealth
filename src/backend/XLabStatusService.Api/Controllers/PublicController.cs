using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using XLabStatusService.Api.Middleware;
using XLabStatusService.Application.DTOs;
using XLabStatusService.Core.Interfaces;
using XLabStatusService.Core.Enums;

namespace XLabStatusService.Api.Controllers;

/// <summary>
/// Публичные endpoints (без авторизации)
/// </summary>
[ApiController]
[Route("api/public")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.PublicPolicy)]
public class PublicController : ControllerBase
{
    private readonly IServiceRepository _serviceRepository;
    private readonly IHealthCheckResultRepository _resultRepository;
    private readonly IBlockedIpRepository _blockedIpRepository;

    public PublicController(
        IServiceRepository serviceRepository,
        IHealthCheckResultRepository resultRepository,
        IBlockedIpRepository blockedIpRepository)
    {
        _serviceRepository = serviceRepository;
        _resultRepository = resultRepository;
        _blockedIpRepository = blockedIpRepository;
    }

    /// <summary>
    /// Получить общий статус системы.
    /// Логика: если хотя бы один критический сервис не работает — вся система не работает (Unhealthy).
    /// Если не работают только некритичные сервисы — система ограниченно функционирует (Degraded).
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<PublicStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var publicServices = await _serviceRepository.GetPublicServicesAsync(cancellationToken);
        var servicesList = publicServices.ToList();

        var healthyCount = 0;
        var degradedCount = 0;
        var unhealthyCount = 0;
        DateTime? lastUpdated = null;

        var hasUnhealthyCritical = false;
        var hasDegradedCritical = false;
        var hasUnhealthyOrDegradedNonCritical = false;

        foreach (var service in servicesList)
        {
            var latestResult = await _resultRepository.GetLatestByServiceIdAsync(service.Id, cancellationToken);
            var status = latestResult?.Status ?? HealthStatus.Unknown;

            if (latestResult != null && latestResult.CheckedAt > (lastUpdated ?? DateTime.MinValue))
            {
                lastUpdated = latestResult.CheckedAt;
            }

            switch (status)
            {
                case HealthStatus.Healthy:
                    healthyCount++;
                    break;
                case HealthStatus.Degraded:
                    degradedCount++;
                    if (service.IsCritical)
                        hasDegradedCritical = true;
                    else
                        hasUnhealthyOrDegradedNonCritical = true;
                    break;
                case HealthStatus.Unhealthy:
                    unhealthyCount++;
                    if (service.IsCritical)
                        hasUnhealthyCritical = true;
                    else
                        hasUnhealthyOrDegradedNonCritical = true;
                    break;
                case HealthStatus.Unknown:
                    if (service.IsCritical)
                        hasUnhealthyCritical = true; // Unknown для критического = система не работает
                    else
                        hasUnhealthyOrDegradedNonCritical = true;
                    break;
            }
        }

        var totalServices = servicesList.Count;
        var availabilityPercentage = totalServices > 0
            ? (double)(healthyCount + degradedCount * 0.5) / totalServices * 100
            : 100;

        // Новая логика с учётом критичности
        var overallStatus = hasUnhealthyCritical
            ? HealthStatus.Unhealthy  // Критический сервис не работает — вся система не работает
            : hasDegradedCritical
                ? HealthStatus.Degraded  // Критический сервис деградирован — ограниченная работа
                : hasUnhealthyOrDegradedNonCritical
                    ? HealthStatus.Degraded  // Только некритичные не работают — ограниченное функционирование
                    : HealthStatus.Healthy;

        return Ok(new PublicStatusDto
        {
            Status = overallStatus,
            TotalServices = totalServices,
            HealthyServices = healthyCount,
            DegradedServices = degradedCount,
            UnhealthyServices = unhealthyCount,
            LastUpdated = lastUpdated,
            AvailabilityPercentage = availabilityPercentage
        });
    }

    /// <summary>
    /// Получить список публичных сервисов
    /// </summary>
    [HttpGet("services")]
    public async Task<ActionResult<IEnumerable<PublicServiceDto>>> GetServices(CancellationToken cancellationToken)
    {
        var services = await _serviceRepository.GetPublicServicesAsync(cancellationToken);
        var result = new List<PublicServiceDto>();

        foreach (var service in services)
        {
            var latestResult = await _resultRepository.GetLatestByServiceIdAsync(service.Id, cancellationToken);
            result.Add(new PublicServiceDto
            {
                Id = service.Id,
                Name = service.Name,
                Status = latestResult?.Status ?? HealthStatus.Unknown,
                IsCritical = service.IsCritical,
                LastCheckedAt = latestResult?.CheckedAt
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Получить информацию о публичном сервисе
    /// </summary>
    [HttpGet("services/{id}")]
    public async Task<ActionResult<PublicServiceDto>> GetService(Guid id, CancellationToken cancellationToken)
    {
        var service = await _serviceRepository.GetByIdAsync(id, cancellationToken);
        if (service == null || !service.IsPublic)
        {
            return NotFound();
        }

        var latestResult = await _resultRepository.GetLatestByServiceIdAsync(id, cancellationToken);
        return Ok(new PublicServiceDto
        {
            Id = service.Id,
            Name = service.Name,
            Status = latestResult?.Status ?? HealthStatus.Unknown,
            IsCritical = service.IsCritical,
            LastCheckedAt = latestResult?.CheckedAt
        });
    }

    /// <summary>
    /// Получить общую сводку
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<PublicStatusDto>> GetSummary(CancellationToken cancellationToken)
    {
        return await GetStatus(cancellationToken);
    }

    /// <summary>
    /// Проверить статус IP-адреса (заблокирован ли)
    /// </summary>
    /// <param name="ipAddress">IP-адрес для проверки (опционально — при отсутствии используется IP соединения)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    [HttpGet("ip-status")]
    public async Task<ActionResult<IpStatusDto>> GetIpStatus(
        [FromQuery] string? ipAddress,
        CancellationToken cancellationToken)
    {
        // Если IP не передан, используем IP из соединения (тот, что видит сервер)
        var ipToCheck = !string.IsNullOrWhiteSpace(ipAddress)
            ? ipAddress
            : GetClientIpAddress();

        if (string.IsNullOrWhiteSpace(ipToCheck))
        {
            return Ok(new IpStatusDto
            {
                IpAddress = null,
                IsBlocked = false,
                BlockedDate = null
            });
        }

        var blockedIp = await _blockedIpRepository.GetByIpAddressAsync(ipToCheck, cancellationToken);
        var isBlocked = blockedIp != null;

        return Ok(new IpStatusDto
        {
            IpAddress = blockedIp?.IpAddress ?? ipToCheck,
            IsBlocked = isBlocked,
            BlockedDate = blockedIp?.Date
        });
    }

    /// <summary>
    /// Получить IP-адрес клиента с учётом прокси
    /// </summary>
    private string? GetClientIpAddress()
    {
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var ip = forwardedFor.Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(ip))
            {
                return ip;
            }
        }

        var realIp = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
        {
            return realIp;
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}

