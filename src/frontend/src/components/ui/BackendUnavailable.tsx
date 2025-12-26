import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import './BackendUnavailable.css';

interface BackendUnavailableProps {
  message?: string;
  onRetry?: () => void;
  autoRetry?: boolean; // Автоматическая попытка переподключения
  autoRetryInterval?: number; // Интервал автоматической попытки в миллисекундах
}

/**
 * Компонент для отображения нейтрального сообщения о недоступности бэкенда
 */
const BackendUnavailable = ({ 
  message,
  onRetry,
  autoRetry = true,
  autoRetryInterval = 10000 // По умолчанию каждые 10 секунд
}: BackendUnavailableProps) => {
  const { t } = useTranslation();
  const defaultMessage = message || t('public.backendUnavailable.message');
  // Автоматическая попытка переподключения
  useEffect(() => {
    if (autoRetry && onRetry) {
      const interval = setInterval(() => {
        onRetry();
      }, autoRetryInterval);

      return () => clearInterval(interval);
    }
  }, [autoRetry, onRetry, autoRetryInterval]);

  return (
    <div className="backend-unavailable">
      <div className="backend-unavailable-content">
        <div className="backend-unavailable-icon">⚠️</div>
        <div className="backend-unavailable-message">{defaultMessage}</div>
        {autoRetry && (
          <div className="backend-unavailable-auto-retry">
            {t('public.backendUnavailable.autoRetry', { seconds: autoRetryInterval / 1000 })}
          </div>
        )}
        {onRetry && (
          <button 
            className="backend-unavailable-retry" 
            onClick={onRetry}
          >
            {t('public.backendUnavailable.retry')}
          </button>
        )}
      </div>
    </div>
  );
};

export default BackendUnavailable;

