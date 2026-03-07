import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { servicesApi } from '@/services/api';
import { toast } from 'react-toastify';
import StatusIndicator from '@/components/ui/StatusIndicator';
import ServiceForm from './ServiceForm';
import { ServiceType, ServiceDto, ServiceCreateDto, ServiceUpdateDto, HealthStatus } from '@/types';
import { formatDateTimeWithTimezone } from '@/utils/date';
import { parseServiceType, getServiceTypeLabel } from '@/utils/status';
import './ServicesManagement.css';

const ServicesManagement = () => {
  const queryClient = useQueryClient();
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [editingService, setEditingService] = useState<ServiceDto | null>(null);

  const { data: services, isLoading } = useQuery({
    queryKey: ['services'],
    queryFn: servicesApi.getAll,
  });

  const createMutation = useMutation({
    mutationFn: servicesApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] });
      toast.success('Сервис создан');
      setIsCreateModalOpen(false);
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.error || 'Ошибка при создании сервиса');
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: ServiceUpdateDto }) =>
      servicesApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] });
      toast.success('Сервис обновлен');
      setEditingService(null);
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.error || 'Ошибка при обновлении сервиса');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: servicesApi.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] });
      toast.success('Сервис удален');
    },
    onError: () => {
      toast.error('Ошибка при удалении сервиса');
    },
  });

  const checkMutation = useMutation({
    mutationFn: servicesApi.check,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] });
      toast.success('Проверка сервиса завершена');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.error || 'Ошибка при проверке сервиса');
    },
  });

  const checkAllMutation = useMutation({
    mutationFn: servicesApi.checkAll,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] });
      toast.success('Проверка всех сервисов запущена');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.error || 'Ошибка при проверке сервисов');
    },
  });

  const handleCreate = (data: ServiceCreateDto) => {
    createMutation.mutate(data);
  };

  const handleUpdate = (data: ServiceUpdateDto) => {
    if (!editingService) return;
    updateMutation.mutate({ id: editingService.id, data });
  };

  const handleSubmit = (data: ServiceCreateDto | ServiceUpdateDto) => {
    if (editingService) {
      handleUpdate(data as ServiceUpdateDto);
    } else {
      handleCreate(data as ServiceCreateDto);
    }
  };

  const handleEdit = (service: ServiceDto) => {
    setEditingService(service);
  };

  const handleCancel = () => {
    setIsCreateModalOpen(false);
    setEditingService(null);
  };

  const handleDelete = (id: string) => {
    if (window.confirm('Вы уверены, что хотите удалить этот сервис?')) {
      deleteMutation.mutate(id);
    }
  };

  const handleCheck = (id: string) => {
    checkMutation.mutate(id);
  };

  const handleCheckAll = () => {
    if (window.confirm('Запустить проверку всех сервисов?')) {
      checkAllMutation.mutate();
    }
  };

  // Маппинг строковых значений статуса в enum (локальная версия для обратной совместимости)
  const parseHealthStatusLocal = (status: string | number | null | undefined): HealthStatus => {
    if (status === null || status === undefined) {
      return HealthStatus.Unknown;
    }
    
    // Если это уже число, проверяем валидность
    if (typeof status === 'number') {
      if (status >= 0 && status <= 3) {
        return status as HealthStatus;
      }
      return HealthStatus.Unknown;
    }
    
    // Преобразуем строку в enum
    const statusMap: Record<string, HealthStatus> = {
      'healthy': HealthStatus.Healthy,
      'degraded': HealthStatus.Degraded,
      'unhealthy': HealthStatus.Unhealthy,
      'unknown': HealthStatus.Unknown,
      // Также поддерживаем PascalCase
      'Healthy': HealthStatus.Healthy,
      'Degraded': HealthStatus.Degraded,
      'Unhealthy': HealthStatus.Unhealthy,
      'Unknown': HealthStatus.Unknown,
    };
    
    return statusMap[status.toLowerCase()] ?? HealthStatus.Unknown;
  };

  const getServiceTypeIcon = (type: ServiceType | number | string | null | undefined): string => {
    // Нормализуем тип к числу
    const normalizedType = parseServiceType(type);
    
    const typeIcons: Record<ServiceType, string> = {
      [ServiceType.Http]: '🌐',
      [ServiceType.Tcp]: '🔌',
      [ServiceType.Database]: '💾',
      [ServiceType.Redis]: '⚡',
      [ServiceType.WindowsService]: '⚙️',
      [ServiceType.Kafka]: '📨',
      [ServiceType.Custom]: '🔧',
    };
    return typeIcons[normalizedType] ?? '❓';
  };

  if (isLoading) {
    return (
      <div className="services-management">
        <div className="loading">Загрузка...</div>
      </div>
    );
  }

  return (
    <div className="services-management">
      <div className="services-header">
        <h2>Управление сервисами</h2>
        <div className="services-header-actions">
          <button
            className="btn-check-all"
            onClick={handleCheckAll}
            disabled={checkAllMutation.isPending || !services || services.length === 0}
            title="Запустить проверку всех сервисов"
          >
            <span className="btn-check-all-icon">
              {checkAllMutation.isPending ? '⏳' : '🔄'}
            </span>
            <span className="btn-check-all-text">
              {checkAllMutation.isPending ? 'Проверка...' : 'Проверить все'}
            </span>
            {services && services.length > 0 && (
              <span className="btn-check-all-count">({services.length})</span>
            )}
          </button>
          <button className="btn-primary" onClick={() => setIsCreateModalOpen(true)}>
            <span>➕</span>
            <span>Добавить сервис</span>
          </button>
        </div>
      </div>

      <div className="services-content">
        {services && services.length > 0 ? (
          <div className="services-grid">
            {services.map((service) => {
              // Преобразуем тип сервиса - может прийти как строка или число
              const serviceType = parseServiceType(service.type);
              
              // Преобразуем статус - может прийти как строка или число
              const status = parseHealthStatusLocal(service.lastStatus);
              
              return (
                <div key={service.id} className="service-admin-card">
                  <div 
                    className="service-admin-card-status-bar" 
                    data-status={String(status)}
                  ></div>
                  
                  <div className="service-admin-card-content">
                    <div className="service-admin-card-header">
                      <div className="service-admin-card-title-wrapper">
                        <span className="service-admin-card-type-icon" title={getServiceTypeLabel(serviceType)}>
                          {getServiceTypeIcon(serviceType)}
                        </span>
                        <h3 className="service-admin-card-title">{service.name}</h3>
                      </div>
                      <StatusIndicator status={status} size="sm" showLabel />
                    </div>

                    {service.description && (
                      <p className="service-admin-card-description">{service.description}</p>
                    )}

                    <div className="service-admin-card-url">
                      <span className="service-admin-card-url-label">URL:</span>
                      <code className="service-admin-card-url-value" title={service.url}>
                        {service.url.length > 50 ? `${service.url.substring(0, 50)}...` : service.url}
                      </code>
                    </div>

                    <div className="service-admin-card-meta">
                      <div className="service-admin-card-meta-item">
                        <span className="service-admin-card-meta-label">Тип:</span>
                        <span className="service-admin-card-meta-value">{getServiceTypeLabel(serviceType)}</span>
                      </div>
                      <div className="service-admin-card-meta-item">
                        <span className="service-admin-card-meta-label">Интервал:</span>
                        <span className="service-admin-card-meta-value">{service.checkInterval}с</span>
                      </div>
                      {service.timeout && (
                        <div className="service-admin-card-meta-item">
                          <span className="service-admin-card-meta-label">Таймаут:</span>
                          <span className="service-admin-card-meta-value">{service.timeout}мс</span>
                        </div>
                      )}
                      {service.retryCount !== undefined && service.retryCount > 0 && (
                        <div className="service-admin-card-meta-item">
                          <span className="service-admin-card-meta-label">Повторы:</span>
                          <span className="service-admin-card-meta-value">{service.retryCount}</span>
                        </div>
                      )}
                    </div>

                    <div className="service-admin-card-badges">
                      <span className={`service-admin-card-badge ${service.isEnabled ? 'enabled' : 'disabled'}`}>
                        {service.isEnabled ? '✓ Включен' : '✕ Выключен'}
                      </span>
                      <span className={`service-admin-card-badge ${service.isPublic ? 'public' : 'private'}`}>
                        {service.isPublic ? '✓ Публичный' : '✕ Приватный'}
                      </span>
                      {(service.isCritical ?? false) && (
                        <span className="service-admin-card-badge critical">
                          ⚠ Критичный
                        </span>
                      )}
                    </div>

                    <div className="service-admin-card-footer">
                      <div className="service-admin-card-time">
                        <span className="service-admin-card-time-icon">🕐</span>
                        {service.lastCheckedAt ? (
                          <span>{formatDateTimeWithTimezone(service.lastCheckedAt)}</span>
                        ) : (
                          <span className="text-muted">Никогда не проверялся</span>
                        )}
                      </div>
                    </div>

                    <div className="service-admin-card-actions">
                      <button
                        className="btn-check"
                        onClick={() => handleCheck(service.id)}
                        disabled={checkMutation.isPending}
                        title="Запустить проверку"
                      >
                        {checkMutation.isPending ? '⏳' : '🔄'}
                      </button>
                      <button
                        className="btn-edit"
                        onClick={() => handleEdit(service)}
                        title="Редактировать сервис"
                      >
                        ✏️
                      </button>
                      <button
                        className="btn-delete"
                        onClick={() => handleDelete(service.id)}
                        title="Удалить сервис"
                      >
                        🗑️
                      </button>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        ) : (
          <div className="no-services">Нет сервисов</div>
        )}
      </div>

      {(isCreateModalOpen || editingService) && (
        <div className="modal-overlay" onClick={handleCancel}>
          <div className="modal-content modal-content-large" onClick={(e) => e.stopPropagation()}>
            <h2>{editingService ? 'Редактировать сервис' : 'Добавить сервис'}</h2>
            <ServiceForm
              service={editingService || undefined}
              onSubmit={handleSubmit}
              onCancel={handleCancel}
              isLoading={createMutation.isPending || updateMutation.isPending}
            />
          </div>
        </div>
      )}
    </div>
  );
};

export default ServicesManagement;

