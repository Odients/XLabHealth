import { useEffect } from 'react';
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
  message = 'Сервис временно недоступен. Пожалуйста, попробуйте позже.',
  onRetry,
  autoRetry = true,
  autoRetryInterval = 10000 // По умолчанию каждые 10 секунд
}: BackendUnavailableProps) => {
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
        <div className="backend-unavailable-message">{message}</div>
        {autoRetry && (
          <div className="backend-unavailable-auto-retry">
            Автоматическая попытка переподключения каждые {autoRetryInterval / 1000} сек...
          </div>
        )}
        {onRetry && (
          <button 
            className="backend-unavailable-retry" 
            onClick={onRetry}
          >
            Попробовать снова
          </button>
        )}
      </div>
    </div>
  );
};

export default BackendUnavailable;

