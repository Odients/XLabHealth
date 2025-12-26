# Техническое задание - Backend
## Сервис мониторинга здоровья сервисов X-Lab Status Service
### Backend (ASP.NET Core API)

---

## 1. Общие сведения

### 1.1. Назначение документа
Настоящее техническое задание определяет требования к разработке backend части сервиса мониторинга здоровья сервисов и отображения временной шкалы их состояний для экосистемы X-Lab.

### 1.2. Цель проекта
Создание RESTful API сервиса для:
- Мониторинга текущего состояния здоровья всех сервисов в экосистеме
- Сбора и хранения исторической информации о состояниях сервисов
- Предоставления API для интеграции с другими сервисами и frontend приложением
- Обеспечения публичного и приватного доступа к данным
- Реализации системы аутентификации и авторизации

### 1.3. Область применения
Backend сервис предназначен для:
- Предоставления API для frontend приложения
- Интеграции с другими сервисами экосистемы X-Lab
- Автоматического мониторинга состояния сервисов
- Хранения и обработки данных о состоянии сервисов

---

## 2. Функциональные требования

### 2.1. Мониторинг здоровья сервисов

#### 2.1.1. Сбор информации о состоянии сервисов
- **Описание**: Сервис должен периодически проверять состояние всех зарегистрированных сервисов
- **Требования**:
  - Поддержка различных типов проверок (HTTP endpoints, TCP соединения, проверка БД, Windows Services, Redis)
  - Настраиваемый интервал проверки для каждого сервиса
  - Поддержка таймаутов и retry-логики
  - Сбор дополнительных метрик (время отклика, использование ресурсов)
  - Асинхронное выполнение проверок
  - Параллельная проверка нескольких сервисов

#### 2.1.2. Классификация состояний
- **Healthy** (Здоров) - сервис работает корректно
- **Degraded** (Деградирован) - сервис работает, но с ограниченной функциональностью
- **Unhealthy** (Не здоров) - сервис не работает или недоступен
- **Unknown** (Неизвестно) - состояние не может быть определено

#### 2.1.3. Хранение истории состояний
- Сохранение всех изменений состояний с временными метками
- Хранение метрик производительности
- Настраиваемый период хранения данных
- Партиционирование таблиц по дате для оптимизации

### 2.2. Режимы доступа

#### 2.2.1. Public (Публичный доступ)
- **Назначение**: Предоставление общей информации о здоровье системы без авторизации
- **Доступная информация**:
  - Общий статус системы (Healthy/Degraded/Unhealthy)
  - Список публичных сервисов с их текущими состояниями (без деталей)
  - Общие метрики (количество публичных сервисов, количество проблемных)
  - Время последнего обновления
  - Общий процент доступности системы
- **Ограничения**:
  - Нет доступа к детальной информации о сервисах
  - Нет доступа к истории состояний
  - Нет доступа к метрикам производительности
  - Нет доступа к конфигурации и администрированию
  - Нет доступа к приватным сервисам (IsPublic = false)

#### 2.2.2. Private (Приватный доступ)
- **Назначение**: Полный доступ к детальной информации для авторизованных пользователей
- **Доступная информация**:
  - Детальная информация о каждом сервисе (включая приватные)
  - Полная история состояний и временная шкала
  - Метрики производительности и аналитика
  - Конфигурация проверок
  - Управление сервисами и настройками (для роли Admin)
  - Логи ошибок и исключений
  - Настройка публичности сервисов (IsPublic флаг)
- **Требования**:
  - Обязательная аутентификация (JWT токены)
  - Авторизация по ролям:
    - **Admin**: полный доступ, включая управление
    - **Viewer**: доступ к просмотру всех данных без возможности изменений

### 2.3. REST API

#### 2.3.1. Public REST API (без авторизации)
- **GET /api/public/status** - общий статус системы
  - Возвращает: общий статус, количество сервисов, время последнего обновления
- **GET /api/public/services** - список публичных сервисов с базовой информацией
  - Возвращает: только сервисы с IsPublic = true, только состояние и название
- **GET /api/public/services/{id}** - базовая информация о публичном сервисе
  - Возвращает: название, текущее состояние, время последней проверки
- **GET /api/public/summary** - общая сводка
  - Возвращает: количество сервисов, статистика состояний, процент доступности

#### 2.3.2. Private REST API (требуется авторизация)
- **GET /api/services** - полный список всех сервисов с деталями
  - Требует: роль Viewer или Admin
  - Возвращает: полная информация о всех сервисах
- **GET /api/services/{id}** - детальная информация о конкретном сервисе
  - Требует: роль Viewer или Admin
- **GET /api/services/{id}/status** - текущее состояние сервиса с метриками
  - Требует: роль Viewer или Admin
- **GET /api/services/{id}/history** - полная история состояний
  - Требует: роль Viewer или Admin
  - Параметры: fromDate, toDate, page, pageSize
- **GET /api/services/{id}/metrics** - метрики производительности
  - Требует: роль Viewer или Admin
- **POST /api/services** - регистрация нового сервиса
  - Требует: роль Admin
  - Body: ServiceCreateDto
- **PUT /api/services/{id}** - обновление конфигурации сервиса
  - Требует: роль Admin
  - Body: ServiceUpdateDto
- **DELETE /api/services/{id}** - удаление сервиса из мониторинга
  - Требует: роль Admin
- **GET /api/health** - проверка здоровья самого сервиса мониторинга
- **GET /api/analytics** - аналитика и статистика
  - Требует: роль Admin или Viewer
  - Возвращает: метрики доступности, статистика по состояниям, тренды

#### 2.3.3. Аутентификация
- **POST /api/auth/login** - вход в систему
  - Body: { username, password }
  - Возвращает: { accessToken, refreshToken, expiresIn, user }
- **POST /api/auth/refresh** - обновление токена
  - Body: { refreshToken }
  - Возвращает: { accessToken, refreshToken, expiresIn }
- **POST /api/auth/logout** - выход из системы
  - Требует: авторизация
  - Body: { refreshToken }

#### 2.3.4. Webhooks
- **GET /api/webhooks** - список webhooks (требует роль Admin)
- **POST /api/webhooks** - создание webhook (требует роль Admin)
- **PUT /api/webhooks/{id}** - обновление webhook (требует роль Admin)
- **DELETE /api/webhooks/{id}** - удаление webhook (требует роль Admin)
- Отправка уведомлений при изменении состояния сервиса
- Настраиваемые события (переход в Unhealthy, восстановление и т.д.)

