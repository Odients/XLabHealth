/**
 * Утилита для получения IP-адреса клиента
 * Использует внешний API для определения IP, так как браузер не предоставляет прямой доступ к IP клиента
 */

const IP_CACHE_KEY = 'client_ip_address';
const IP_CACHE_EXPIRY = 1000 * 60 * 60; // 1 час

interface IpCache {
  ip: string;
  timestamp: number;
}

/**
 * Получить IP-адрес клиента
 * Сначала проверяет кэш, затем использует внешний API
 */
export async function getClientIp(): Promise<string | null> {
  // Проверяем кэш
  const cached = getCachedIp();
  if (cached) {
    return cached;
  }

  try {
    // Используем несколько сервисов для надежности
    const ipServices = [
      'https://api.ipify.org?format=json',
      'https://ipapi.co/json/',
      'https://api.myip.com',
    ];

    for (const serviceUrl of ipServices) {
      try {
        const response = await fetch(serviceUrl, {
          method: 'GET',
          headers: {
            'Accept': 'application/json',
          },
        });

        if (response.ok) {
          const data = await response.json();
          // Разные сервисы возвращают IP в разных полях
          const ip = data.ip || data.query || data.address;
          
          if (ip && isValidIp(ip)) {
            // Сохраняем в кэш
            cacheIp(ip);
            return ip;
          }
        }
      } catch (error) {
        // Пробуем следующий сервис
        console.warn(`Failed to get IP from ${serviceUrl}:`, error);
        continue;
      }
    }

    return null;
  } catch (error) {
    console.error('Failed to get client IP:', error);
    return null;
  }
}

/**
 * Получить IP из кэша
 */
function getCachedIp(): string | null {
  try {
    const cached = localStorage.getItem(IP_CACHE_KEY);
    if (!cached) {
      return null;
    }

    const cache: IpCache = JSON.parse(cached);
    const now = Date.now();

    // Проверяем, не истек ли кэш
    if (now - cache.timestamp > IP_CACHE_EXPIRY) {
      localStorage.removeItem(IP_CACHE_KEY);
      return null;
    }

    return cache.ip;
  } catch {
    return null;
  }
}

/**
 * Сохранить IP в кэш
 */
function cacheIp(ip: string): void {
  try {
    const cache: IpCache = {
      ip,
      timestamp: Date.now(),
    };
    localStorage.setItem(IP_CACHE_KEY, JSON.stringify(cache));
  } catch (error) {
    console.warn('Failed to cache IP:', error);
  }
}

/**
 * Проверить, является ли строка валидным IP-адресом
 */
function isValidIp(ip: string): boolean {
  // Простая проверка IPv4
  const ipv4Regex = /^(\d{1,3}\.){3}\d{1,3}$/;
  // Простая проверка IPv6
  const ipv6Regex = /^([0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}$/;
  
  return ipv4Regex.test(ip) || ipv6Regex.test(ip);
}

/**
 * Очистить кэш IP
 */
export function clearIpCache(): void {
  localStorage.removeItem(IP_CACHE_KEY);
}

