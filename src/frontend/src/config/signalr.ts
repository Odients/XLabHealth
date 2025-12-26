import * as signalR from '@microsoft/signalr';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5021';
export const SIGNALR_HUB_URL =
  import.meta.env.VITE_SIGNALR_HUB_URL || `${API_URL}/hubs/status`;

// Уровни логирования в зависимости от окружения
const LOG_LEVEL = {
  // В development показываем все логи
  DEV: signalR.LogLevel.Information,
  // В production только ошибки и предупреждения
  PROD: signalR.LogLevel.Warning,
} as const;

const isDevelopment = import.meta.env.DEV;
const currentLogLevel = isDevelopment ? LOG_LEVEL.DEV : LOG_LEVEL.PROD;

// Детальный logger для SignalR с форматированием и контекстом
class DetailedLogger implements signalR.ILogger {
  private lastMessageRef: { message: string; count: number; timestamp: number } | null = null;
  private readonly MESSAGE_DEDUP_WINDOW = 5000; // 5 секунд для дедупликации

  private getLogLevelName(logLevel: signalR.LogLevel): string {
    switch (logLevel) {
      case signalR.LogLevel.Trace:
        return '🔍 TRACE';
      case signalR.LogLevel.Debug:
        return '🐛 DEBUG';
      case signalR.LogLevel.Information:
        return 'ℹ️ INFO';
      case signalR.LogLevel.Warning:
        return '⚠️ WARN';
      case signalR.LogLevel.Error:
        return '❌ ERROR';
      case signalR.LogLevel.Critical:
        return '🚨 CRITICAL';
      case signalR.LogLevel.None:
        return '🔇 NONE';
      default:
        return '📝 LOG';
    }
  }

  private formatMessage(logLevel: signalR.LogLevel, message: string): string {
    const timestamp = new Date().toISOString();
    const levelName = this.getLogLevelName(logLevel);
    return `[SignalR] ${timestamp} ${levelName}: ${message}`;
  }

  private shouldSuppress(message: string, logLevel: signalR.LogLevel): boolean {
    // Подавляем повторяющиеся сообщения об ошибках подключения
    const isConnectionError =
      message.includes('Failed to fetch') ||
      message.includes('Failed to complete negotiation') ||
      message.includes('Error from HTTP request');

    if (isConnectionError && (logLevel === signalR.LogLevel.Warning || logLevel === signalR.LogLevel.Error)) {
      const now = Date.now();
      
      if (
        this.lastMessageRef &&
        this.lastMessageRef.message === message &&
        now - this.lastMessageRef.timestamp < this.MESSAGE_DEDUP_WINDOW
      ) {
        this.lastMessageRef.count++;
        // Показываем только первое сообщение, остальные подавляем
        return true;
      } else {
        this.lastMessageRef = {
          message,
          count: 1,
          timestamp: now,
        };
        return false;
      }
    }

    return false;
  }

  log(logLevel: signalR.LogLevel, message: string): void {
    // Фильтруем по минимальному уровню логирования
    if (logLevel < currentLogLevel) {
      return;
    }

    // Подавляем повторяющиеся сообщения
    if (this.shouldSuppress(message, logLevel)) {
      return;
    }

    const formattedMessage = this.formatMessage(logLevel, message);

    // Логируем в зависимости от уровня
    switch (logLevel) {
      case signalR.LogLevel.Critical:
        // Критические ошибки - всегда логируем
        console.error(formattedMessage);
        break;
      case signalR.LogLevel.Error:
        // Ошибки подключения обрабатываются в useSignalR, здесь подавляем
        if (message.includes('Failed to fetch') || message.includes('Failed to complete negotiation')) {
          // В development показываем как debug, в production не показываем
          if (isDevelopment) {
            console.debug(formattedMessage);
          }
        } else {
          // Другие ошибки - всегда логируем
          console.error(formattedMessage);
        }
        break;
      case signalR.LogLevel.Warning:
        // Предупреждения о подключении - только в development
        if (message.includes('Error from HTTP request')) {
          if (isDevelopment) {
            console.debug(formattedMessage);
          }
        } else {
          // Другие предупреждения - логируем всегда
          console.warn(formattedMessage);
        }
        break;
      case signalR.LogLevel.Information:
        // Информационные сообщения - только в development
        if (isDevelopment) {
          console.info(formattedMessage);
        }
        break;
      case signalR.LogLevel.Debug:
      case signalR.LogLevel.Trace:
        // Debug и Trace - только в development
        if (isDevelopment) {
          console.debug(formattedMessage);
        }
        break;
      default:
        // None - не логируем
        break;
    }
  }
}

// Singleton для подключения SignalR - одно подключение на все приложение
let connectionInstance: signalR.HubConnection | null = null;

function createSignalRConnectionInternal(): signalR.HubConnection {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(SIGNALR_HUB_URL, {
      accessTokenFactory: async () => {
        const token = localStorage.getItem('accessToken');
        // Логируем только в development
        if (isDevelopment) {
          console.debug('[SignalR] Access token factory called:', {
            tokenPresent: !!token,
            tokenLength: token?.length || 0,
            url: SIGNALR_HUB_URL,
          });
        }
        return token || '';
      },
      skipNegotiation: false,
      transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
      withCredentials: false, // Отключаем credentials для CORS
    })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (retryContext) => {
        const delay = retryContext.previousRetryCount < 10
          ? 1000 * Math.pow(2, retryContext.previousRetryCount)
          : 60000;

        // Логируем попытки переподключения только в development
        if (isDevelopment) {
          console.debug(
            `[SignalR] Reconnect attempt ${retryContext.previousRetryCount + 1}, next retry in ${delay}ms`
          );
        }

        return delay;
      },
    })
    .configureLogging(new DetailedLogger())
    .build();

  if (isDevelopment) {
    console.info(`[SignalR] Connection created for URL: ${SIGNALR_HUB_URL}`);
  }

  return connection;
}

export function createSignalRConnection(): signalR.HubConnection {
  // Возвращаем существующее подключение, если оно уже создано
  if (connectionInstance) {
    if (isDevelopment) {
      console.debug('[SignalR] Reusing existing connection instance');
    }
    return connectionInstance;
  }

  // Создаем новое подключение только если его еще нет
  connectionInstance = createSignalRConnectionInternal();
  return connectionInstance;
}

export function getSignalRConnection(): signalR.HubConnection | null {
  return connectionInstance;
}

export function resetSignalRConnection(): void {
  if (connectionInstance) {
    if (isDevelopment) {
      console.info('[SignalR] Resetting connection instance');
    }
    connectionInstance.stop().catch(() => {
      // Игнорируем ошибки при остановке
    });
    connectionInstance = null;
  }
}

