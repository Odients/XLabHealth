using System.Net;
using System.Text.Json;
using XLabStatusService.Core.Interfaces;

namespace XLabStatusService.Api.Middleware;

/// <summary>
/// Middleware для проверки заблокированных IP-адресов
/// </summary>
public class IpBlockingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IpBlockingMiddleware> _logger;

    public IpBlockingMiddleware(RequestDelegate next, ILogger<IpBlockingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IBlockedIpRepository blockedIpRepository)
    {
        // Получаем IP-адрес клиента
        var ipAddress = GetClientIpAddress(context);

        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            // Проверяем, заблокирован ли IP
            var isBlocked = await blockedIpRepository.IsBlockedAsync(ipAddress, context.RequestAborted);

            if (isBlocked)
            {
                _logger.LogWarning("Blocked IP address attempted to access: {IpAddress} from {Path}", 
                    ipAddress, context.Request.Path);

                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    error = "Forbidden",
                    message = "Your IP address has been blocked."
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

    /// <summary>
    /// Получить IP-адрес клиента с учетом прокси-серверов
    /// </summary>
    private static string? GetClientIpAddress(HttpContext context)
    {
        // Проверяем заголовок X-Forwarded-For (для прокси/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            // X-Forwarded-For может содержать несколько IP, берем первый
            var ip = forwardedFor.Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(ip))
            {
                return ip;
            }
        }

        // Проверяем заголовок X-Real-IP
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
        {
            return realIp;
        }

        // Используем RemoteIpAddress как последний вариант
        return context.Connection.RemoteIpAddress?.ToString();
    }
}

