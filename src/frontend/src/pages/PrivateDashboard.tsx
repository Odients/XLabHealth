import { useQuery } from '@tanstack/react-query';
import { useState, useMemo } from 'react';
import { servicesApi } from '@/services/api';
import { useServiceStatusUpdates } from '@/hooks/useSignalR';
import ServiceCard from '@/components/ui/ServiceCard';
import StatusIndicator from '@/components/ui/StatusIndicator';
import BackendUnavailable from '@/components/ui/BackendUnavailable';
import { HealthStatus } from '@/types';
import { parseHealthStatus } from '@/utils/status';
import { isBackendUnavailable } from '@/utils/backend';
import './PrivateDashboard.css';

const PrivateDashboard = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<HealthStatus | 'all'>('all');
  const [criticalFilter, setCriticalFilter] = useState<'all' | 'critical' | 'non-critical'>('all');

  const { data: services, isLoading, error, refetch } = useQuery({
    queryKey: ['services'],
    queryFn: servicesApi.getAll,
  });

  // Подписка на обновления через SignalR
  useServiceStatusUpdates(() => {
    // Обновляем данные при получении события от SignalR
    refetch();
  });

  // Вычисляем статистику с правильным парсингом статусов
  const stats = useMemo(() => {
    if (!services || services.length === 0) {
      return {
        total: 0,
        healthy: 0,
        degraded: 0,
        unhealthy: 0,
        unknown: 0,
      };
    }

    let healthy = 0;
    let degraded = 0;
    let unhealthy = 0;
    let unknown = 0;

    services.forEach((service) => {
      const status = parseHealthStatus(service.lastStatus);
      switch (status) {
        case HealthStatus.Healthy:
          healthy++;
          break;
        case HealthStatus.Degraded:
          degraded++;
          break;
        case HealthStatus.Unhealthy:
          unhealthy++;
          break;
        case HealthStatus.Unknown:
        default:
          unknown++;
          break;
      }
    });

    const criticalCount = services.filter((s) => s.isCritical ?? false).length;

    return {
      total: services.length,
      healthy,
      degraded,
      unhealthy,
      unknown,
      criticalCount,
    };
  }, [services]);

  // Вычисляем общий статус системы с учётом критичности
  // Логика: если хотя бы один критический сервис не работает — вся система не работает
  // Если не работают только некритичные — система ограниченно функционирует
  const systemStatus = useMemo(() => {
    if (!services || services.length === 0) {
      return HealthStatus.Unknown;
    }

    let hasUnhealthyCritical = false;
    let hasDegradedCritical = false;
    let hasUnhealthyOrDegradedNonCritical = false;

    services.forEach((service) => {
      const status = parseHealthStatus(service.lastStatus);
      const isCritical = service.isCritical ?? false;

      switch (status) {
        case HealthStatus.Unhealthy:
        case HealthStatus.Unknown:
          if (isCritical) hasUnhealthyCritical = true;
          else hasUnhealthyOrDegradedNonCritical = true;
          break;
        case HealthStatus.Degraded:
          if (isCritical) hasDegradedCritical = true;
          else hasUnhealthyOrDegradedNonCritical = true;
          break;
        default:
          break;
      }
    });

    if (hasUnhealthyCritical) return HealthStatus.Unhealthy;
    if (hasDegradedCritical) return HealthStatus.Degraded;
    if (hasUnhealthyOrDegradedNonCritical) return HealthStatus.Degraded;
    return HealthStatus.Healthy;
  }, [services]);

  const filteredServices = services?.filter((service) => {
    const matchesSearch =
      service.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      service.description?.toLowerCase().includes(searchTerm.toLowerCase());

    const isCritical = service.isCritical ?? false;
    const matchesCritical =
      criticalFilter === 'all' ||
      (criticalFilter === 'critical' && isCritical) ||
      (criticalFilter === 'non-critical' && !isCritical);

    if (statusFilter === 'all') {
      return matchesSearch && matchesCritical;
    }

    const serviceStatus = parseHealthStatus(service.lastStatus);
    const matchesStatus = serviceStatus === statusFilter;
    return matchesSearch && matchesStatus && matchesCritical;
  });

  // Проверяем, недоступен ли бэкенд
  const backendUnavailable = error && isBackendUnavailable(error);

  if (isLoading) {
    return (
      <div className="private-dashboard">
        <div className="loading">Загрузка...</div>
      </div>
    );
  }

  // Если бэкенд недоступен, показываем нейтральное сообщение
  if (backendUnavailable) {
    return (
      <div className="private-dashboard">
        <BackendUnavailable onRetry={() => refetch()} />
      </div>
    );
  }

  return (
    <div className="private-dashboard">
      <div className="dashboard-header">
        <div className="dashboard-header-content">
          <h1>Dashboard</h1>
          <button onClick={() => refetch()} className="btn-refresh">
            Обновить
          </button>
        </div>
        {services && services.length > 0 && (
          <div className="overall-status">
            <StatusIndicator status={systemStatus} size="lg" showLabel />
          </div>
        )}
      </div>

      <div className="stats-grid">
        <div className="stat-card">
          <div className="stat-value">{stats.total}</div>
          <div className="stat-label">Всего сервисов</div>
        </div>
        <div className="stat-card healthy">
          <div className="stat-value">{stats.healthy}</div>
          <div className="stat-label">Работают</div>
        </div>
        <div className="stat-card degraded">
          <div className="stat-value">{stats.degraded}</div>
          <div className="stat-label">Деградированы</div>
        </div>
        <div className="stat-card unhealthy">
          <div className="stat-value">{stats.unhealthy}</div>
          <div className="stat-label">Проблемы</div>
        </div>
        <div className="stat-card unknown">
          <div className="stat-value">{stats.unknown}</div>
          <div className="stat-label">Неизвестно</div>
        </div>
        <div className="stat-card critical">
          <div className="stat-value">{stats.criticalCount}</div>
          <div className="stat-label">Критичных</div>
        </div>
      </div>

      <div className="filters-section">
        <div className="search-box">
          <input
            type="text"
            placeholder="Поиск сервисов..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="search-input"
          />
        </div>
        <div className="critical-filters">
          <button
            className={`filter-btn ${criticalFilter === 'all' ? 'active' : ''}`}
            onClick={() => setCriticalFilter('all')}
          >
            Все
          </button>
          <button
            className={`filter-btn ${criticalFilter === 'critical' ? 'active' : ''}`}
            onClick={() => setCriticalFilter('critical')}
          >
            ⚠ Критичные
          </button>
          <button
            className={`filter-btn ${criticalFilter === 'non-critical' ? 'active' : ''}`}
            onClick={() => setCriticalFilter('non-critical')}
          >
            Некритичные
          </button>
        </div>
        <div className="status-filters">
          <button
            className={`filter-btn ${statusFilter === 'all' ? 'active' : ''}`}
            onClick={() => setStatusFilter('all')}
          >
            Все
          </button>
          <button
            className={`filter-btn ${statusFilter === HealthStatus.Healthy ? 'active' : ''}`}
            onClick={() => setStatusFilter(HealthStatus.Healthy)}
          >
            <StatusIndicator status={HealthStatus.Healthy} size="sm" />
            Работают
          </button>
          <button
            className={`filter-btn ${statusFilter === HealthStatus.Degraded ? 'active' : ''}`}
            onClick={() => setStatusFilter(HealthStatus.Degraded)}
          >
            <StatusIndicator status={HealthStatus.Degraded} size="sm" />
            Деградированы
          </button>
          <button
            className={`filter-btn ${statusFilter === HealthStatus.Unhealthy ? 'active' : ''}`}
            onClick={() => setStatusFilter(HealthStatus.Unhealthy)}
          >
            <StatusIndicator status={HealthStatus.Unhealthy} size="sm" />
            Проблемы
          </button>
        </div>
      </div>

      <div className="services-section">
        {filteredServices && filteredServices.length > 0 ? (
          <div className="services-grid">
            {filteredServices.map((service) => (
              <ServiceCard key={service.id} service={service} />
            ))}
          </div>
        ) : (
          <div className="no-services">
            {searchTerm || statusFilter !== 'all'
              ? 'Сервисы не найдены'
              : 'Нет доступных сервисов'}
          </div>
        )}
      </div>
    </div>
  );
};

export default PrivateDashboard;

