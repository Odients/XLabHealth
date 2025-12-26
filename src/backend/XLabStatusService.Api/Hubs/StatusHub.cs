using Microsoft.AspNetCore.SignalR;
using XLabStatusService.Application.DTOs;

namespace XLabStatusService.Api.Hubs;

/// <summary>
/// SignalR Hub для real-time обновлений статусов сервисов
/// </summary>
public class StatusHub : Hub
{
    public async Task SubscribeToService(Guid serviceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"service-{serviceId}");
    }

    public async Task SubscribeToAllServices()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all-services");
    }

    public async Task UnsubscribeFromService(Guid serviceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"service-{serviceId}");
    }
}