### 2.4. Real-time обновления

#### 2.4.1. SignalR Hub
- **Hub**: `/hubs/status`
- **Методы**:
  - `SubscribeToService(serviceId)` - подписка на обновления конкретного сервиса
  - `SubscribeToAllServices()` - подписка на обновления всех сервисов
  - `UnsubscribeFromService(serviceId)` - отписка от обновлений
- **События**:
  - `ServiceStatusChanged` - изменение состояния сервиса
  - `ServiceChecked` - завершение проверки сервиса
  - `SystemStatusChanged` - изменение общего статуса системы

---

## 3. Технические требования

### 3.1. Стек технологий

#### 3.1.1. Платформа и язык
- **Платформа**: ASP.NET Core 8.0+
- **Язык**: C# 12
- **Архитектура**: RESTful API
- **Target Framework**: .NET 8.0

#### 3.1.2. Основные библиотеки
- **ORM**: Entity Framework Core 8.0+
- **Health Checks**: ASP.NET Core Health Checks
- **Real-time**: ASP.NET Core SignalR
- **Валидация**: FluentValidation
- **Логирование**: Serilog
- **Конфигурация**: IOptions pattern, appsettings.json
- **Аутентификация**: Microsoft.AspNetCore.Authentication.JwtBearer
- **Swagger**: Swashbuckle.AspNetCore (Swagger/OpenAPI)
- **Планировщик задач**: Quartz 3.8.0+ (Quartz.AspNetCore для интеграции с ASP.NET Core)
- **Redis клиент**: StackExchange.Redis 2.x+ (для мониторинга Redis Server)
- **SQL Server**: Microsoft.Data.SqlClient (встроен в EF Core, для прямых проверок БД)

#### 3.1.3. Фоновые задачи
- **Планировщик задач**: Quartz.NET 3.x
  - Периодические проверки здоровья сервисов
  - Настраиваемые интервалы для каждого сервиса
  - Поддержка cron-выражений для сложных расписаний
  - Кластеризация для распределенной работы (при масштабировании)
  - Персистентность заданий в БД (MS SQL Server)
  - Обработка ошибок и retry логика
- **Background Services**: IHostedService для интеграции Quartz.NET с ASP.NET Core
- **Job Store**: ADO.NET Job Store (SQL Server) для хранения состояния заданий

#### 3.1.4. Дополнительные компоненты
- **Кэширование**: Redis (опционально, для повышения производительности)
- **HTTP клиент**: HttpClientFactory для проверок сервисов
- **Контейнеризация**: Docker
- **Оркестрация**: Docker Compose (для локальной разработки)

### 3.2. База данных

#### 3.2.1. Основная БД
- **СУБД**: Microsoft SQL Server 2019+
- **Название базы данных**: XLabHealth
- **Назначение**: Отдельная база данных для сервиса мониторинга здоровья сервисов
- **Примечание**: БД XLabHealth отделена от основной базы данных xlab, что обеспечивает изоляцию данных мониторинга
- **Миграции**: Entity Framework Core Migrations
- **Резервное копирование**: Настраиваемые политики бэкапов

#### 3.2.2. Схема данных
- **Services** - информация о сервисах (с флагом IsPublic)
- **HealthCheckResults** - результаты проверок (partitioned по дате)
- **ServiceConfigurations** - конфигурации проверок
- **ServiceTags** - теги для сервисов
- **Incidents** - инциденты (периоды недоступности)
- **Metrics** - метрики производительности
- **Users** - пользователи системы
- **UserRoles** - роли пользователей (Admin, Viewer)
- **RefreshTokens** - токены для обновления сессий
- **Notifications** - настройки уведомлений
- **Webhooks** - настройки webhooks
- **Quartz.NET таблицы** (префикс QRTZ_):
  - **QRTZ_JOB_DETAILS** - детали Job'ов
  - **QRTZ_TRIGGERS** - триггеры для Job'ов
  - **QRTZ_SIMPLE_TRIGGERS** - простые триггеры
  - **QRTZ_CRON_TRIGGERS** - cron триггеры
  - **QRTZ_SCHEDULER_STATE** - состояние планировщика (для кластеризации)
  - **QRTZ_LOCKS** - блокировки для кластеризации
  - И другие служебные таблицы Quartz.NET

