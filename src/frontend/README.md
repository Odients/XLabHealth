# X-Lab Status Service - Frontend

Frontend приложение для сервиса мониторинга здоровья сервисов X-Lab Status Service.

## Технологии

- **React 18+** с TypeScript
- **Vite** - сборщик
- **React Router** - маршрутизация
- **TanStack Query (React Query)** - управление серверным состоянием
- **Zustand** - управление глобальным состоянием
- **SignalR** - real-time обновления
- **React Hook Form** + **Zod** - формы и валидация
- **date-fns** - работа с датами
- **react-toastify** - уведомления

## Установка

```bash
cd src/frontend
npm install
```

## Разработка

```bash
npm run dev
```

Приложение будет доступно по адресу `http://localhost:3000`

## Сборка

```bash
npm run build
```

Собранные файлы будут в папке `dist/`

## Переменные окружения

Создайте файл `.env` в корне проекта `src/frontend/`:

```env
VITE_API_URL=http://localhost:5021
VITE_FRONTEND_URL=https://localhost:7146
VITE_GOOGLE_TAG_ID=GTM-XXXXXXX
```

**Примечание**: `VITE_GOOGLE_TAG_ID` - опциональная переменная. Если не указана, Google Tag Manager не будет инициализирован.

## Структура проекта

```
src/
├── components/          # Переиспользуемые компоненты
│   ├── layout/        # Layout компоненты
│   ├── routing/       # Компоненты роутинга
│   └── ui/            # UI компоненты
├── config/            # Конфигурация (API, SignalR)
├── hooks/             # Custom hooks
├── pages/             # Страницы приложения
├── services/          # API сервисы
├── store/             # Zustand store
├── styles/            # Глобальные стили
└── types/             # TypeScript типы
```

## Маршруты

- `/` - Публичный dashboard (без авторизации)
- `/login` - Страница входа
- `/dashboard` - Приватный dashboard (требует авторизации)
- `/services/:id` - Детальная информация о сервисе (требует авторизации)
- `/admin` - Администрирование (требует роль Admin)

## Деплой на Vercel

1. Подключите репозиторий к Vercel
2. Настройте переменные окружения в Vercel:
   - `VITE_API_URL` - URL API бэкенда
3. Vercel автоматически соберет и задеплоит приложение

Подробнее см. `docs/TZ-Frontend.md`

