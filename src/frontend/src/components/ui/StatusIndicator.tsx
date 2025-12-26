import { useTranslation } from 'react-i18next';
import { HealthStatus } from '@/types';
import './StatusIndicator.css';

interface StatusIndicatorProps {
  status: HealthStatus;
  size?: 'sm' | 'md' | 'lg';
  showLabel?: boolean;
  showIcon?: boolean;
}

const StatusIndicator = ({
  status,
  size = 'md',
  showLabel = false,
  showIcon = true,
}: StatusIndicatorProps) => {
  const { t } = useTranslation();

  const getStatusConfig = () => {
    const statusLabels = {
      [HealthStatus.Healthy]: t('public.status.healthy'),
      [HealthStatus.Degraded]: t('public.status.degraded'),
      [HealthStatus.Unhealthy]: t('public.status.unhealthy'),
      [HealthStatus.Unknown]: t('public.status.unknown'),
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
      label: statusLabels[status] || statusLabels[HealthStatus.Unknown],
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

