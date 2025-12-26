# Руководство по развертыванию X-Lab Status Service

## Подготовка к продакшн развертыванию

### 1. Переменные окружения

Все секретные данные должны быть настроены через переменные окружения или через `appsettings.Production.json` (который не должен попадать в репозиторий).

#### Обязательные переменные окружения для Production:

```bash
# База данных
ConnectionStrings__DefaultConnection="Data Source=...;Initial Catalog=XLabHealth;..."
ConnectionStrings__Quartz="Data Source=...;Initial Catalog=XLabHealth;..."

# JWT
Jwt__SecretKey="<минимум 32 символа, используйте криптографически стойкий ключ>"
Jwt__Issuer="XLabStatusService"
Jwt__Audience="XLabStatusService"

# CORS (разделенные запятыми)
Cors__AllowedOrigins__0="https://status.x-lab.by"
Cors__AllowedOrigins__1="https://admin.x-lab.by"
```

#### Альтернатива: appsettings.Production.json

Создайте файл `appsettings.Production.json` в папке `src/backend/XLabStatusService.Api/` со следующим содержимым:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=...;Initial Catalog=XLabHealth;...",
    "Quartz": "Data Source=...;Initial Catalog=XLabHealth;..."
  },
  "Jwt": {
    "SecretKey": "<минимум 32 символа>",
    "Issuer": "XLabStatusService",
    "Audience": "XLabStatusService"
  },
  "Cors": {
    "AllowedOrigins": [
      "https://status.x-lab.by",
      "https://admin.x-lab.by"
    ]
  }
}
```

**ВАЖНО**: Файл `appsettings.Production.json` уже добавлен в `.gitignore` и не будет попадать в репозиторий.

### 2. Генерация JWT SecretKey

Для генерации безопасного JWT SecretKey используйте один из следующих методов:

#### PowerShell (Windows):
```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

#### Linux/Mac:
```bash
openssl rand -base64 32
```

#### .NET:
```csharp
var key = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
```

### 3. Настройка базы данных

#### 3.1. Создание базы данных

Убедитесь, что база данных `XLabHealth` создана на SQL Server.

#### 3.2. Применение миграций

Выполните миграции Entity Framework Core:

```bash
cd src/backend/XLabStatusService.Api
dotnet ef database update --project ../XLabStatusService.Infrastructure
```

Или используйте команду из корня решения:

```bash
dotnet ef database update --project src/backend/XLabStatusService.Infrastructure --startup-project src/backend/XLabStatusService.Api
```

### 4. Настройка Quartz.NET

Quartz.NET автоматически создаст необходимые таблицы в схеме `[quartz]` при первом запуске приложения, если они еще не существуют.

Убедитесь, что:
- Схема `[quartz]` существует в базе данных
- Пользователь БД имеет права на создание таблиц в этой схеме
- `Clustered: true` в `appsettings.Production.json` для кластеризации (если планируется несколько экземпляров)

### 5. Настройка логирования

Логирование настроено через Serilog и записывается в:
- Консоль (для Docker/Kubernetes)
- Файлы в папке `logs/` (ротация ежедневно, хранение 30 дней)

Убедитесь, что:
- Папка `logs/` существует и приложение имеет права на запись
- Для Docker: логи выводятся в консоль (stdout/stderr)

### 6. Развертывание

#### 6.1. Сборка для Production

```bash
cd src/backend/XLabStatusService.Api
dotnet publish -c Release -o ./publish
```

#### 6.2. Запуск приложения

```bash
cd publish
export ASPNETCORE_ENVIRONMENT=Production
dotnet XLabStatusService.Api.dll
```

#### 6.3. Windows Service (опционально)

Для запуска как Windows Service используйте инструменты вроде NSSM или создайте собственный сервис.

#### 6.4. Docker (опционально)

Создайте `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/backend/XLabStatusService.Api/XLabStatusService.Api.csproj", "src/backend/XLabStatusService.Api/"]
COPY ["src/backend/XLabStatusService.Application/XLabStatusService.Application.csproj", "src/backend/XLabStatusService.Application/"]
COPY ["src/backend/XLabStatusService.Infrastructure/XLabStatusService.Infrastructure.csproj", "src/backend/XLabStatusService.Infrastructure/"]
COPY ["src/backend/XLabStatusService.Core/XLabStatusService.Core.csproj", "src/backend/XLabStatusService.Core/"]
RUN dotnet restore "src/backend/XLabStatusService.Api/XLabStatusService.Api.csproj"
COPY . .
WORKDIR "/src/src/backend/XLabStatusService.Api"
RUN dotnet build "XLabStatusService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "XLabStatusService.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "XLabStatusService.Api.dll"]
```

