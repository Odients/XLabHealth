using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Interfaces;
using XLabStatusService.Infrastructure.Data;
using XLabStatusService.Infrastructure.Services;

namespace XLabStatusService.Infrastructure.Extensions;

/// <summary>
/// Расширения для инициализации начальных данных в базе данных
/// </summary>
public static class DatabaseSeederExtensions
{
    /// <summary>
    /// Инициализирует начальные данные в базе данных (seed data)
    /// </summary>
    public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        try
        {
            // Проверяем, применены ли миграции
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogWarning("There are pending migrations. Please run 'dotnet ef database update' first. Skipping seed.");
                return;
            }

            // Проверяем, что база данных доступна
            if (!await context.Database.CanConnectAsync())
            {
                logger.LogWarning("Cannot connect to database. Skipping seed.");
                return;
            }

            // Создаем начального пользователя admin, если его нет
            await SeedAdminUserAsync(context, logger);

            // Инициализируем Quartz jobs для всех включенных сервисов
            await InitializeQuartzJobsAsync(services, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private static async Task SeedAdminUserAsync(ApplicationDbContext context, ILogger logger)
    {
        const string adminUsername = "admin";
        const string adminPassword = "Amber_9";
        const string adminEmail = "admin@x-lab.by";
        const string adminRole = "Admin";

        // Проверяем, существует ли пользователь admin
        var existingAdmin = await context.Users
            .FirstOrDefaultAsync(u => u.Username == adminUsername);

        if (existingAdmin != null)
        {
            logger.LogInformation("Admin user already exists. Skipping seed.");
            return;
        }

        // Создаем пользователя admin
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = adminUsername,
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            Role = adminRole,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        logger.LogInformation("Admin user created successfully. Username: {Username}, Email: {Email}", 
            adminUsername, adminEmail);
    }

    /// <summary>
    /// Инициализирует Quartz jobs для всех включенных сервисов
    /// </summary>
    private static async Task InitializeQuartzJobsAsync(IServiceProvider services, ILogger logger)
    {
        try
        {
            var serviceRepository = services.GetRequiredService<IServiceRepository>();
            var quartzJobService = services.GetRequiredService<QuartzJobService>();

            // Получаем все включенные сервисы
            var enabledServices = await serviceRepository.GetEnabledServicesAsync();

            if (!enabledServices.Any())
            {
                logger.LogInformation("No enabled services found. Skipping Quartz jobs initialization.");
                return;
            }

            logger.LogInformation("Initializing Quartz jobs for {Count} enabled services", enabledServices.Count());

            // Создаем или обновляем jobs для каждого сервиса
            foreach (var service in enabledServices)
            {
                try
                {
                    await quartzJobService.CreateOrUpdateJobAsync(service);
                    logger.LogDebug("Initialized Quartz job for service {ServiceId} ({ServiceName})", 
                        service.Id, service.Name);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to initialize Quartz job for service {ServiceId} ({ServiceName})", 
                        service.Id, service.Name);
                }
            }

            logger.LogInformation("Quartz jobs initialization completed. Processed {Count} services", 
                enabledServices.Count());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing Quartz jobs.");
            // Не пробрасываем исключение, чтобы не блокировать запуск приложения
        }
    }
}

