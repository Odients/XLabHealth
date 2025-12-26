using Microsoft.Extensions.Logging;
using Quartz;
using XLabStatusService.Infrastructure.Services;

namespace XLabStatusService.Infrastructure.Jobs;

/// <summary>
/// Job для периодической проверки здоровья сервиса
/// </summary>
[DisallowConcurrentExecution]
public class HealthCheckJob : IJob
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthCheckJob> _logger;

    public HealthCheckJob(
        HealthCheckService healthCheckService,
        ILogger<HealthCheckJob> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        // При UseProperties = true все значения хранятся как строки
        var serviceIdString = context.JobDetail.JobDataMap.GetString("ServiceId");
        if (string.IsNullOrEmpty(serviceIdString) || !Guid.TryParse(serviceIdString, out var serviceId))
        {
            var errorMessage = $"Invalid or missing ServiceId in job data map for job {context.JobDetail.Key}";
            _logger.LogError(errorMessage);
            throw new JobExecutionException(new InvalidOperationException(errorMessage), refireImmediately: false);
        }
        
        _logger.LogInformation("Executing health check job for service {ServiceId}", serviceId);

        try
        {
            await _healthCheckService.CheckServiceHealthAsync(serviceId, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing health check job for service {ServiceId}", serviceId);
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }
}

