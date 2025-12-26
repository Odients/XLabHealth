import { AnalyticsDto } from '@/types';
import './AnalyticsKPICards.css';

interface AnalyticsKPICardsProps {
  analytics: AnalyticsDto;
}

const AnalyticsKPICards = ({ analytics }: AnalyticsKPICardsProps) => {
  const { systemStatistics, timeSeries } = analytics;

  const formatDuration = (minutes: number): string => {
    if (minutes < 60) {
      return `${Math.round(minutes)} мин`;
    }
    const hours = Math.floor(minutes / 60);
    const mins = Math.round(minutes % 60);
    if (hours < 24) {
      return `${hours}ч ${mins}мин`;
    }
    const days = Math.floor(hours / 24);
    const hrs = hours % 24;
    return `${days}д ${hrs}ч`;
  };

  // Получаем текущую доступность из последнего значения графика
  const currentUptime = timeSeries.uptimeSeries.length > 0
    ? timeSeries.uptimeSeries[timeSeries.uptimeSeries.length - 1].value
    : systemStatistics.uptimePercentage;

  return (
    <div className="kpi-cards">
      <div className="kpi-card">
        <div className="kpi-card-header">
          <span className="kpi-card-icon">📊</span>
          <span className="kpi-card-title">Доступность системы</span>
        </div>
        <div className="kpi-card-value">
          {currentUptime.toFixed(2)}%
        </div>
        <div className="kpi-card-subtitle">
          Средняя за период: {systemStatistics.uptimePercentage.toFixed(2)}% | 
          Время недоступности: {formatDuration(systemStatistics.totalDowntimeMinutes)}
        </div>
        <div className="kpi-card-hint">
          Система недоступна, когда хотя бы один сервис недоступен. Периоды обслуживания исключены из расчета.
        </div>
      </div>

      <div className="kpi-card">
        <div className="kpi-card-header">
          <span className="kpi-card-icon">⚡</span>
          <span className="kpi-card-title">Среднее время отклика</span>
        </div>
        <div className="kpi-card-value">
          {Math.round(systemStatistics.responseTimeStatistics.average)} мс
        </div>
        <div className="kpi-card-subtitle">
          P95: {Math.round(systemStatistics.responseTimeStatistics.p95)} мс
        </div>
      </div>

      <div className="kpi-card">
        <div className="kpi-card-header">
          <span className="kpi-card-icon">🔍</span>
          <span className="kpi-card-title">Проверок выполнено</span>
        </div>
        <div className="kpi-card-value">
          {systemStatistics.checkStatistics.totalChecks.toLocaleString()}
        </div>
        <div className="kpi-card-subtitle">
          Успешных: {systemStatistics.checkStatistics.successPercentage.toFixed(1)}%
        </div>
      </div>

      <div className="kpi-card">
        <div className="kpi-card-header">
          <span className="kpi-card-icon">⚠️</span>
          <span className="kpi-card-title">Инцидентов</span>
        </div>
        <div className="kpi-card-value">
          {systemStatistics.incidentStatistics.totalIncidents}
        </div>
        <div className="kpi-card-subtitle">
          Критических: {systemStatistics.incidentStatistics.criticalIncidents}
        </div>
      </div>

      <div className="kpi-card">
        <div className="kpi-card-header">
          <span className="kpi-card-icon">✅</span>
          <span className="kpi-card-title">Здоровых проверок</span>
        </div>
        <div className="kpi-card-value">
          {systemStatistics.statusStatistics.healthyCount.toLocaleString()}
        </div>
        <div className="kpi-card-subtitle">
          {systemStatistics.statusStatistics.healthyPercentage.toFixed(1)}% от общего числа
        </div>
      </div>

      <div className="kpi-card">
        <div className="kpi-card-header">
          <span className="kpi-card-icon">❌</span>
          <span className="kpi-card-title">Неудачных проверок</span>
        </div>
        <div className="kpi-card-value">
          {systemStatistics.statusStatistics.unhealthyCount.toLocaleString()}
        </div>
        <div className="kpi-card-subtitle">
          {systemStatistics.statusStatistics.unhealthyPercentage.toFixed(1)}% от общего числа
        </div>
      </div>
    </div>
  );
};

export default AnalyticsKPICards;

