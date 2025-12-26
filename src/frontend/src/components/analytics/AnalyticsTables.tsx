import { useState } from 'react';
import { AnalyticsDto, ServiceAnalyticsDto, IncidentDto, HealthStatus, ServiceType } from '@/types';
import { formatDateTimeWithTimezone } from '@/utils/date';
import './AnalyticsTables.css';

interface AnalyticsTablesProps {
  analytics: AnalyticsDto;
}

const AnalyticsTables = ({ analytics }: AnalyticsTablesProps) => {
  const [activeTab, setActiveTab] = useState<'services' | 'incidents' | 'top'>('services');

  const formatSize = (mb: number): string => {
    if (mb < 1024) {
      return `${mb.toFixed(2)} МБ`;
    } else if (mb < 1024 * 1024) {
      return `${(mb / 1024).toFixed(2)} ГБ`;
    } else {
      return `${(mb / (1024 * 1024)).toFixed(2)} ТБ`;
    }
  };

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

  const getStatusLabel = (status: HealthStatus | number | string | null | undefined): string => {
    if (status === null || status === undefined) return 'Неизвестно';
    
    // Нормализуем статус к числу
    let statusNum: number;
    if (typeof status === 'number') {
      statusNum = status;
    } else if (typeof status === 'string') {
      // Пытаемся распарсить строку
      const parsed = parseInt(status, 10);
      if (isNaN(parsed)) {
        // Если не число, пытаемся найти по имени
        const statusMap: Record<string, number> = {
          'healthy': HealthStatus.Healthy,
          'degraded': HealthStatus.Degraded,
          'unhealthy': HealthStatus.Unhealthy,
          'unknown': HealthStatus.Unknown,
          'Healthy': HealthStatus.Healthy,
          'Degraded': HealthStatus.Degraded,
          'Unhealthy': HealthStatus.Unhealthy,
          'Unknown': HealthStatus.Unknown,
        };
        statusNum = statusMap[status.toLowerCase()] ?? HealthStatus.Unknown;
      } else {
        statusNum = parsed;
      }
    } else {
      statusNum = HealthStatus.Unknown;
    }
    
    // Приводим к валидному диапазону
    if (statusNum < 0 || statusNum > 3) {
      statusNum = HealthStatus.Unknown;
    }
    
    switch (statusNum) {
      case HealthStatus.Healthy: return 'Работает';
      case HealthStatus.Degraded: return 'Деградирован';
      case HealthStatus.Unhealthy: return 'Не работает';
      case HealthStatus.Unknown: return 'Неизвестно';
      default: return 'Неизвестно';
    }
  };

  const getServiceTypeLabel = (type: ServiceType | number | string): string => {
    const typeNum = typeof type === 'number' ? type : parseInt(String(type));
    switch (typeNum) {
      case ServiceType.Http: return 'HTTP/HTTPS';
      case ServiceType.Tcp: return 'TCP';
      case ServiceType.Database: return 'База данных';
      case ServiceType.Redis: return 'Redis';
      case ServiceType.WindowsService: return 'Windows Service';
      case ServiceType.Kafka: return 'Kafka';
      case ServiceType.Custom: return 'Пользовательский';
      default: return 'Неизвестно';
    }
  };

  return (
    <div className="analytics-tables">
      <div className="tabs">
        <button
          className={`tab ${activeTab === 'services' ? 'active' : ''}`}
          onClick={() => setActiveTab('services')}
        >
          Все сервисы
        </button>
        <button
          className={`tab ${activeTab === 'incidents' ? 'active' : ''}`}
          onClick={() => setActiveTab('incidents')}
        >
          Инциденты
        </button>
        <button
          className={`tab ${activeTab === 'top' ? 'active' : ''}`}
          onClick={() => setActiveTab('top')}
        >
          Топ сервисы
        </button>
      </div>

      {activeTab === 'services' && (
        <div className="table-container">
          <table className="analytics-table">
            <thead>
              <tr>
                <th>Сервис</th>
                <th>Тип</th>
                <th>Статус</th>
                <th>Доступность</th>
                <th>Ср. время отклика</th>
                <th>Инциденты</th>
                <th>Размер БД</th>
              </tr>
            </thead>
            <tbody>
              {analytics.services.map((service) => (
                <tr key={service.serviceId}>
                  <td>{service.serviceName}</td>
                  <td>{getServiceTypeLabel(service.serviceType)}</td>
                  <td>
                    <span className={`status-badge status-${service.currentStatus ?? 'unknown'}`}>
                      {getStatusLabel(service.currentStatus)}
                    </span>
                  </td>
                  <td>{service.uptimePercentage.toFixed(2)}%</td>
                  <td>{Math.round(service.responseTimeStatistics.average)} мс</td>
                  <td>{service.incidentCount}</td>
                  <td>
                    {service.databaseSizeMetrics
                      ? formatSize(service.databaseSizeMetrics.totalSizeMB)
                      : '-'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {activeTab === 'incidents' && (
        <div className="table-container">
          <table className="analytics-table">
            <thead>
              <tr>
                <th>Сервис</th>
                <th>Начало</th>
                <th>Окончание</th>
                <th>Длительность</th>
                <th>Статус до</th>
                <th>Статус после</th>
                <th>Причина</th>
              </tr>
            </thead>
            <tbody>
              {analytics.incidents.map((incident) => (
                <tr key={incident.id} className={incident.isCritical ? 'critical' : ''}>
                  <td>{incident.serviceName}</td>
                  <td>{formatDateTimeWithTimezone(incident.startTime)}</td>
                  <td>
                    {incident.endTime
                      ? formatDateTimeWithTimezone(incident.endTime)
                      : 'В процессе'}
                  </td>
                  <td>{formatDuration(incident.durationMinutes)}</td>
                  <td>
                    <span className={`status-badge status-${incident.statusBefore}`}>
                      {getStatusLabel(incident.statusBefore)}
                    </span>
                  </td>
                  <td>
                    <span className={`status-badge status-${incident.statusAfter}`}>
                      {getStatusLabel(incident.statusAfter)}
                    </span>
                  </td>
                  <td className="reason-cell" title={incident.reason}>
                    {incident.reason ? (incident.reason.length > 50
                      ? `${incident.reason.substring(0, 50)}...`
                      : incident.reason) : '-'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {activeTab === 'top' && (
        <div className="top-services">
          <div className="top-section">
            <h3>Топ-10 по доступности</h3>
            <table className="analytics-table">
              <thead>
                <tr>
                  <th>Сервис</th>
                  <th>Доступность</th>
                  <th>Инциденты</th>
                </tr>
              </thead>
              <tbody>
                {analytics.topServices.topByUptime.map((service) => (
                  <tr key={service.serviceId}>
                    <td>{service.serviceName}</td>
                    <td>{service.uptimePercentage.toFixed(2)}%</td>
                    <td>{service.incidentCount}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="top-section">
            <h3>Топ-10 по времени отклика</h3>
            <table className="analytics-table">
              <thead>
                <tr>
                  <th>Сервис</th>
                  <th>Ср. время отклика</th>
                  <th>P95</th>
                </tr>
              </thead>
              <tbody>
                {analytics.topServices.topByResponseTime.map((service) => (
                  <tr key={service.serviceId}>
                    <td>{service.serviceName}</td>
                    <td>{Math.round(service.responseTimeStatistics.average)} мс</td>
                    <td>{Math.round(service.responseTimeStatistics.p95)} мс</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {analytics.topServices.topDatabaseBySize.length > 0 && (
            <div className="top-section">
              <h3>Топ-10 Database по размеру</h3>
              <table className="analytics-table">
                <thead>
                  <tr>
                    <th>Сервис</th>
                    <th>Размер</th>
                    <th>Использование</th>
                    <th>Изменение</th>
                  </tr>
                </thead>
                <tbody>
                  {analytics.topServices.topDatabaseBySize.map((service) => (
                    <tr key={service.serviceId}>
                      <td>{service.serviceName}</td>
                      <td>
                        {service.databaseSizeMetrics
                          ? formatSize(service.databaseSizeMetrics.totalSizeMB)
                          : '-'}
                      </td>
                      <td>
                        {service.databaseSizeMetrics
                          ? `${service.databaseSizeMetrics.usagePercentage.toFixed(1)}%`
                          : '-'}
                      </td>
                      <td>
                        {service.databaseSizeMetrics?.sizeChangeMB
                          ? `${service.databaseSizeMetrics.sizeChangeMB > 0 ? '+' : ''}${formatSize(service.databaseSizeMetrics.sizeChangeMB)}`
                          : '-'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default AnalyticsTables;

