import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useState, useEffect } from 'react';
import { publicApi } from '@/services/api';
import { useAuthStore } from '@/store/authStore';
import StatusIndicator from '@/components/ui/StatusIndicator';
import ServiceCard from '@/components/ui/ServiceCard';
import BackendUnavailable from '@/components/ui/BackendUnavailable';
import { HealthStatus } from '@/types';
import { parseHealthStatus } from '@/utils/status';
import { getClientIpAddress } from '@/utils/ip';
import { isBackendUnavailable } from '@/utils/backend';
import './PublicDashboard.css';

const PublicDashboard = () => {
  const { isAuthenticated } = useAuthStore();
  const navigate = useNavigate();
  const [clientIp, setClientIp] = useState<string | null>(null);

  // Получаем IP-адрес клиента при загрузке компонента
  useEffect(() => {
    getClientIpAddress().then(setClientIp);
  }, []);

  const { 
    data: status, 
    isLoading: statusLoading, 
    error: statusError,
    refetch: refetchStatus 
  } = useQuery({
    queryKey: ['public-status'],
    queryFn: publicApi.getStatus,
    refetchInterval: 10000, // Обновление каждые 10 секунд
  });

  const { 
    data: services, 
    isLoading: servicesLoading,
    error: servicesError,
    refetch: refetchServices 
  } = useQuery({
    queryKey: ['public-services'],
    queryFn: publicApi.getServices,
    refetchInterval: 10000, // Обновление каждые 10 секунд
  });

  const { data: ipStatus } = useQuery({
    queryKey: ['ip-status', clientIp],
    queryFn: () => {
      if (!clientIp) {
        throw new Error('IP address not available');
      }
      return publicApi.getIpStatus(clientIp);
    },
    enabled: !!clientIp, // Запрос выполняется только когда IP определен
    refetchInterval: 10000, // Обновление каждые 10 секунд
    retry: false, // Не повторять запрос при ошибке
  });

  // Проверяем, недоступен ли бэкенд
  const backendUnavailable = 
    (statusError && isBackendUnavailable(statusError)) ||
    (servicesError && isBackendUnavailable(servicesError));

  if (statusLoading || servicesLoading) {
    return (
      <div className="public-dashboard">
        <div className="container">
          <div className="loading">Загрузка...</div>
        </div>
      </div>
    );
  }

  // Если бэкенд недоступен, показываем нейтральное сообщение
  if (backendUnavailable) {
    return (
      <div className="public-dashboard">
        <div className="container">
          <BackendUnavailable 
            onRetry={() => {
              refetchStatus();
              refetchServices();
            }}
          />
        </div>
      </div>
    );
  }

  // Преобразуем статус в правильный формат
  const systemStatus = status ? parseHealthStatus(status.status) : HealthStatus.Unknown;

  return (
    <div className="public-dashboard">
      <div className="container">
        {ipStatus?.isBlocked && (
          <div className="ip-blocked-warning">
            <div className="ip-blocked-warning-content">
              <span className="ip-blocked-icon">⚠️</span>
              <div className="ip-blocked-text">
                <strong>Внимание:</strong> Ваш IP-адрес ({ipStatus.ipAddress || 'неизвестен'}) находится в списке заблокированных.
                {ipStatus.blockedDate && (
                  <span className="ip-blocked-date">
                    {' '}Дата блокировки: {new Date(ipStatus.blockedDate).toLocaleString('ru-RU')}
                  </span>
                )}
              </div>
            </div>
          </div>
        )}
        
        <div className="dashboard-header">
          <div className="dashboard-header-content">
            <h1>Статус системы</h1>
            {isAuthenticated && (
              <button
                onClick={() => navigate('/dashboard')}
                className="btn-dashboard-link"
                title="Перейти в приватный дашборд"
              >
                Приватный дашборд
              </button>
            )}
          </div>
          {status && (
            <div className="overall-status">
              <StatusIndicator status={systemStatus} size="lg" showLabel />
            </div>
          )}
        </div>

        {status && (
          <div className="metrics-grid">
            <div className="metric-card">
              <div className="metric-value">{status.totalServices}</div>
              <div className="metric-label">Всего сервисов</div>
            </div>
            <div className="metric-card healthy">
              <div className="metric-value">{status.healthyServices}</div>
              <div className="metric-label">Работают</div>
            </div>
            <div className="metric-card degraded">
              <div className="metric-value">{status.degradedServices}</div>
              <div className="metric-label">Деградированы</div>
            </div>
            <div className="metric-card unhealthy">
              <div className="metric-value">{status.unhealthyServices}</div>
              <div className="metric-label">Проблемы</div>
            </div>
            <div className="metric-card">
              <div className="metric-value">
                {status.availabilityPercentage.toFixed(1)}%
              </div>
              <div className="metric-label">Доступность</div>
            </div>
          </div>
        )}

        {status?.lastUpdated && (
          <div className="last-updated">
            Последнее обновление:{' '}
            {new Date(status.lastUpdated).toLocaleString('ru-RU')}
          </div>
        )}

        <div className="services-section">
          <h2>Сервисы</h2>
          {services && services.length > 0 ? (
            <div className="services-grid">
              {services.map((service) => (
                <ServiceCard key={service.id} service={service} />
              ))}
            </div>
          ) : (
            <div className="no-services">Нет доступных сервисов</div>
          )}
        </div>
      </div>
    </div>
  );
};

export default PublicDashboard;

