/**
 * Утилиты для работы с IP-адресами
 */

/**
 * Получить IP-адрес клиента через внешний API
 * @returns IP-адрес клиента или null в случае ошибки
 */
export const getClientIpAddress = async (): Promise<string | null> => {
  try {
    // Используем несколько сервисов для надежности
    const services = [
      'https://api.ipify.org?format=json',
      'https://api64.ipify.org?format=json',
      'https://ipapi.co/json/',
      'http://ip-api.com/json/?fields=status,message,query',
    ];

    for (const service of services) {
      try {
        const response = await fetch(service, {
          method: 'GET',
          headers: {
            'Accept': 'application/json',
          },
          // Таймаут для запроса
          signal: AbortSignal.timeout(3000),
        });

        if (!response.ok) {
          continue;
        }

        const data = await response.json();
        
        // ip-api.com возвращает { status: "success", query: "..." } или { status: "fail", message: "..." }
        if (service.includes('ip-api.com')) {
          if (data.status === 'success' && data.query) {
            return data.query;
          }
          // Если status !== "success", пропускаем этот сервис
          continue;
        }
        
        // ipify возвращает { ip: "..." }
        if (data.ip) {
          return data.ip;
        }
        
        // ipapi.co возвращает { ip: "..." }
        if (data.ip) {
          return data.ip;
        }
      } catch (error) {
        // Игнорируем ошибки и пробуем следующий сервис
        // Не логируем каждую ошибку, так как это ожидаемое поведение при fallback
        continue;
      }
    }

    // Логируем только если все сервисы не сработали
    console.warn('Failed to get IP from all services. This is non-critical.');
    return null;
  } catch (error) {
    // Критическая ошибка - логируем только если это не ожидаемая ошибка сети
    if (error instanceof Error && error.name !== 'AbortError' && error.name !== 'TypeError') {
      console.error('Failed to get client IP address:', error);
    }
    return null;
  }
};

