using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace XLabStatusService.Api.Middleware;

/// <summary>
/// Middleware для применения rate limiting к запросам
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
    }
}

/// <summary>
/// Политика rate limiting для публичных endpoints
/// </summary>
public static class RateLimitPolicies
{
    public const string PublicPolicy = "PublicPolicy";
    public const string AuthPolicy = "AuthPolicy";
}

/// <summary>
/// Расширения для настройки rate limiting
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Получить IP-адрес клиента с учетом прокси-серверов
    /// Приоритет: X-Client-Ip (от фронтенда) > X-Forwarded-For > X-Real-IP > RemoteIpAddress
    /// </summary>
    public static string GetClientIpAddress(HttpContext context)
    {
        // В первую очередь проверяем заголовок X-Client-Ip, который передает фронтенд
        // Это позволяет получить реальный IP клиента, когда фронтенд находится на серверах провайдера
        var clientIp = context.Request.Headers["X-Client-Ip"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(clientIp))
        {
            return clientIp.Trim();
        }

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
            return realIp.Trim();
        }

        // Используем RemoteIpAddress как последний вариант
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Настроить rate limiting для приложения
    /// </summary>
    public static IServiceCollection AddRateLimitingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rateLimitingSection = configuration.GetSection("RateLimiting");

        services.AddRateLimiter(options =>
        {
            // Политика для публичных endpoints
            var publicConfig = rateLimitingSection.GetSection("PublicEndpoints");
            options.AddFixedWindowLimiter(RateLimitPolicies.PublicPolicy, limiterOptions =>
            {
                limiterOptions.PermitLimit = publicConfig.GetValue<int>("PermitLimit", 100);
                limiterOptions.Window = TimeSpan.Parse(publicConfig.GetValue<string>("Window") ?? "00:01:00");
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = publicConfig.GetValue<int>("QueueLimit", 0);
                limiterOptions.AutoReplenishment = publicConfig.GetValue<bool>("AutoReplenishment", true);
            });

            // Политика для endpoints аутентификации (более строгая)
            var authConfig = rateLimitingSection.GetSection("AuthEndpoints");
            options.AddFixedWindowLimiter(RateLimitPolicies.AuthPolicy, limiterOptions =>
            {
                limiterOptions.PermitLimit = authConfig.GetValue<int>("PermitLimit", 5);
                limiterOptions.Window = TimeSpan.Parse(authConfig.GetValue<string>("Window") ?? "00:01:00");
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = authConfig.GetValue<int>("QueueLimit", 0);
                limiterOptions.AutoReplenishment = authConfig.GetValue<bool>("AutoReplenishment", true);
            });

            // Глобальная политика по умолчанию (для всех endpoints)
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Используем IP-адрес клиента как ключ для разделения лимитов
                var ipAddress = GetClientIpAddress(context);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ipAddress,
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 200,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    });
            });

            // Обработчик при превышении лимита
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var response = new
                {
                    error = "TooManyRequests",
                    message = "Rate limit exceeded. Please try again later."
                };

                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await context.HttpContext.Response.WriteAsync(json, cancellationToken);
            };
        });

        return services;
    }
}

