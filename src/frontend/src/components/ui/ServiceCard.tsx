import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { formatRelativeTime } from '@/utils/date';
import { parseHealthStatus, parseServiceType, getServiceTypeLabel } from '@/utils/status';
import StatusIndicator from './StatusIndicator';
import { ServiceType, HealthStatus } from '@/types';
import type { PublicServiceDto, ServiceDto } from '@/types';
import './ServiceCard.css';

interface ServiceCardProps {
  service: PublicServiceDto | ServiceDto;
  clickable?: boolean;
}

const ServiceCard = ({ service, clickable = true }: ServiceCardProps) => {
  const { t } = useTranslation();

  const getLastCheckedText = () => {
    if (!service.lastCheckedAt) return t('public.serviceCard.neverChecked');
    return formatRelativeTime(service.lastCheckedAt);
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
    // Получаем значение type напрямую из объекта и парсим его
    const typeValue = (fullService as unknown as Record<string, unknown>).type;
    if (typeValue !== undefined && typeValue !== null) {
      serviceType = parseServiceType(typeValue as string | number | ServiceType | null | undefined);
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
            <span className="service-card-url-label">{t('public.serviceCard.url')}:</span>
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
              <span className="service-card-meta-label">{t('public.serviceCard.interval')}:</span>
              <span className="service-card-meta-value">{(service as ServiceDto).checkInterval}с</span>
            </div>
            {(service as ServiceDto).isEnabled !== undefined && (
              <div className="service-card-meta-item">
                <span className={`service-card-badge ${(service as ServiceDto).isEnabled ? 'enabled' : 'disabled'}`}>
                  {(service as ServiceDto).isEnabled ? t('public.serviceCard.enabled') : t('public.serviceCard.disabled')}
                </span>
              </div>
            )}
          </div>
        )}

        <div className="service-card-footer">
          <span className="service-card-time">
            <span className="service-card-time-icon">🕐</span>
            {t('public.serviceCard.updated')} {getLastCheckedText()}
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

