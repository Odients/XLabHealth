import { HealthStatus, ServiceType } from '@/types';

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

/**
 * Преобразует строковое, числовое или enum значение типа сервиса в ServiceType enum
 * @param type - тип сервиса в любом формате (строка, число, enum, null, undefined)
 * @returns ServiceType enum
 */
export const parseServiceType = (
  type: string | number | ServiceType | null | undefined
): ServiceType => {
  if (type === null || type === undefined) {
    return ServiceType.Custom;
  }

  // Если это уже число, проверяем валидность
  if (typeof type === 'number') {
    if (type >= 0 && type <= 6) {
      return type as ServiceType;
    }
    return ServiceType.Custom;
  }

  // Преобразуем строку в enum
  if (typeof type === 'string') {
    // Сначала проверяем, является ли это числовой строкой (enum может сериализоваться как "0", "1", "2" и т.д.)
    const numericValue = parseInt(type, 10);
    if (!isNaN(numericValue) && numericValue >= 0 && numericValue <= 6) {
      return numericValue as ServiceType;
    }

    // Затем проверяем точные совпадения (регистрозависимо)
    const exactMatch: Record<string, ServiceType> = {
      'Http': ServiceType.Http,
      'WindowsService': ServiceType.WindowsService,
      'http': ServiceType.Http,
      'https': ServiceType.Http,
      'Https': ServiceType.Http,
      'tcp': ServiceType.Tcp,
      'Tcp': ServiceType.Tcp,
      'database': ServiceType.Database,
      'Database': ServiceType.Database,
      'redis': ServiceType.Redis,
      'Redis': ServiceType.Redis,
      'kafka': ServiceType.Kafka,
      'Kafka': ServiceType.Kafka,
      'custom': ServiceType.Custom,
      'Custom': ServiceType.Custom,
    };

    if (exactMatch[type]) {
      return exactMatch[type];
    }

    // Затем проверяем lowercase версию
    const typeLower = type.toLowerCase();
    const typeMap: Record<string, ServiceType> = {
      'http': ServiceType.Http,
      'https': ServiceType.Http,
      'tcp': ServiceType.Tcp,
      'database': ServiceType.Database,
      'redis': ServiceType.Redis,
      'windowsservice': ServiceType.WindowsService,
      'windows-service': ServiceType.WindowsService,
      'kafka': ServiceType.Kafka,
      'custom': ServiceType.Custom,
    };

    return typeMap[typeLower] ?? ServiceType.Custom;
  }

  // Если это уже ServiceType enum, возвращаем как есть
  return type as ServiceType;
};

/**
 * Получает читаемое название типа сервиса
 * @param type - тип сервиса (enum, число или строка)
 * @returns читаемое название типа сервиса
 */
export const getServiceTypeLabel = (type: string | number | ServiceType | null | undefined): string => {
  const parsedType = parseServiceType(type);
  
  const typeLabels: Record<ServiceType, string> = {
    [ServiceType.Http]: 'HTTP',
    [ServiceType.Tcp]: 'TCP',
    [ServiceType.Database]: 'База данных',
    [ServiceType.Redis]: 'Redis',
    [ServiceType.WindowsService]: 'Windows Service',
    [ServiceType.Kafka]: 'Kafka',
    [ServiceType.Custom]: 'Пользовательский',
  };
  
  return typeLabels[parsedType] || 'Неизвестно';
};

