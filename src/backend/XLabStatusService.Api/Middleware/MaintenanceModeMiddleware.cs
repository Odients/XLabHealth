using System.Net;
using System.Text.Json;
using XLabStatusService.Core.Interfaces;

namespace XLabStatusService.Api.Middleware;

/// <summary>
/// Middleware для проверки режима обслуживания
/// </summary>
public class MaintenanceModeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MaintenanceModeMiddleware> _logger;

    // Пути, которые должны быть доступны даже в режиме обслуживания
    private static readonly string[] AllowedPaths = new[]
    {
        "/api/auth/login",
        "/api/auth/refresh",
        "/api/health",
        "/api/maintenance/status", // Публичный endpoint для проверки статуса
        "/swagger",
        "/hubs/status"
    };

    public MaintenanceModeMiddleware(RequestDelegate next, ILogger<MaintenanceModeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IMaintenanceModeRepository maintenanceModeRepository)
    {
        // Проверяем, включен ли режим обслуживания
        var isMaintenanceEnabled = await maintenanceModeRepository.IsEnabledAsync(context.RequestAborted);

        if (isMaintenanceEnabled)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

            // Проверяем, разрешен ли доступ к этому пути
            var isAllowed = AllowedPaths.Any(allowedPath => path.StartsWith(allowedPath, StringComparison.OrdinalIgnoreCase));

            // Проверяем, является ли пользователь администратором
            var isAdmin = context.User.IsInRole("Admin");

            // Разрешаем доступ к endpoints управления режимом обслуживания только для администраторов
            var isMaintenanceManagementEndpoint = path.StartsWith("/api/maintenance/enable", StringComparison.OrdinalIgnoreCase) ||
                                                  path.StartsWith("/api/maintenance/disable", StringComparison.OrdinalIgnoreCase);
            var canAccessMaintenanceManagement = isMaintenanceManagementEndpoint && isAdmin;

            if (!isAllowed && !canAccessMaintenanceManagement && !isAdmin)
            {
                var maintenanceMode = await maintenanceModeRepository.GetCurrentAsync(context.RequestAborted);
                var message = maintenanceMode?.Message ?? "Система находится в режиме обслуживания. Пожалуйста, попробуйте позже.";

                context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    error = "Service Unavailable",
                    message = message,
                    maintenanceMode = true,
                    scheduledEndTime = maintenanceMode?.ScheduledEndTime
                };

                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await context.Response.WriteAsync(json, context.RequestAborted);
                return;
            }
        }

        await _next(context);
    }
}