### 7. Проверка работоспособности

#### 7.1. Health Check

Проверьте endpoint здоровья:

```bash
curl https://your-domain.com/api/health
```

Ожидаемый ответ:
```json
{
  "status": "healthy",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

#### 7.2. Проверка Swagger

Swagger должен быть **отключен** в Production. Убедитесь, что доступ к `/swagger` возвращает 404.

#### 7.3. Проверка аутентификации

Проверьте, что приватные endpoints требуют JWT токен:

```bash
# Должен вернуть 401 Unauthorized
curl https://your-domain.com/api/services

# С токеном должен работать
curl -H "Authorization: Bearer YOUR_TOKEN" https://your-domain.com/api/services
```

### 8. Безопасность

#### 8.1. HTTPS

Убедитесь, что:
- Приложение работает только через HTTPS в Production
- HTTP редиректит на HTTPS (настроено в `Program.cs`)

#### 8.2. CORS

Настройте CORS только для разрешенных доменов:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://status.x-lab.by",
      "https://admin.x-lab.by"
    ]
  }
}
```

**НЕ используйте** `"*"` для `AllowedOrigins` в Production!

#### 8.3. Секреты

- Никогда не коммитьте `appsettings.Production.json` в репозиторий
- Используйте переменные окружения или секретные хранилища (Azure Key Vault, AWS Secrets Manager и т.д.)
- JWT SecretKey должен быть минимум 32 символа
- Регулярно ротируйте секретные ключи

### 9. Мониторинг

#### 9.1. Логи

Логи записываются в:
- Консоль (для Docker/Kubernetes)
- Файлы `logs/xlab-status-service-YYYY-MM-DD.log`

#### 9.2. Метрики

Настройте мониторинг для:
- Health check endpoint (`/api/health`)
- Время отклика API
- Использование памяти и CPU
- Количество активных подключений SignalR

### 10. Резервное копирование

Регулярно создавайте резервные копии:
- База данных `XLabHealth`
- Конфигурационные файлы (без секретов)
- Логи (опционально)

### 11. Обновление приложения

#### 11.1. Процесс обновления

1. Остановите приложение
2. Создайте резервную копию базы данных
3. Примените новые миграции (если есть):
   ```bash
   dotnet ef database update --project src/backend/XLabStatusService.Infrastructure --startup-project src/backend/XLabStatusService.Api
   ```
4. Разверните новую версию приложения
5. Запустите приложение
6. Проверьте работоспособность через health check

#### 11.2. Откат

Если что-то пошло не так:
1. Остановите приложение
2. Восстановите предыдущую версию
3. Восстановите базу данных из резервной копии (если миграции были применены)
4. Запустите приложение

### 12. Troubleshooting

#### Проблема: Приложение не запускается

**Решение:**
- Проверьте переменные окружения
- Проверьте логи в `logs/` или консоли
- Убедитесь, что база данных доступна
- Проверьте права доступа к папке `logs/`

#### Проблема: Ошибка "JWT SecretKey is not configured"

**Решение:**
- Убедитесь, что `Jwt:SecretKey` установлен в переменных окружения или `appsettings.Production.json`
- Проверьте, что ключ не пустой и не равен значению по умолчанию

#### Проблема: CORS ошибки

**Решение:**
- Проверьте настройки `Cors:AllowedOrigins` в конфигурации
- Убедитесь, что фронтенд домен добавлен в список разрешенных
- Проверьте, что используется HTTPS для фронтенда

#### Проблема: Quartz jobs не выполняются

**Решение:**
- Проверьте подключение к базе данных для Quartz
- Убедитесь, что таблицы Quartz созданы в схеме `[quartz]`
- Проверьте логи на наличие ошибок Quartz

### 13. Контрольный список перед деплоем

- [ ] Создан `appsettings.Production.json` с правильными настройками
- [ ] JWT SecretKey сгенерирован и установлен (минимум 32 символа)
- [ ] Строки подключения к БД настроены
- [ ] CORS настроен для продакшн доменов
- [ ] База данных создана и миграции применены
- [ ] Quartz таблицы созданы (автоматически при первом запуске)
- [ ] Папка `logs/` создана и доступна для записи
- [ ] Swagger отключен в Production (проверено)
- [ ] HTTPS настроен
- [ ] Health check endpoint работает
- [ ] Аутентификация работает
- [ ] Логирование работает
- [ ] Резервное копирование настроено

### 14. Контакты и поддержка

При возникновении проблем:
1. Проверьте логи в `logs/` или консоли
2. Проверьте документацию в `docs/TZ-Backend.md`
3. Обратитесь к команде разработки

