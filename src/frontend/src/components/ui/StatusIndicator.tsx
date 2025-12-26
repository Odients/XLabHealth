import { HealthStatus } from '@/types';
import './StatusIndicator.css';

interface StatusIndicatorProps {
  status: HealthStatus;
  size?: 'sm' | 'md' | 'lg';
  showLabel?: boolean;
  showIcon?: boolean;
  labelLanguage?: 'ru' | 'en';
}

const StatusIndicator = ({
  status,
  size = 'md',
  showLabel = false,
  showIcon = true,
  labelLanguage = 'ru',
}: StatusIndicatorProps) => {
  const getStatusConfig = () => {
    const labels = {
      ru: {
        [HealthStatus.Healthy]: 'Работает',
        [HealthStatus.Degraded]: 'Деградирован',
        [HealthStatus.Unhealthy]: 'Не работает',
        [HealthStatus.Unknown]: 'Неизвестно',
      },
      en: {
        [HealthStatus.Healthy]: 'Healthy',
        [HealthStatus.Degraded]: 'Degraded',
        [HealthStatus.Unhealthy]: 'Unhealthy',
        [HealthStatus.Unknown]: 'Unknown',
      },
    };

    const configs = {
      [HealthStatus.Healthy]: {
        icon: '✓',
        className: 'status-healthy',
      },
      [HealthStatus.Degraded]: {
        icon: '⚠',
        className: 'status-degraded',
      },
      [HealthStatus.Unhealthy]: {
        icon: '✕',
        className: 'status-unhealthy',
      },
      [HealthStatus.Unknown]: {
        icon: '?',
        className: 'status-unknown',
      },
    };

    const config = configs[status] || configs[HealthStatus.Unknown];
    return {
      ...config,
      label: labels[labelLanguage][status] || labels[labelLanguage][HealthStatus.Unknown],
    };
  };

  const config = getStatusConfig();

  return (
    <div className={`status-indicator ${config.className} size-${size}`}>
      {showIcon && <span className="status-icon">{config.icon}</span>}
      {showLabel && <span className="status-label">{config.label}</span>}
    </div>
  );
};

export default StatusIndicator;

