# Структура проекта X-Lab Status Service

## Общая структура

```
X-Lab Status Service/
├── .cursor/                    # Cursor IDE configuration and rules
│   └── rules/                 # AI assistant rules
├── src/                       # Source code directory
│   └── backend/               # Backend projects (ASP.NET Core)
│       ├── XLabStatusService.Api/        # Main API project
│       ├── XLabStatusService.Core/      # Core domain logic
│       ├── XLabStatusService.Infrastructure/  # Infrastructure layer
│       └── XLabStatusService.Application/    # Application layer
├── tests/                       # Test projects
│   └── backend/               # Backend tests (будущие проекты)
├── docs/                        # Project documentation
│   ├── TZ-Backend.md            # Backend technical specification
│   ├── TZ-Frontend.md           # Frontend technical specification
│   └── TZ.md                    # General overview
├── .gitignore                   # Git ignore patterns
├── XLabStatusService.sln        # Solution file (Backend)
└── README.md                    # Project README
```

## Структура проектов

### XLabStatusService.Core (Domain Layer)

```
XLabStatusService.Core/
├── Entities/                    # Domain entities
├── Interfaces/                  # Repository and service interfaces
├── Enums/                       # Enumerations
│   ├── ServiceType.cs          # Типы сервисов (Http, Tcp, Database, Redis, WindowsService, Custom)
│   └── HealthStatus.cs         # Статусы здоровья (Healthy, Degraded, Unhealthy, Unknown)
└── Exceptions/                  # Custom exceptions
```

### XLabStatusService.Application (Application Layer)

```
XLabStatusService.Application/
├── DTOs/                        # Data Transfer Objects
├── Mappings/                    # AutoMapper profiles
├── Validators/                  # FluentValidation validators
└── Services/                    # Application services
```

### XLabStatusService.Infrastructure (Infrastructure Layer)

```
XLabStatusService.Infrastructure/
├── Data/
│   ├── ApplicationDbContext.cs # DbContext
│   └── Configurations/         # EF Core configurations
├── Repositories/                # Repository implementations
├── Services/                    # Infrastructure services
│   ├── HealthCheckService.cs
│   ├── HttpHealthCheckProvider.cs
│   ├── DatabaseHealthCheckProvider.cs
│   ├── RedisHealthCheckProvider.cs
│   └── WindowsServiceHealthCheckProvider.cs (будущий)
├── Jobs/                        # Quartz.NET jobs
│   ├── HealthCheckJob.cs
│   └── CleanupJob.cs
├── HealthChecks/                # ASP.NET Core Health Checks
├── SignalR/                     # SignalR extensions
└── Extensions/                  # Service collection extensions
```

### XLabStatusService.Api (API Layer)

```
XLabStatusService.Api/
├── Controllers/                 # API controllers
│   ├── PublicController.cs     # Public endpoints (no auth)
│   ├── ServicesController.cs   # Private endpoints (with auth)
│   ├── AuthController.cs       # Authentication endpoints
│   └── WebhooksController.cs   # Webhooks management
├── Hubs/                        # SignalR hubs
│   └── StatusHub.cs            # Real-time updates hub
├── Middleware/                  # Custom middleware
│   ├── ExceptionHandlingMiddleware.cs
│   ├── RequestLoggingMiddleware.cs
│   └── RateLimitingMiddleware.cs
├── Filters/                     # Action filters
│   ├── AuthorizeAdminAttribute.cs
│   └── ValidateModelAttribute.cs
├── Program.cs                   # Application entry point
└── appsettings.json            # Configuration
```

## Зависимости между проектами

```
XLabStatusService.Api
    ├── XLabStatusService.Application
    └── XLabStatusService.Infrastructure
            └── XLabStatusService.Core
XLabStatusService.Application
    └── XLabStatusService.Core
```

## Текущий статус

✅ Создана базовая структура проекта
✅ Настроены зависимости между проектами
✅ Созданы базовые enum классы (ServiceType, HealthStatus)
✅ Настроены файлы конфигурации (appsettings.json)
✅ Проект успешно собирается

## Следующие шаги

✅ 1. Добавить необходимые NuGet пакеты (Entity Framework Core, Quartz.NET, SignalR, Serilog и т.д.)
✅ 2. Создать доменные сущности (Service, HealthCheckResult, User, RefreshToken, ServiceConfiguration, Webhook)
✅ 3. Реализовать репозитории и сервисы
✅ 4. Создать контроллеры и endpoints (PublicController, ServicesController, AuthController, WebhooksController)
✅ 5. Настроить Entity Framework Core (DbContext, конфигурации созданы; миграции нужно создать вручную: `dotnet ef migrations add InitialCreate`)
✅ 6. Реализовать health check провайдеры (HttpHealthCheckProvider, DatabaseHealthCheckProvider, RedisHealthCheckProvider, WindowsServiceHealthCheckProvider)
✅ 7. Настроить Quartz.NET для фоновых задач (HealthCheckJob, QuartzJobService для динамического управления Job'ами)
✅ 8. Реализовать SignalR для real-time обновлений (StatusHub, SignalRNotificationService, интеграция с HealthCheckService)

## Дополнительно выполнено

- ✅ Создан WebhooksController для управления webhooks
- ✅ Интегрирован SignalR с HealthCheckService для автоматической отправки обновлений
- ✅ Реализован QuartzJobService для динамического создания/обновления/удаления Job при изменении сервисов
- ✅ Создан INotificationService для абстракции уведомлений (соответствует Clean Architecture)
- ✅ Все сервисы зарегистрированы в DI контейнере

## Примечания

- Миграции EF Core нужно создать вручную командой: `dotnet ef migrations add InitialCreate --project src/backend/XLabStatusService.Infrastructure --startup-project src/backend/XLabStatusService.Api`
- Для создания миграций требуется установить dotnet-ef: `dotnet tool install --global dotnet-ef`

