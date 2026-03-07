import { useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { servicesApi } from '@/services/api';
import { useServiceStatusUpdates } from '@/hooks/useSignalR';
import StatusIndicator from '@/components/ui/StatusIndicator';
import BackendUnavailable from '@/components/ui/BackendUnavailable';
import { formatRelativeTime, formatDateTime } from '@/utils/date';
import { isBackendUnavailable } from '@/utils/backend';
import { parseHealthStatus, getServiceTypeLabel } from '@/utils/status';
import { ServiceStatusChangedEvent } from '@/types';
import './ServiceDetailPage.css';

const ServiceDetailPage = () => {
  const { id } = useParams<{ id: string }>();

  const { data: service, isLoading, error, refetch } = useQuery({
    queryKey: ['service', id],
    queryFn: () => servicesApi.getById(id!),
    enabled: !!id,
  });

  const { data: history, error: historyError, refetch: refetchHistory } = useQuery({
    queryKey: ['service-history', id],
    queryFn: () => servicesApi.getHistory(id!),
    enabled: !!id,
  });

  // Подписка на обновления через SignalR для конкретного сервиса
  useServiceStatusUpdates((event: ServiceStatusChangedEvent) => {
    // Обновляем данные, если событие относится к текущему сервису
    if (event.serviceId === id) {
      refetch();
      refetchHistory();
    }
  });

  // Проверяем, недоступен ли бэкенд
  const backendUnavailable = 
    (error && isBackendUnavailable(error)) ||
    (historyError && isBackendUnavailable(historyError));

  if (isLoading) {
    return (
      <div className="service-detail-page">
        <div className="loading">Загрузка...</div>
      </div>
    );
  }

  // Если бэкенд недоступен, показываем нейтральное сообщение
  if (backendUnavailable) {
    return (
      <div className="service-detail-page">
        <BackendUnavailable onRetry={() => {
          refetch();
          refetchHistory();
        }} />
      </div>
    );
  }

  if (!service) {
    return (
      <div className="service-detail-page">
        <div className="error">Сервис не найден</div>
      </div>
    );
  }

  // Парсим статус для правильного отображения
  const status = parseHealthStatus(service.lastStatus);

  return (
    <div className="service-detail-page">
      <div className="service-header">
        <div>
          <h1>{service.name}</h1>
          {service.description && <p className="service-description">{service.description}</p>}
        </div>
        <StatusIndicator status={status} size="lg" showLabel />
      </div>

      <div className="service-info-grid">
        <div className="info-card">
          <h3>Информация о сервисе</h3>
          <div className="info-item">
            <span className="info-label">URL:</span>
            <span className="info-value">{service.url}</span>
          </div>
          <div className="info-item">
            <span className="info-label">Тип:</span>
            <span className="info-value">{getServiceTypeLabel(service.type)}</span>
          </div>
          <div className="info-item">
            <span className="info-label">Интервал проверки:</span>
            <span className="info-value">{service.checkInterval} сек</span>
          </div>
          <div className="info-item">
            <span className="info-label">Таймаут:</span>
            <span className="info-value">{service.timeout} мс</span>
          </div>
          <div className="info-item">
            <span className="info-label">Повторы:</span>
            <span className="info-value">{service.retryCount}</span>
          </div>
          <div className="info-item">
            <span className="info-label">Включен:</span>
            <span className="info-value">{service.isEnabled ? 'Да' : 'Нет'}</span>
          </div>
          <div className="info-item">
            <span className="info-label">Публичный:</span>
            <span className="info-value">{service.isPublic ? 'Да' : 'Нет'}</span>
          </div>
          <div className="info-item">
            <span className="info-label">Критичный:</span>
            <span className="info-value">{service.isCritical ?? false ? 'Да' : 'Нет'}</span>
          </div>
        </div>

        <div className="info-card">
          <h3>Текущий статус</h3>
          {service.lastCheckedAt ? (
            <>
              <div className="info-item">
                <span className="info-label">Последняя проверка:</span>
                <span className="info-value">
                  {formatRelativeTime(service.lastCheckedAt)}
                </span>
              </div>
              <div className="info-item">
                <span className="info-label">Время:</span>
                <span className="info-value">
                  {formatDateTime(service.lastCheckedAt)}
                </span>
              </div>
            </>
          ) : (
            <div className="info-item">
              <span className="info-value">Никогда не проверялся</span>
            </div>
          )}
        </div>
      </div>

      {history && history.length > 0 && (
        <div className="history-section">
          <h2>История проверок</h2>
          <div className="history-list">
            {history.slice(0, 20).map((result) => {
              const historyStatus = parseHealthStatus(result.status);
              return (
                <div key={result.id} className="history-item">
                  <StatusIndicator status={historyStatus} size="sm" />
                  <div className="history-info">
                    <span className="history-time">
                      {new Date(result.checkedAt).toLocaleString('ru-RU')}
                    </span>
                    {result.responseTime && (
                      <span className="history-response-time">
                        {result.responseTime} мс
                      </span>
                    )}
                    {result.message && (
                      <span className="history-message">{result.message}</span>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
};

export default ServiceDetailPage;

