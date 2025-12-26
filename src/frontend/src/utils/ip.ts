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
        console.warn(`Failed to get IP from ${service}:`, error);
        continue;
      }
    }

    return null;
  } catch (error) {
    console.error('Failed to get client IP address:', error);
    return null;
  }
};

