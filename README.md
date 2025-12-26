# X-Lab Status Service

Сервис мониторинга здоровья сервисов для экосистемы X-Lab.

## Структура проекта

Проект следует принципам Clean Architecture и разделен на следующие слои:

- **XLabStatusService.Core** - Доменные сущности, интерфейсы, enums
- **XLabStatusService.Application** - DTOs, маппинг, валидация
- **XLabStatusService.Infrastructure** - Репозитории, провайдеры health checks, Quartz.NET Jobs
- **XLabStatusService.Api** - Контроллеры, SignalR Hub, Middleware

## Требования

- .NET 8.0 SDK
- Microsoft SQL Server 2019+ (для базы данных XLabHealth)
- Visual Studio 2022 или Rider (опционально)

## Настройка

1. Обновите строку подключения в `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=YOUR_SERVER;Initial Catalog=XLabHealth;..."
  }
}
```

2. Создайте миграции EF Core:
```bash
dotnet ef migrations add InitialCreate --project src/backend/XLabStatusService.Infrastructure --startup-project src/backend/XLabStatusService.Api
```

3. Примените миграции:
```bash
dotnet ef database update --project src/backend/XLabStatusService.Infrastructure --startup-project src/backend/XLabStatusService.Api
```

## Запуск

```bash
cd src/backend/XLabStatusService.Api
dotnet run
```

API будет доступен по адресу: `https://localhost:5001` (или `http://localhost:5000`)

Swagger UI: `https://localhost:5001/swagger`

## API Endpoints

### Публичные (без авторизации)
- `GET /api/public/status` - общий статус системы
- `GET /api/public/services` - список публичных сервисов
- `GET /api/public/services/{id}` - информация о публичном сервисе
- `GET /api/public/summary` - общая сводка

### Приватные (требуется авторизация)
- `GET /api/services` - все сервисы (Admin, Viewer)
- `GET /api/services/{id}` - детали сервиса (Admin, Viewer)
- `POST /api/services` - создать сервис (Admin)
- `PUT /api/services/{id}` - обновить сервис (Admin)
- `DELETE /api/services/{id}` - удалить сервис (Admin)
- `GET /api/services/{id}/history` - история проверок (Admin, Viewer)

### Аутентификация
- `POST /api/auth/login` - вход в систему
- `POST /api/auth/refresh` - обновить токен
- `POST /api/auth/logout` - выход из системы

### Webhooks (Admin)
- `GET /api/webhooks` - список webhooks
- `POST /api/webhooks` - создать webhook
- `PUT /api/webhooks/{id}` - обновить webhook
- `DELETE /api/webhooks/{id}` - удалить webhook

## SignalR Hub

- Hub URL: `/hubs/status`
- Методы:
  - `SubscribeToService(serviceId)` - подписка на обновления сервиса
  - `SubscribeToAllServices()` - подписка на все сервисы
  - `UnsubscribeFromService(serviceId)` - отписка

## Health Check Providers

Поддерживаются следующие типы проверок:
- **HTTP/HTTPS** - проверка REST API endpoints
- **Database** - проверка MS SQL Server
- **Redis** - проверка Redis Server
- **Windows Service** - проверка Windows Services

## Следующие шаги

1. Реализовать полную JWT аутентификацию
2. Добавить AutoMapper профили для маппинга
3. Добавить FluentValidation валидаторы
4. Реализовать динамическое управление Quartz.NET Jobs
5. Добавить интеграцию SignalR с HealthCheckService
6. Реализовать отправку webhooks
