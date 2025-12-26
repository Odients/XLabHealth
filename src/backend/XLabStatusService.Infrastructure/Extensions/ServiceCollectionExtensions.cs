using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using XLabStatusService.Core.Interfaces;
using XLabStatusService.Infrastructure.Data;
using XLabStatusService.Infrastructure.Jobs;
using XLabStatusService.Infrastructure.Repositories;
using XLabStatusService.Infrastructure.Services;
using XLabStatusService.Infrastructure.Services.HealthCheckProviders;

namespace XLabStatusService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // XLab Database (для проверки заблокированных IP)
        services.AddDbContext<XLabDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("XLabConnection")));

        // Repositories
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IHealthCheckResultRepository, HealthCheckResultRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IWebhookRepository, WebhookRepository>();
        services.AddScoped<IMaintenanceModeRepository, MaintenanceModeRepository>();
        services.AddScoped<IBlockedIpRepository, BlockedIpRepository>();
        services.AddScoped<ILoginAttemptRepository, LoginAttemptRepository>();

        // Health Check Providers
        services.AddScoped<IHealthCheckProvider, HttpHealthCheckProvider>();
        services.AddScoped<IHealthCheckProvider, DatabaseHealthCheckProvider>();
        services.AddScoped<IHealthCheckProvider, RedisHealthCheckProvider>();
        services.AddScoped<IHealthCheckProvider, KafkaHealthCheckProvider>();
        
        // Windows Service Provider только на Windows
        if (OperatingSystem.IsWindows())
        {
            services.AddScoped<IHealthCheckProvider, WindowsServiceHealthCheckProvider>();
        }

        // Services
        services.AddScoped<HealthCheckService>();
        services.AddScoped<QuartzJobService>();
        services.AddHttpClient();

        // Quartz.NET
        // Настройка сериализатора (обязательно для AdoJobStore)
        // Используем 'stj' (алиас для SystemTextJsonObjectSerializer из пакета Quartz.Serialization.SystemTextJson)
        var serializerType = configuration.GetValue<string>("Quartz:quartz.serializer.type") ?? "stj";

        services.AddQuartz(q =>
        {
            // Устанавливаем свойство сериализатора напрямую
            q.Properties["quartz.serializer.type"] = serializerType;
            // Используем SQL Server как persistent store
            q.UsePersistentStore(s =>
            {
                s.PerformSchemaValidation = true;
                s.UseProperties = true; // Используем строки для JobDataMap
                s.RetryInterval = TimeSpan.FromSeconds(15);
                s.UseSqlServer(sqlServer =>
                {
                    sqlServer.ConnectionString = configuration.GetConnectionString("DefaultConnection");
                    sqlServer.TablePrefix = "QRTZ_";
                });
                
                // Настройка кластеризации (если нужно)
                var clustered = configuration.GetValue<string>("Quartz:quartz.jobStore.clustered") == "true";
                if (clustered)
                {
                    s.UseClustering(c =>
                    {
                        c.CheckinMisfireThreshold = TimeSpan.FromSeconds(20);
                        c.CheckinInterval = TimeSpan.FromSeconds(10);
                    });
                }
            });

            // Настройка Scheduler из конфигурации
            var instanceName = configuration.GetValue<string>("Quartz:quartz.scheduler.instanceName") ?? "XLabStatusServiceScheduler";
            q.SchedulerId = "AUTO";
            q.SchedulerName = instanceName;
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        return services;
    }
}