#### 3.2.3. Назначение таблиц Quartz.NET
Таблицы Quartz.NET используются для хранения состояния и конфигурации фоновых задач (Job'ов), которые выполняют периодические проверки здоровья сервисов.

**Основные таблицы и их назначение**:

- **QRTZ_JOB_DETAILS** - хранит информацию о каждом Job:
  - Имя и группа Job'а
  - Тип Job'а (класс, который будет выполнен)
  - Параметры Job'а (например, ServiceId для проверки конкретного сервиса)
  - Описание и другие метаданные
  
- **QRTZ_TRIGGERS** - хранит информацию о триггерах (когда и как часто выполнять Job):
  - Связь с Job'ом
  - Тип триггера (Simple или Cron)
  - Время следующего выполнения
  - Состояние триггера (активен, приостановлен, завершен)
  
- **QRTZ_SIMPLE_TRIGGERS** - для простых триггеров с фиксированным интервалом:
  - Интервал повторения (например, каждые 60 секунд)
  - Количество повторений
  - Используется для проверки сервисов с фиксированным интервалом
  
- **QRTZ_CRON_TRIGGERS** - для cron-триггеров со сложным расписанием:
  - Cron-выражение (например, "0 */5 * * * ?" - каждые 5 минут)
  - Используется для проверок по сложному расписанию
  
- **QRTZ_SCHEDULER_STATE** - состояние планировщика (для кластеризации):
  - Информация о каждом инстансе планировщика
  - Время последнего "heartbeat" (проверки активности)
  - Необходимо для распределенной работы нескольких инстансов сервиса
  
- **QRTZ_LOCKS** - блокировки для предотвращения конфликтов:
  - Обеспечивает, что один Job выполняется только одним инстансом
  - Предотвращает дублирование проверок при работе в кластере

**Зачем это нужно в проекте**:

1. **Персистентность**: При перезапуске приложения все запланированные проверки сервисов сохраняются и продолжают выполняться
2. **Кластеризация**: При работе нескольких инстансов сервиса, Quartz.NET автоматически распределяет Job'ы между ними
3. **Надежность**: Если один инстанс упадет, другой подхватит его Job'ы
4. **Гибкость**: Можно динамически создавать, изменять и удалять Job'ы при добавлении/изменении сервисов
5. **Мониторинг**: Можно отслеживать состояние всех запланированных проверок через БД

**Пример использования в проекте**:
- При добавлении нового сервиса через API создается Job в `QRTZ_JOB_DETAILS` с триггером в `QRTZ_TRIGGERS`
- Job содержит ServiceId в параметрах, чтобы знать, какой сервис проверять
- Триггер настроен на выполнение каждые N секунд (интервал проверки сервиса)
- При изменении интервала проверки обновляется триггер
- При удалении сервиса удаляется соответствующий Job и триггер

### 3.3. Архитектура разделения Public/Private

#### 3.3.1. Структура контроллеров
- **PublicController** - публичные endpoints без авторизации
  - `[AllowAnonymous]` атрибут
  - Возвращает только базовую информацию
- **ServicesController** - приватные endpoints с авторизацией
  - `[Authorize]` атрибут
  - Проверка ролей через `[Authorize(Roles = "Admin")]`
- **AuthController** - управление аутентификацией
  - Публичные endpoints для login/refresh
  - Приватные endpoints для logout

#### 3.3.2. Middleware
- **JWT Bearer Authentication** для приватных endpoints
- **CORS политики** для разделения доступа
- **Rate limiting** для публичных endpoints (защита от злоупотреблений)
- **Exception handling** middleware для обработки ошибок
- **Request logging** middleware

#### 3.3.3. DTO (Data Transfer Objects)
- **PublicServiceDto** - для публичных endpoints (минимальная информация)
- **ServiceDto** - для приватных endpoints (полная информация)
- **ServiceCreateDto** - для создания сервиса
- **ServiceUpdateDto** - для обновления сервиса
- **HealthCheckResultDto** - для результатов проверок
- **UserDto** - для информации о пользователе
- **LoginDto**, **RefreshTokenDto** - для аутентификации

### 3.4. Интеграция с существующими сервисами

#### 3.4.1. Мониторинг X-Lab API (ASP.NET на IIS)
- **Тип проверки**: HTTP/HTTPS Health Check endpoint
- **Endpoint**: Стандартный ASP.NET Core Health Checks endpoint (`/api/health`)
- **Метод проверки**:
  - HTTP GET запрос на health check endpoint
  - Ожидаемый статус код: 200 (Healthy), 503 (Unhealthy)
  - Проверка времени отклика
  - Парсинг JSON ответа с детальной информацией о модулях
- **Формат ответа**:
  ```json
  {
    "status": "healthy",                    // Общий статус: "healthy", "degraded", "unhealthy"
    "timestamp": "2025-12-25T16:35:43.4720107Z",
    "version": "1.0.1450.0",                // Версия приложения
    "modules": {
      "database": {                         // Модули базы данных
        "openIddict": {
          "status": "healthy",
          "responseTime": 2,                // Время отклика в мс
          "error": null,
          "details": null
        },
        "working": { "status": "healthy", "responseTime": 1, ... },
        "license": { "status": "healthy", "responseTime": 6, ... },
        "report": { "status": "healthy", "responseTime": 6, ... }
      },
      "cache": {                            // Кэш (Redis)
        "redis": {
          "status": "healthy",
          "responseTime": 66,
          "error": null,
          "details": null
        }
      },
      "external": {                          // Внешние сервисы
        "n8N": {
          "status": "healthy",
          "responseTime": 148,
          "error": null,
          "details": null
        }
      },
      "api": {                              // API модули
        "oDataModules": {
          "status": "healthy",
          "responseTime": 0,
          "error": null,
          "details": {
            "count": 66                     // Количество модулей
          }
        }
      }
    }
  }
  ```
- **Интерпретация статуса**:
  - **"healthy"** → `HealthStatus.Healthy` - все модули работают нормально
  - **"degraded"** → `HealthStatus.Degraded` - некоторые модули работают с ограничениями
  - **"unhealthy"** → `HealthStatus.Unhealthy` - критические модули недоступны
- **Извлекаемые метрики**:
  - Общий статус системы
  - Версия приложения
  - Время отклика каждого модуля (database, cache, external, api)
  - Статус каждого модуля отдельно
  - Детальная информация о модулях (например, количество OData модулей)
  - Время последней проверки (timestamp)
- **Логика определения состояния**:
  - Если `status == "healthy"` → сервис здоров (`HealthStatus.Healthy`)
  - Если `status == "degraded"` → сервис деградирован (`HealthStatus.Degraded`)
  - Если `status == "unhealthy"` → сервис не здоров (`HealthStatus.Unhealthy`)
  - Если любой критический модуль (database, cache) имеет `status != "healthy"` → можно считать деградированным
  - Если модуль имеет `error != null` → записывать в `Exception` поле
- **Метрики для сохранения**:
  - Общий статус из поля `status`
  - Версия приложения из поля `version`
  - Время отклика каждого модуля (database, cache, external, api)
  - Максимальное время отклика среди всех модулей (для общего `ResponseTime`)
  - Детальная информация о модулях в `Metadata` (JSON)
  - Timestamp из ответа для синхронизации
- **Аутентификация**: Поддержка JWT токенов или API Keys при необходимости
- **Конфигурация**:
  ```json
  {
    "Type": "Http",
    "Url": "https://api.x-lab.by/api/health",
    "Method": "GET",
    "ExpectedStatusCode": 200,
    "Timeout": 5000,
    "Headers": {
      "Authorization": "Bearer {token}" // опционально
    },
    "ParseModules": true,              // Парсить детальную информацию о модулях
    "CriticalModules": ["database", "cache"] // Критические модули для определения статуса
  }
  ```

#### 3.4.2. Мониторинг MS SQL Server
- **Тип проверки**: Database Connection Check
- **Важно**: Сервис мониторинга использует свою базу данных **XLabHealth** для хранения данных о мониторинге, но мониторит другую базу данных **xlab** (основную БД приложения)
- **Метод проверки**:
  - Установка соединения с БД через connection string
  - Выполнение простого запроса (например, `SELECT 1`)
  - Проверка времени отклика БД
  - Валидация доступности базы данных
- **Базовые метрики** (всегда собираются):
  - Время отклика на запрос (response time)
  - Статус доступности БД (доступна/недоступна)
  - Время выполнения тестового запроса

- **Расширенные метрики** (опционально, настраиваются через конфигурацию):
  - **Размер базы данных**:
    - Общий размер БД (в МБ/ГБ)
    - Размер данных (data files)
    - Размер логов (log files)
    - Свободное пространство
    - Процент использования
  - **Активные соединения**:
    - Общее количество активных соединений
    - Количество соединений к конкретной БД
    - Количество блокирующих соединений
    - Максимальное количество разрешенных соединений
  - **Производительность**:
    - Среднее время выполнения запросов
    - Количество ожидающих запросов
    - Количество deadlock'ов
    - Количество блокировок (locks)
  - **Использование ресурсов** (если доступно):
    - CPU usage (если есть права на sys.dm_os_performance_counters)
    - Memory usage
    - I/O статистика (reads/writes)
  - **Информация о таблицах**:
    - Количество таблиц
    - Количество строк в основных таблицах (если доступно)
    - Размер индексов
  - **Резервное копирование**:
    - Дата последнего бэкапа
    - Тип последнего бэкапа (Full/Diff/Log)
    - Статус бэкапа
  - **Статус сервиса SQL Server**:
    - Состояние сервиса (Running/Stopped)
    - Версия SQL Server
    - Edition (Standard/Enterprise и т.д.)
- **Безопасность**:
  - Использование отдельного пользователя БД с минимальными правами (только SELECT для системных представлений)
  - Хранение connection string в зашифрованном виде или в переменных окружения
  - Пароль не должен храниться в открытом виде в конфигурационных файлах
  - Поддержка Windows Authentication и SQL Authentication
  - Использование `TrustServerCertificate=True` только для внутренних/тестовых окружений
- **Конфигурация**:
  ```json
  {
    "Type": "Database",
    "Provider": "SqlServer",
    "ConnectionString": "Data Source=db.x-lab.by;Initial Catalog=xlab;Integrated Security=False;TrustServerCertificate=True;User ID=crmuser;Password=***;Connection Timeout=600",
    "TestQuery": "SELECT 1",
    "Timeout": 10000,
    "CheckDatabaseSize": true,
    "CheckActiveConnections": true
  }
  ```
  **Важно**: 
  - Connection string указывает на БД **xlab**, которую мы мониторим (проверяем)
  - Сервис мониторинга использует отдельную БД **XLabHealth** для хранения своих данных
  - Пароль должен храниться в зашифрованном виде или в переменных окружения. В примере показан замаскированный пароль.
- **SQL запросы для расширенных метрик**:

  **Размер базы данных**:
  ```sql
  -- Общий размер БД в МБ
  SELECT 
      SUM(size) * 8 / 1024 AS TotalSizeMB,
      SUM(CASE WHEN type_desc = 'ROWS' THEN size END) * 8 / 1024 AS DataSizeMB,
      SUM(CASE WHEN type_desc = 'LOG' THEN size END) * 8 / 1024 AS LogSizeMB
  FROM sys.database_files;
  
  -- Свободное пространство
  SELECT 
      SUM(size) * 8 / 1024 AS TotalSizeMB,
      SUM(FILEPROPERTY(name, 'SpaceUsed')) * 8 / 1024 AS UsedSpaceMB,
      (SUM(size) - SUM(FILEPROPERTY(name, 'SpaceUsed'))) * 8 / 1024 AS FreeSpaceMB
  FROM sys.database_files;
  ```

  **Активные соединения**:
  ```sql
  -- Количество активных соединений к БД
  SELECT COUNT(*) AS ActiveConnections
  FROM sys.dm_exec_sessions 
  WHERE database_id = DB_ID() AND is_user_process = 1;
  
  -- Блокирующие соединения
  SELECT COUNT(*) AS BlockingSessions
  FROM sys.dm_exec_requests
  WHERE blocking_session_id <> 0;
  ```

  **Производительность**:
  ```sql
  -- Среднее время выполнения запросов
  SELECT 
      AVG(total_elapsed_time) AS AvgElapsedTimeMs,
      COUNT(*) AS ActiveRequests
  FROM sys.dm_exec_requests
  WHERE database_id = DB_ID();
  
  -- Количество deadlock'ов (требует права на sys.dm_os_performance_counters)
  SELECT cntr_value AS DeadlockCount
  FROM sys.dm_os_performance_counters
  WHERE counter_name = 'Number of Deadlocks/sec' AND instance_name = '_Total';
  ```

  **Информация о таблицах**:
  ```sql
  -- Количество таблиц
  SELECT COUNT(*) AS TableCount
  FROM sys.tables;
  
  -- Размер таблиц и индексов (топ 10)
  SELECT TOP 10
      t.name AS TableName,
      SUM(p.rows) AS RowCount,
      SUM(a.total_pages) * 8 / 1024 AS TotalSizeMB,
      SUM(a.used_pages) * 8 / 1024 AS UsedSizeMB
  FROM sys.tables t
  INNER JOIN sys.indexes i ON t.object_id = i.object_id
  INNER JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
  INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
  GROUP BY t.name
  ORDER BY SUM(a.total_pages) DESC;
  ```

  **Резервное копирование**:
  ```sql
  -- Последний бэкап
  SELECT TOP 1
      database_name,
      backup_start_date,
      backup_finish_date,
      type,
      CASE type
          WHEN 'D' THEN 'Full'
          WHEN 'I' THEN 'Differential'
          WHEN 'L' THEN 'Log'
      END AS BackupType
  FROM msdb.dbo.backupset
  WHERE database_name = DB_NAME()
  ORDER BY backup_start_date DESC;
  ```

  **Информация о SQL Server**:
  ```sql
  -- Версия и Edition
  SELECT 
      @@VERSION AS Version,
      SERVERPROPERTY('Edition') AS Edition,
      SERVERPROPERTY('ProductVersion') AS ProductVersion;
  ```

- **Конфигурация расширенных метрик**:
  ```json
  {
    "Type": "Database",
    "Provider": "SqlServer",
    "ConnectionString": "...",
    "TestQuery": "SELECT 1",
    "Timeout": 10000,
    "CheckDatabaseSize": true,
    "CheckActiveConnections": true,
    "CheckPerformance": true,
    "CheckTableInfo": false,
    "CheckBackupInfo": true,
    "CheckServerInfo": true,
    "CheckResourceUsage": false
  }
  ```

#### 3.4.3. Мониторинг Redis Server (Linux)
- **Тип проверки**: TCP Connection + Redis Command
- **Метод проверки**:
  - Установка TCP соединения на порт Redis (по умолчанию 6379)
  - Выполнение команды `PING` для проверки доступности
  - Проверка ответа `PONG`
  - Измерение времени отклика
- **Дополнительные метрики**:
  - Время отклика на команду PING
  - Статус Redis (доступен/недоступен)
  - Информация о сервере (через команду `INFO` - опционально)
  - Использование памяти (через `INFO memory`)
  - Количество подключенных клиентов (через `INFO clients`)
- **Безопасность**:
  - Аутентификация не требуется (Redis работает без пароля)
  - Поддержка TLS/SSL соединений (если настроено)
- **Конфигурация**:
  ```json
  {
    "Type": "Redis",
    "Host": "192.168.20.7",
    "Port": 6379,
    "UseSsl": false,
    "Timeout": 3000,
    "CheckMemoryUsage": true,
    "CheckConnectedClients": true
  }
  ```
  **Альтернативный формат** (если используется единая строка подключения):
  ```json
  {
    "Type": "Redis",
    "RedisConnection": "192.168.20.7:6379",
    "UseSsl": false,
    "Timeout": 3000,
    "CheckMemoryUsage": true,
    "CheckConnectedClients": true
  }
  ```
  **Примечание**: Поле `Password` не требуется, так как Redis сервер не требует аутентификации.
- **Реализация**:
  - Использование библиотеки StackExchange.Redis для .NET
  - Обработка ошибок соединения и таймаутов
  - Retry логика при временных сбоях

#### 3.4.4. Мониторинг Windows Services (Windows Службы)
- **Тип проверки**: Windows Service Status Check
- **Метод проверки**:
  - Проверка состояния службы Windows через ServiceController API
  - Проверка статуса службы (Running, Stopped, Paused, StartPending, StopPending, PausePending, ContinuePending)
  - Определение здоровья на основе статуса службы
  - Измерение времени выполнения проверки
- **Статусы служб Windows и их интерпретация**:
  - **Running** → `HealthStatus.Healthy` - служба работает нормально
  - **Stopped** → `HealthStatus.Unhealthy` - служба остановлена
  - **Paused** → `HealthStatus.Degraded` - служба приостановлена
  - **StartPending** / **ContinuePending** → `HealthStatus.Degraded` - служба запускается
  - **StopPending** / **PausePending** → `HealthStatus.Degraded` - служба останавливается
- **Дополнительные метрики**:
  - Статус службы (Status)
  - Имя службы (ServiceName)
  - Отображаемое имя службы (DisplayName)
  - Тип запуска службы (StartType: Automatic, Manual, Disabled, Boot, System)
  - Время последней проверки
- **Безопасность**:
  - Требуются права администратора или учетная запись с правами на чтение состояния служб
  - Можно использовать учетную запись с ограниченными правами (только чтение статуса)
  - Рекомендуется использовать Service Controller API с минимальными необходимыми правами
- **Конфигурация**:
  ```json
  {
    "Type": "WindowsService",
    "ServiceName": "XLabNotificationService",
    "MachineName": ".", // "." для локальной машины или имя удаленного компьютера
    "CheckStartType": false // опционально, проверять тип запуска (Automatic/Manual/Disabled)
  }
  ```
  **Альтернативный формат** (для удаленных машин):
  ```json
  {
    "Type": "WindowsService",
    "ServiceName": "XLabSendService",
    "MachineName": "SERVER-01", // имя удаленного компьютера
    "CheckStartType": true,
    "ExpectedStartType": "Automatic" // ожидаемый тип запуска (опционально)
  }
  ```
- **Реализация**:
  - Использование класса `System.ServiceProcess.ServiceController` из .NET
  - Обработка исключений при недоступности службы или отсутствии прав доступа
  - Retry логика при временных ошибках (например, служба находится в переходном состоянии)
  - Поддержка проверки служб на удаленных машинах (требуется сетевое подключение и права доступа)
- **Примеры проверяемых служб**:
  - XLabNotificationService - служба уведомлений X-Lab
  - XLabSendService - служба отправки сообщений X-Lab
  - Другие Windows службы экосистемы X-Lab

#### 3.4.5. Поддержка других типов сервисов
- **HTTP/HTTPS endpoints** - любые REST API (аналогично X-Lab API)
- **TCP соединения** - проверка доступности портов (для сервисов без HTTP)
- **Проверка доступности баз данных** - PostgreSQL, MySQL, Oracle (аналогично MS SQL)
- **Проверка файловых систем** - доступность путей, размер диска
- **Кастомные проверки** - через плагины и провайдеры

### 3.5. Производительность

#### 3.5.1. Требования к производительности
- Время отклика API: < 200ms для стандартных запросов
- Поддержка одновременной проверки до 100 сервисов
- Минимальный интервал проверки: 30 секунд
- Хранение истории: минимум 90 дней
- Поддержка до 1000 одновременных подключений SignalR

#### 3.5.2. Масштабируемость
- Возможность горизонтального масштабирования
- Поддержка нескольких инстансов сервиса
- Распределенная проверка сервисов между инстансами
- Использование Redis для SignalR backplane (при масштабировании)

### 3.6. Архитектура фоновых задач (Quartz.NET)

#### 3.6.1. Конфигурация Quartz.NET
- **Scheduler Factory**: Использование StdSchedulerFactory с настройками из appsettings.json
- **Job Store**: ADO.NET Job Store (SQL Server)
  - Хранение состояния заданий в БД
  - Поддержка кластеризации для распределенной работы
  - Персистентность заданий при перезапуске приложения
- **Thread Pool**: Настройка пула потоков для параллельного выполнения заданий
- **Интеграция с ASP.NET Core**: Использование QuartzHostedService

#### 3.6.2. Структура Job'ов
- **HealthCheckJob** - базовый Job для проверки здоровья сервиса
  - Принимает ServiceId в JobDataMap
  - Выполняет проверку через соответствующий Health Check провайдер
  - Сохраняет результат в БД
  - Отправляет уведомление через SignalR (если состояние изменилось)
  - Обрабатывает ошибки и логирует их
- **SystemMetricsJob** - Job для сбора системных метрик (опционально)
- **CleanupJob** - Job для очистки старых данных (архивирование)

#### 3.6.3. Управление Job'ами
- **Динамическое создание**: При добавлении нового сервиса создается соответствующий Job
- **Динамическое обновление**: При изменении интервала проверки обновляется Trigger
- **Динамическое удаление**: При удалении сервиса удаляется соответствующий Job
- **Пауза/Возобновление**: Возможность временно остановить проверку сервиса

#### 3.6.4. Trigger'ы
- **SimpleTrigger**: Для фиксированных интервалов (например, каждые 30 секунд)
- **CronTrigger**: Для сложных расписаний (например, проверка каждые 5 минут в рабочее время)
- **Кастомизация**: Каждый сервис может иметь свой собственный Trigger с настраиваемым интервалом

#### 3.6.5. Обработка ошибок
- **Retry логика**: Настройка количества повторных попыток при ошибке
- **Circuit Breaker**: Автоматическая пауза проверок проблемных сервисов
- **Логирование**: Все ошибки логируются через Serilog
- **Уведомления**: Критические ошибки отправляются администраторам

#### 3.6.6. Кластеризация (для масштабирования)
- **Распределенная работа**: Несколько инстансов могут работать в кластере
- **Координация**: Quartz.NET автоматически распределяет Job'ы между инстансами
- **Отказоустойчивость**: При падении одного инстанса, Job'ы перераспределяются

#### 3.6.7. Пример конфигурации
```json
{
  "Quartz": {
    "Scheduler": {
      "InstanceName": "XLabStatusServiceScheduler"
    },
    "ThreadPool": {
      "Type": "Quartz.Simpl.SimpleThreadPool, Quartz",
      "ThreadCount": 10,
      "ThreadPriority": "Normal"
    },
    "JobStore": {
      "Type": "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz",
      "Provider": "SqlServer",
      "ConnectionString": "Data Source=db.x-lab.by;Initial Catalog=XLabHealth;Integrated Security=False;TrustServerCertificate=True;User ID=crmuser;Password=***;Connection Timeout=600",
      "TablePrefix": "QRTZ_",
      "Clustered": true
    }
  }
}
```

### 3.7. Безопасность

#### 3.7.1. Аутентификация и авторизация
- **JWT токены** для аутентификации
  - Access Token: короткоживущий (15-30 минут)
  - Refresh Token: долгоживущий (7-30 дней), хранится в БД
- **Роли пользователей**:
  - **Admin** - полный доступ ко всем функциям, включая управление
  - **Viewer** - доступ к просмотру детальной информации, без возможности изменений
- **API Keys** для интеграций (опционально)
- **CORS** настройки для разделения публичных и приватных endpoints
- **Rate limiting** для защиты от злоупотреблений

#### 3.7.2. Защита данных
- Шифрование чувствительных данных (credentials для проверок)
- HTTPS для всех соединений
- Валидация входных данных (FluentValidation)
- Защита от SQL injection (параметризованные запросы через EF Core)
- Защита от XSS (валидация и санитизация входных данных)
- Хеширование паролей (BCrypt или Argon2)

### 3.8. Надежность

#### 3.8.1. Отказоустойчивость
- Graceful degradation при недоступности БД
- Retry логика для проверок сервисов
- Circuit breaker pattern для проблемных сервисов
- Логирование всех ошибок (Serilog)
- Health checks для самого сервиса мониторинга

#### 3.8.2. Мониторинг
- Health check endpoint для самого сервиса мониторинга
- Метрики производительности (Prometheus format, опционально)
- Structured logging (Serilog)
- Application Insights или аналогичные инструменты

---

## 4. Структура данных

### 4.1. Основные сущности

#### 4.1.1. Service (Сервис)
```csharp
public class Service
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Url { get; set; } // endpoint для проверки
    public ServiceType Type { get; set; } // Http, Tcp, Database, Redis, WindowsService, Custom
    public int CheckInterval { get; set; } // секунды
    public int Timeout { get; set; } // миллисекунды
    public int RetryCount { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsPublic { get; set; } // отображается ли в публичном API
    public List<string> Tags { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<HealthCheckResult> HealthCheckResults { get; set; }
    public virtual ServiceConfiguration Configuration { get; set; }
}
```

#### 4.1.2. HealthCheckResult (Результат проверки)
```csharp
public class HealthCheckResult
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public HealthStatus Status { get; set; } // Healthy, Degraded, Unhealthy, Unknown
    public int ResponseTime { get; set; } // миллисекунды
    public string Message { get; set; }
    public string Exception { get; set; } // если есть
    public DateTime CheckedAt { get; set; }
    public string Metadata { get; set; } // JSON с дополнительными данными
    
    // Navigation property
    public virtual Service Service { get; set; }
}
```

#### 4.1.2.1. HealthCheckResponse (DTO для парсинга ответа X-Lab API)
```csharp
public class HealthCheckResponse
{
    public string Status { get; set; } // "healthy", "degraded", "unhealthy"
    public DateTime Timestamp { get; set; }
    public string Version { get; set; }
    public HealthCheckModules Modules { get; set; }
}

public class HealthCheckModules
{
    public Dictionary<string, ModuleHealth> Database { get; set; }
    public Dictionary<string, ModuleHealth> Cache { get; set; }
    public Dictionary<string, ModuleHealth> External { get; set; }
    public Dictionary<string, ModuleHealth> Api { get; set; }
}

public class ModuleHealth
{
    public string Status { get; set; }
    public int ResponseTime { get; set; }
    public string Error { get; set; }
    public Dictionary<string, object> Details { get; set; }
}
```

#### 4.1.3. ServiceConfiguration (Конфигурация проверки)
```csharp
public class ServiceConfiguration
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public string CheckType { get; set; }
    public string Parameters { get; set; } // JSON
    public string Headers { get; set; } // JSON для HTTP
    public int? ExpectedStatusCode { get; set; } // для HTTP
    public string ExpectedResponse { get; set; } // опционально
    
    // Navigation property
    public virtual Service Service { get; set; }
}
```

#### 4.1.4. User (Пользователь)
```csharp
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; } // Admin, Viewer
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; }
}
```

#### 4.1.5. RefreshToken
```csharp
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }
    
    // Navigation property
    public virtual User User { get; set; }
}
```

### 4.2. Enums

```csharp
public enum ServiceType
{
    Http = 0,           // HTTP/HTTPS endpoints (X-Lab API, REST API)
    Tcp = 1,            // TCP соединения
    Database = 2,       // Базы данных (MS SQL Server, PostgreSQL, MySQL)
    Redis = 3,          // Redis Server
    WindowsService = 4, // Windows Services (XLabNotificationService, XLabSendService и др.)
    Custom = 5          // Кастомные проверки
}

public enum HealthStatus
{
    Healthy = 0,
    Degraded = 1,
    Unhealthy = 2,
    Unknown = 3
}
```

### 4.3. Примеры конфигурации сервисов

#### 4.3.1. Конфигурация X-Lab API (HTTP)
```json
{
  "Name": "X-Lab API",
  "Description": "Основной API сервис X-Lab на IIS",
  "Type": "Http",
  "Url": "https://api.x-lab.by/api/health",
  "CheckInterval": 60,
  "Timeout": 5000,
  "RetryCount": 2,
  "IsEnabled": true,
  "IsPublic": true,
  "Tags": ["api", "iis", "production"],
  "Configuration": {
    "CheckType": "Http",
    "Method": "GET",
    "ExpectedStatusCode": 200,
    "Headers": {},
    "ParseModules": true,
    "CriticalModules": ["database", "cache"],
    "StoreModuleDetails": true
  }
}
```

**Примечание**: При `ParseModules: true` система будет:
- Парсить детальную информацию о модулях из ответа health check
- Сохранять метрики каждого модуля (database, cache, external, api) в `Metadata` поля `HealthCheckResult`
- Определять общий статус на основе критических модулей
- Отслеживать версию приложения из поля `version`
- Сохранять время отклика каждого модуля для детального анализа

#### 4.3.2. Конфигурация MS SQL Server (Database)
```json
{
  "Name": "X-Lab Database",
  "Description": "Основная база данных MS SQL Server",
  "Type": "Database",
  "Url": "sqlserver://db.x-lab.by:1433/xlab",
  "CheckInterval": 120,
  "Timeout": 10000,
  "RetryCount": 3,
  "IsEnabled": true,
  "IsPublic": false,
  "Tags": ["database", "sqlserver", "critical"],
  "Configuration": {
    "CheckType": "SqlServer",
    "ConnectionString": "Data Source=db.x-lab.by;Initial Catalog=xlab;Integrated Security=False;TrustServerCertificate=True;User ID=crmuser;Password=***;Connection Timeout=600",
    "TestQuery": "SELECT 1",
    "CheckDatabaseSize": true,
    "CheckActiveConnections": true
  }
}
```

**Примечания**:
- **Важно**: Это конфигурация для мониторинга основной БД `xlab` (которую мы проверяем). Сам сервис мониторинга использует отдельную БД `XLabHealth` для хранения своих данных (результаты проверок, конфигурация сервисов, пользователи, таблицы Quartz.NET и т.д.)
- **Безопасность**:
  - Пароль в строке подключения должен храниться в зашифрованном виде
  - Рекомендуется использовать переменные окружения или защищенное хранилище секретов (например, Azure Key Vault, AWS Secrets Manager)
  - Пользователь `crmuser` должен иметь минимальные права доступа (только SELECT для системных представлений)
  - `TrustServerCertificate=True` используется для тестовых/внутренних окружений
  - В production рекомендуется использовать SSL/TLS сертификаты

#### 4.3.3. Конфигурация Redis Server (Redis)
```json
{
  "Name": "Redis Cache Server",
  "Description": "Redis сервер для кэширования на Linux",
  "Type": "Redis",
  "Url": "redis://192.168.20.7:6379",
  "CheckInterval": 30,
  "Timeout": 3000,
  "RetryCount": 2,
  "IsEnabled": true,
  "IsPublic": false,
  "Tags": ["cache", "redis", "linux"],
  "Configuration": {
    "CheckType": "Redis",
    "RedisConnection": "192.168.20.7:6379",
    "UseSsl": false,
    "CheckMemoryUsage": true,
    "CheckConnectedClients": true
  }
}
```

**Примечания**:
- Используется внутренний IP адрес `192.168.20.7` (приватная сеть)
- Порт по умолчанию Redis: `6379`
- Аутентификация не требуется - Redis работает без пароля
- Для production рекомендуется использовать DNS имя вместо IP адреса

#### 4.3.4. Конфигурация Windows Services (Windows Службы)
```json
{
  "Name": "X-Lab Notification Service",
  "Description": "Служба уведомлений X-Lab",
  "Type": "WindowsService",
  "Url": "winservice://XLabNotificationService",
  "CheckInterval": 60,
  "Timeout": 5000,
  "RetryCount": 2,
  "IsEnabled": true,
  "IsPublic": false,
  "Tags": ["windows", "service", "notification"],
  "Configuration": {
    "CheckType": "WindowsService",
    "ServiceName": "XLabNotificationService",
    "MachineName": ".",
    "CheckStartType": false
  }
}
```

**Пример конфигурации для XLabSendService**:
```json
{
  "Name": "X-Lab Send Service",
  "Description": "Служба отправки сообщений X-Lab",
  "Type": "WindowsService",
  "Url": "winservice://XLabSendService",
  "CheckInterval": 60,
  "Timeout": 5000,
  "RetryCount": 2,
  "IsEnabled": true,
  "IsPublic": false,
  "Tags": ["windows", "service", "send"],
  "Configuration": {
    "CheckType": "WindowsService",
    "ServiceName": "XLabSendService",
    "MachineName": ".",
    "CheckStartType": true,
    "ExpectedStartType": "Automatic"
  }
}
```

**Примечания**:
- **ServiceName**: Точное имя службы Windows (как оно отображается в `services.msc` или через `sc query`)
- **MachineName**: Имя компьютера:
  - `"."` или `"localhost"` - для локальной машины
  - Имя компьютера в сети (например, `"SERVER-01"`) - для удаленной машины
  - IP адрес - также поддерживается, но рекомендуется использовать имя компьютера
- **CheckStartType**: Если `true`, дополнительно проверяется тип запуска службы (Automatic, Manual, Disabled)
- **ExpectedStartType**: Ожидаемый тип запуска (опционально):
  - `"Automatic"` - автоматический запуск
  - `"Manual"` - ручной запуск
  - `"Disabled"` - отключена
  - Если тип запуска не соответствует ожидаемому, статус будет `Degraded`
- **Безопасность**:
  - Приложение должно запускаться с правами администратора или от имени учетной записи с правами на чтение состояния служб
  - Для проверки служб на удаленных машинах требуется сетевое подключение и соответствующие права доступа
  - В production рекомендуется использовать отдельную учетную запись с минимальными необходимыми правами
- **Интерпретация статусов**:
  - `Running` → `Healthy` - служба работает нормально
  - `Stopped` → `Unhealthy` - служба остановлена (критическая ошибка)
  - `Paused` → `Degraded` - служба приостановлена (работает с ограничениями)
  - `StartPending`, `ContinuePending` → `Degraded` - служба запускается (временное состояние)
  - `StopPending`, `PausePending` → `Degraded` - служба останавливается (временное состояние)
- **Время выполнения проверки**:
  - Проверка локальных служб обычно выполняется очень быстро (< 100ms)
  - Проверка удаленных служб может занять больше времени (зависит от сетевой задержки)
  - Рекомендуется установить таймаут достаточный для проверки удаленных служб (по умолчанию 5000ms)

---

## 5. Этапы разработки

### 5.1. Этап 1: MVP (Минимально жизнеспособный продукт)
**Срок: 2-3 недели**
- Базовая структура проекта ASP.NET Core
- Модели данных и миграции БД (EF Core)
- REST API для управления сервисами (публичные и приватные endpoints)
- Система аутентификации и авторизации (JWT)
- Простая проверка здоровья:
  - HTTP endpoints (X-Lab API на IIS)
  - MS SQL Server (базовая проверка соединения)
  - Redis Server (базовая проверка через PING)
  - Windows Services (XLabNotificationService, XLabSendService - проверка статуса служб)
- Сохранение результатов в БД
- Базовый Health Check endpoint
- Swagger документация

### 5.2. Этап 2: Расширенная функциональность
**Срок: 2-3 недели**
- Временная шкала состояний (API endpoints для истории)
- Расширенные проверки для всех типов сервисов:
  - MS SQL Server: 
    - Метрики размера БД (общий размер, данные, логи, свободное пространство)
    - Активные соединения и блокировки
    - Производительность (время выполнения запросов, deadlock'и)
    - Информация о таблицах и индексах
    - Статус резервного копирования
    - Информация о версии SQL Server
  - Redis Server: метрики использования памяти, подключенные клиенты
  - X-Lab API: расширенные метрики производительности
- Настраиваемые интервалы и параметры для каждого сервиса
- Фоновые задачи для периодических проверок (Quartz.NET)
  - Настройка Quartz.NET с ADO.NET Job Store
  - Создание Job'ов для проверки каждого сервиса
  - Динамическое создание/удаление Job'ов при изменении конфигурации сервисов
  - Обработка результатов проверок и сохранение в БД
- Партиционирование таблиц для оптимизации
- Базовые метрики и статистика
- Webhooks (базовая реализация)

### 5.3. Этап 3: Продвинутые возможности
**Срок: 2-3 недели**
- Real-time обновления (SignalR)
- Расширенная аналитика и отчеты
- Оптимизация производительности
- Кэширование (Redis)
- Расширенные webhooks
- Rate limiting
- Мониторинг самого сервиса

### 5.4. Этап 4: Полировка и документация
**Срок: 1-2 недели**
- Полная документация API (Swagger/OpenAPI)
- Unit тесты и интеграционные тесты
- Настройка CI/CD
- Деплой и настройка production окружения
- Оптимизация и профилирование

---

## 6. Требования к развертыванию

### 6.1. Окружения
- **Development** - локальная разработка
- **Staging** - тестовое окружение
- **Production** - рабочее окружение

### 6.2. Конфигурация
- Использование appsettings.{Environment}.json
- Переменные окружения для чувствительных данных
- Connection strings в защищенном хранилище
- JWT секреты в переменных окружения

### 6.3. Деплой
- Поддержка деплоя в Docker контейнерах
- Возможность деплоя на Windows Server или Linux
- Автоматический деплой через CI/CD pipeline
- Health checks для оркестраторов (Kubernetes, Docker Swarm)

---

## 7. Документация

### 7.1. Техническая документация
- API документация (Swagger/OpenAPI)
- Архитектурная документация
- Документация по развертыванию
- Руководство по интеграции
- Документация по настройке проверок

### 7.2. Код документация
- XML комментарии для всех публичных API
- README с инструкциями по запуску
- Примеры использования API

---

## 8. Критерии приемки

### 8.1. Функциональные требования
- ✅ Все API endpoints работают согласно спецификации
- ✅ Публичные endpoints доступны без авторизации
- ✅ Приватные endpoints требуют авторизацию и проверяют роли
- ✅ Система аутентификации работает корректно (JWT)
- ✅ Все сервисы успешно регистрируются и мониторятся
- ✅ Состояния корректно определяются и сохраняются
- ✅ История состояний сохраняется без потерь данных
- ✅ SignalR hub работает и отправляет обновления

### 8.2. Нефункциональные требования
- ✅ Производительность соответствует требованиям (< 200ms)
- ✅ Система устойчива к сбоям
- ✅ Безопасность данных обеспечена
- ✅ Документация полная и актуальная
- ✅ Код покрыт тестами (минимум 70%)

---

## 9. Риски и ограничения

### 9.1. Технические риски
- Перегрузка БД при большом количестве сервисов
  - **Митигация**: Партиционирование таблиц, архивирование старых данных
- Задержки при проверке медленных сервисов
  - **Митигация**: Асинхронные проверки, таймауты, circuit breaker
- Проблемы с масштабированием SignalR
  - **Митигация**: Redis backplane для распределенных инстансов

### 9.2. Ограничения
- Зависимость от доступности мониторируемых сервисов
- Необходимость настройки каждого сервиса вручную
- Ограничения по хранению данных (зависит от ресурсов БД)

---

## Приложения

### Приложение A: Референсы
- ASP.NET Core Health Checks: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks
- Entity Framework Core: https://learn.microsoft.com/en-us/ef/core/
- SignalR: https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction
- JWT Authentication: https://jwt.io/
- Quartz.NET: https://www.quartz-scheduler.net/
- Quartz.NET Documentation: https://www.quartz-scheduler.net/documentation/
- Quartz.NET Tutorial: https://www.quartz-scheduler.net/documentation/quartz-3.x/tutorial/

---

**Версия документа**: 1.0  
**Дата создания**: 2024  
**Статус**: Черновик

