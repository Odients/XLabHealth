import { Link } from 'react-router-dom';
import { formatRelativeTime } from '@/utils/date';
import { parseHealthStatus } from '@/utils/status';
import StatusIndicator from './StatusIndicator';
import { ServiceType, HealthStatus } from '@/types';
import type { PublicServiceDto, ServiceDto } from '@/types';
import './ServiceCard.css';

interface ServiceCardProps {
  service: PublicServiceDto | ServiceDto;
  clickable?: boolean;
}

const ServiceCard = ({ service, clickable = true }: ServiceCardProps) => {
  const getLastCheckedText = () => {
    if (!service.lastCheckedAt) return 'Никогда не проверялся';
    return formatRelativeTime(service.lastCheckedAt);
  };

  const getServiceTypeLabel = (type: ServiceType): string => {
    const typeLabels: Record<ServiceType, string> = {
      [ServiceType.Http]: 'HTTP',
      [ServiceType.Tcp]: 'TCP',
      [ServiceType.Database]: 'База данных',
      [ServiceType.Redis]: 'Redis',
      [ServiceType.WindowsService]: 'Windows Service',
      [ServiceType.Kafka]: 'Kafka',
      [ServiceType.Custom]: 'Пользовательский',
    };
    return typeLabels[type] || 'Неизвестно';
  };

  const getServiceTypeIcon = (type: ServiceType): string => {
    const typeIcons: Record<ServiceType, string> = {
      [ServiceType.Http]: '🌐',
      [ServiceType.Tcp]: '🔌',
      [ServiceType.Database]: '💾',
      [ServiceType.Redis]: '⚡',
      [ServiceType.WindowsService]: '⚙️',
      [ServiceType.Kafka]: '📨',
      [ServiceType.Custom]: '🔧',
    };
    return typeIcons[type] || '❓';
  };

  // Определяем статус: для PublicServiceDto используется status, для ServiceDto - lastStatus
  const rawStatus = 'status' in service 
    ? service.status 
    : (service.lastStatus ?? HealthStatus.Unknown);
  
  // Преобразуем статус в правильный формат
  const status = parseHealthStatus(rawStatus);
  
  // Проверяем, является ли сервис полным (ServiceDto) или публичным (PublicServiceDto)
  // ServiceDto имеет поле 'type' и 'checkInterval', а PublicServiceDto - нет
  const isFullService = 'checkInterval' in service || 'type' in service;
  
  // Получаем тип сервиса, если он доступен (только для ServiceDto)
  let serviceType: ServiceType | null = null;
  if (isFullService) {
    const fullService = service as ServiceDto;
    // Получаем значение type напрямую из объекта
    const typeValue = (fullService as unknown as Record<string, unknown>).type;
    
    // Обрабатываем разные форматы: число (0, 1, 2...) или строка ("http", "tcp", ...)
    if (typeValue !== undefined && typeValue !== null) {
      if (typeof typeValue === 'number') {
        // Если это число, используем напрямую
        serviceType = typeValue as ServiceType;
      } else if (typeof typeValue === 'string') {
        // Если это строка (camelCase enum), конвертируем в число
        const stringValue = typeValue.toLowerCase();
        // Маппинг строковых значений enum (camelCase) в числовые значения
        const enumMap: Record<string, ServiceType> = {
          'http': ServiceType.Http,
          'tcp': ServiceType.Tcp,
          'database': ServiceType.Database,
          'redis': ServiceType.Redis,
          'windowsservice': ServiceType.WindowsService, // после toLowerCase()
          'kafka': ServiceType.Kafka,
          'custom': ServiceType.Custom,
        };
        serviceType = enumMap[stringValue] ?? null;
      }
    }
  }

  const cardContent = (
    <>
      <div className="service-card-status-bar" data-status={String(status)}></div>
      
      <div className="service-card-content">
        <div className="service-card-header">
          <div className="service-card-title-wrapper">
            {serviceType !== null && (
              <span className="service-card-type-icon" title={getServiceTypeLabel(serviceType)}>
                {getServiceTypeIcon(serviceType)}
              </span>
            )}
            <h3 className="service-card-title">{service.name}</h3>
          </div>
          <StatusIndicator status={status} size="sm" showLabel />
        </div>

        {isFullService && (service as ServiceDto).description && (
          <p className="service-card-description">{(service as ServiceDto).description}</p>
        )}

        {isFullService && (service as ServiceDto).url && (
          <div className="service-card-url">
            <span className="service-card-url-label">URL:</span>
            <code className="service-card-url-value" title={(service as ServiceDto).url}>
              {(service as ServiceDto).url.length > 40 
                ? `${(service as ServiceDto).url.substring(0, 40)}...` 
                : (service as ServiceDto).url}
            </code>
          </div>
        )}

        {isFullService && (
          <div className="service-card-meta">
            <div className="service-card-meta-item">
              <span className="service-card-meta-label">Интервал:</span>
              <span className="service-card-meta-value">{(service as ServiceDto).checkInterval}с</span>
            </div>
            {(service as ServiceDto).isEnabled !== undefined && (
              <div className="service-card-meta-item">
                <span className={`service-card-badge ${(service as ServiceDto).isEnabled ? 'enabled' : 'disabled'}`}>
                  {(service as ServiceDto).isEnabled ? 'Включен' : 'Выключен'}
                </span>
              </div>
            )}
          </div>
        )}

        <div className="service-card-footer">
          <span className="service-card-time">
            <span className="service-card-time-icon">🕐</span>
            Обновлено {getLastCheckedText()}
          </span>
        </div>
      </div>
    </>
  );

  if (clickable) {
    return (
      <Link
        to={`/services/${service.id}`}
        className="service-card"
      >
        {cardContent}
      </Link>
    );
  }

  return (
    <div className="service-card service-card-non-clickable">
      {cardContent}
    </div>
  );
};

export default ServiceCard;

