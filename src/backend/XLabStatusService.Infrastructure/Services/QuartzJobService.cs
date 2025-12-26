using Microsoft.Extensions.Logging;
using Quartz;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Interfaces;
using XLabStatusService.Infrastructure.Jobs;

namespace XLabStatusService.Infrastructure.Services;

/// <summary>
/// Сервис для управления Quartz.NET Jobs
/// </summary>
public class QuartzJobService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<QuartzJobService> _logger;

    public QuartzJobService(ISchedulerFactory schedulerFactory, ILogger<QuartzJobService> logger)
    {
        _schedulerFactory = schedulerFactory;
        _logger = logger;
    }

    public async Task CreateOrUpdateJobAsync(Service service, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var jobKey = new JobKey($"HealthCheck_{service.Id}", "Services");
        var triggerKey = new TriggerKey($"Trigger_{service.Id}", "Services");

        // Проверяем, существует ли Job и Trigger
        var jobExists = await scheduler.CheckExists(jobKey, cancellationToken);
        var triggerExists = await scheduler.CheckExists(triggerKey, cancellationToken);

        var job = JobBuilder.Create<HealthCheckJob>()
            .WithIdentity(jobKey)
            .UsingJobData("ServiceId", service.Id.ToString())
            .StoreDurably()
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(jobKey)
            .StartNow()
            .WithSimpleSchedule(x => x
                .WithIntervalInSeconds(service.CheckInterval)
                .RepeatForever())
            .Build();

        if (jobExists)
        {
            // Обновляем job
            await scheduler.AddJob(job, replace: true, cancellationToken);
            
            // Обновляем или создаем trigger
            if (triggerExists)
            {
                await scheduler.RescheduleJob(triggerKey, trigger, cancellationToken);
            }
            else
            {
                await scheduler.ScheduleJob(trigger, cancellationToken);
            }
            
            _logger.LogInformation("Updated Quartz job for service {ServiceId}", service.Id);
        }
        else
        {
            // Создаем новый job и trigger
            await scheduler.ScheduleJob(job, trigger, cancellationToken);
            _logger.LogInformation("Created Quartz job for service {ServiceId}", service.Id);
        }
    }

    public async Task DeleteJobAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var jobKey = new JobKey($"HealthCheck_{serviceId}", "Services");

        var deleted = await scheduler.DeleteJob(jobKey, cancellationToken);
        if (deleted)
        {
            _logger.LogInformation("Deleted Quartz job for service {ServiceId}", serviceId);
        }
    }

    public async Task PauseJobAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var jobKey = new JobKey($"HealthCheck_{serviceId}", "Services");

        await scheduler.PauseJob(jobKey, cancellationToken);
        _logger.LogInformation("Paused Quartz job for service {ServiceId}", serviceId);
    }

    public async Task ResumeJobAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var jobKey = new JobKey($"HealthCheck_{serviceId}", "Services");

        await scheduler.ResumeJob(jobKey, cancellationToken);
        _logger.LogInformation("Resumed Quartz job for service {ServiceId}", serviceId);
    }
}

