import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { analyticsApi } from '@/services/api';
import { AnalyticsDto, ServiceStatusChangedEvent } from '@/types';
import { useServiceStatusUpdates } from '@/hooks/useSignalR';
import BackendUnavailable from '@/components/ui/BackendUnavailable';
import AnalyticsKPICards from '@/components/analytics/AnalyticsKPICards';
import AnalyticsCharts from '@/components/analytics/AnalyticsCharts';
import AnalyticsDatabaseCharts from '@/components/analytics/AnalyticsDatabaseCharts';
import AnalyticsTables from '@/components/analytics/AnalyticsTables';
import { isBackendUnavailable } from '@/utils/backend';
import './AnalyticsPage.css';

const AnalyticsPage = () => {
  const [period, setPeriod] = useState<string>('7d');

  const { data: analytics, isLoading, error, refetch } = useQuery<AnalyticsDto>({
    queryKey: ['analytics', period],
    queryFn: () => analyticsApi.getAnalytics(period),
  });

  // Подписка на обновления через SignalR
  useServiceStatusUpdates((event: ServiceStatusChangedEvent) => {
    // Обновляем аналитику при изменении статуса любого сервиса
    refetch();
  });

  const handlePeriodChange = (newPeriod: string) => {
    setPeriod(newPeriod);
  };

  // Проверяем, недоступен ли бэкенд
  const backendUnavailable = error && isBackendUnavailable(error);

  if (isLoading) {
    return (
      <div className="analytics-page">
        <div className="analytics-loading">Загрузка аналитики...</div>
      </div>
    );
  }

  // Если бэкенд недоступен, показываем нейтральное сообщение
  if (backendUnavailable) {
    return (
      <div className="analytics-page">
        <BackendUnavailable onRetry={() => refetch()} />
      </div>
    );
  }

  if (error) {
    return (
      <div className="analytics-page">
        <div className="analytics-error">Ошибка при загрузке аналитики</div>
      </div>
    );
  }

  if (!analytics) {
    return (
      <div className="analytics-page">
        <div className="analytics-error">Данные не найдены</div>
      </div>
    );
  }

  return (
    <div className="analytics-page">
      <div className="analytics-header">
        <div>
          <h1>Аналитика</h1>
          <p style={{ fontSize: '0.875rem', color: '#6b7280', marginTop: '0.5rem' }}>
            Доступность системы рассчитывается по принципу: система недоступна, когда хотя бы один сервис недоступен. 
            Периоды обслуживания исключены из расчета доступности и времени недоступности.
          </p>
        </div>
        <div className="analytics-period-selector">
          <button
            className={`period-btn ${period === '24h' ? 'active' : ''}`}
            onClick={() => handlePeriodChange('24h')}
          >
            24 часа
          </button>
          <button
            className={`period-btn ${period === '7d' ? 'active' : ''}`}
            onClick={() => handlePeriodChange('7d')}
          >
            7 дней
          </button>
          <button
            className={`period-btn ${period === '1y' ? 'active' : ''}`}
            onClick={() => handlePeriodChange('1y')}
          >
            1 год
          </button>
        </div>
      </div>

      <AnalyticsKPICards analytics={analytics} />
      <AnalyticsCharts analytics={analytics} period={period} />
      <AnalyticsDatabaseCharts analytics={analytics} period={period} />
      <AnalyticsTables analytics={analytics} />
    </div>
  );
};

export default AnalyticsPage;

