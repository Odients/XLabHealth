using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using XLabStatusService.Api.Hubs;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Enums;

using XLabStatusService.Core.Interfaces;

namespace XLabStatusService.Api.Services;

/// <summary>
/// Сервис для отправки уведомлений через SignalR
/// </summary>
public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<StatusHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(
        IHubContext<StatusHub> hubContext,
        ILogger<SignalRNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyServiceStatusChangedAsync(
        Guid serviceId,
        HealthStatus status,
        HealthCheckResult? result = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients
                .Group($"service-{serviceId}")
                .SendAsync("ServiceStatusChanged", new
                {
                    ServiceId = serviceId,
                    Status = status.ToString(),
                    CheckedAt = result?.CheckedAt ?? DateTime.UtcNow,
                    ResponseTime = result?.ResponseTime,
                    Message = result?.Message
                }, cancellationToken);

            // Также отправляем в группу всех сервисов
            await _hubContext.Clients
                .Group("all-services")
                .SendAsync("ServiceStatusChanged", new
                {
                    ServiceId = serviceId,
                    Status = status.ToString(),
                    CheckedAt = result?.CheckedAt ?? DateTime.UtcNow
                }, cancellationToken);

            _logger.LogDebug("Sent SignalR notification for service {ServiceId} status change", serviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SignalR notification for service {ServiceId}", serviceId);
        }
    }

    public async Task NotifyServiceCheckedAsync(
        Guid serviceId,
        HealthCheckResult result,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients
                .Group($"service-{serviceId}")
                .SendAsync("ServiceChecked", new
                {
                    ServiceId = serviceId,
                    Status = result.Status.ToString(),
                    CheckedAt = result.CheckedAt,
                    ResponseTime = result.ResponseTime,
                    Message = result.Message
                }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SignalR notification for service check {ServiceId}", serviceId);
        }
    }
}

