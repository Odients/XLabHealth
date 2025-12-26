import { HealthStatus } from '@/types';

/**
 * Преобразует строковое, числовое или enum значение статуса в HealthStatus enum
 * @param status - статус в любом формате (строка, число, enum, null, undefined)
 * @returns HealthStatus enum
 */
export const parseHealthStatus = (
  status: string | number | HealthStatus | null | undefined
): HealthStatus => {
  if (status === null || status === undefined) {
    return HealthStatus.Unknown;
  }

  // Если это уже число, проверяем валидность
  if (typeof status === 'number') {
    if (status >= 0 && status <= 3) {
      return status as HealthStatus;
    }
    return HealthStatus.Unknown;
  }

  // Преобразуем строку в enum
  if (typeof status === 'string') {
    const statusMap: Record<string, HealthStatus> = {
      'healthy': HealthStatus.Healthy,
      'degraded': HealthStatus.Degraded,
      'unhealthy': HealthStatus.Unhealthy,
      'unknown': HealthStatus.Unknown,
      // Также поддерживаем PascalCase
      'Healthy': HealthStatus.Healthy,
      'Degraded': HealthStatus.Degraded,
      'Unhealthy': HealthStatus.Unhealthy,
      'Unknown': HealthStatus.Unknown,
    };

    return statusMap[status.toLowerCase()] ?? HealthStatus.Unknown;
  }

  // Если это уже HealthStatus enum, возвращаем как есть
  return status as HealthStatus;
};

