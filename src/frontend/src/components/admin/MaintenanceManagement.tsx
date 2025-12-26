import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { maintenanceApi } from '@/services/api';
import { toast } from 'react-toastify';
import type { MaintenanceModeDto, MaintenanceModeEnableDto } from '@/types';
import './MaintenanceManagement.css';

const DEFAULT_MAINTENANCE_MESSAGE =
  'Система находится в режиме обслуживания. Пожалуйста, попробуйте позже.';

const MaintenanceManagement = () => {
  const queryClient = useQueryClient();
  const [isEnableModalOpen, setIsEnableModalOpen] = useState(false);
  const [formData, setFormData] = useState<MaintenanceModeEnableDto>({
    message: DEFAULT_MAINTENANCE_MESSAGE,
    scheduledStartTime: '',
    scheduledEndTime: '',
  });

  const { data: maintenanceStatus, isLoading } = useQuery({
    queryKey: ['maintenance-status'],
    queryFn: maintenanceApi.getStatus,
  });

  const enableMutation = useMutation({
    mutationFn: maintenanceApi.enable,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['maintenance-status'] });
      toast.success('Режим обслуживания включен');
      setIsEnableModalOpen(false);
      resetForm();
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.error || 'Ошибка при включении режима обслуживания');
    },
  });

  const disableMutation = useMutation({
    mutationFn: maintenanceApi.disable,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['maintenance-status'] });
      toast.success('Режим обслуживания выключен');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.error || 'Ошибка при выключении режима обслуживания');
    },
  });

  const resetForm = () => {
    setFormData({
      message: DEFAULT_MAINTENANCE_MESSAGE,
      scheduledStartTime: '',
      scheduledEndTime: '',
    });
  };

  const handleEnable = () => {
    const enableData: MaintenanceModeEnableDto = {
      message: formData.message?.trim() || DEFAULT_MAINTENANCE_MESSAGE,
      scheduledStartTime: formData.scheduledStartTime
        ? new Date(formData.scheduledStartTime).toISOString()
        : undefined,
      scheduledEndTime: formData.scheduledEndTime
        ? new Date(formData.scheduledEndTime).toISOString()
        : undefined,
    };
    enableMutation.mutate(enableData);
  };

  const handleDisable = () => {
    if (window.confirm('Вы уверены, что хотите выключить режим обслуживания?')) {
      disableMutation.mutate();
    }
  };

  const formatDateTime = (dateString?: string) => {
    if (!dateString) return '-';
    return new Date(dateString).toLocaleString('ru-RU');
  };

  if (isLoading) {
    return (
      <div className="maintenance-management">
        <div className="loading">Загрузка...</div>
      </div>
    );
  }

  const isEnabled = maintenanceStatus?.isEnabled ?? false;

  return (
    <div className="maintenance-management">
      <div className="maintenance-header">
        <h2>Управление режимом обслуживания</h2>
        {!isEnabled ? (
          <button className="btn-primary" onClick={() => setIsEnableModalOpen(true)}>
            Включить режим обслуживания
          </button>
        ) : (
          <button className="btn-danger" onClick={handleDisable}>
            Выключить режим обслуживания
          </button>
        )}
      </div>

      <div className="maintenance-content">
        <div className={`maintenance-status ${isEnabled ? 'enabled' : 'disabled'}`}>
          <div className="status-indicator">
            <div className={`status-dot ${isEnabled ? 'active' : ''}`}></div>
            <span className="status-text">
              {isEnabled ? 'Режим обслуживания активен' : 'Режим обслуживания неактивен'}
            </span>
          </div>
        </div>

        {isEnabled && maintenanceStatus && (
          <div className="maintenance-details">
            <h3>Информация о режиме обслуживания</h3>
            <div className="details-grid">
              <div className="detail-item">
                <label>Сообщение:</label>
                <p>{maintenanceStatus.message || '-'}</p>
              </div>
              <div className="detail-item">
                <label>Запланированное время начала:</label>
                <p>{formatDateTime(maintenanceStatus.scheduledStartTime)}</p>
              </div>
              <div className="detail-item">
                <label>Запланированное время окончания:</label>
                <p>{formatDateTime(maintenanceStatus.scheduledEndTime)}</p>
              </div>
              <div className="detail-item">
                <label>Время начала:</label>
                <p>{formatDateTime(maintenanceStatus.startedAt)}</p>
              </div>
              <div className="detail-item">
                <label>Время окончания:</label>
                <p>{formatDateTime(maintenanceStatus.endedAt)}</p>
              </div>
              <div className="detail-item">
                <label>Создано:</label>
                <p>{formatDateTime(maintenanceStatus.createdAt)}</p>
              </div>
              <div className="detail-item">
                <label>Обновлено:</label>
                <p>{formatDateTime(maintenanceStatus.updatedAt)}</p>
              </div>
            </div>
          </div>
        )}
      </div>

      {isEnableModalOpen && (
        <div className="modal-overlay" onClick={() => setIsEnableModalOpen(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <h2>Включить режим обслуживания</h2>
            <div className="form-group">
              <label htmlFor="message">Сообщение для пользователей</label>
              <textarea
                id="message"
                rows={4}
                value={formData.message}
                onChange={(e) => setFormData({ ...formData, message: e.target.value })}
                placeholder={DEFAULT_MAINTENANCE_MESSAGE}
              />
              <small>Если оставить пустым, будет использовано сообщение по умолчанию</small>
            </div>
            <div className="form-group">
              <label htmlFor="scheduledStartTime">Запланированное время начала</label>
              <input
                id="scheduledStartTime"
                type="datetime-local"
                value={formData.scheduledStartTime}
                onChange={(e) =>
                  setFormData({ ...formData, scheduledStartTime: e.target.value })
                }
              />
              <small>Оставьте пустым для немедленного включения</small>
            </div>
            <div className="form-group">
              <label htmlFor="scheduledEndTime">Запланированное время окончания</label>
              <input
                id="scheduledEndTime"
                type="datetime-local"
                value={formData.scheduledEndTime}
                onChange={(e) =>
                  setFormData({ ...formData, scheduledEndTime: e.target.value })
                }
              />
              <small>Оставьте пустым для ручного выключения</small>
            </div>
            <div className="modal-actions">
              <button className="btn-secondary" onClick={() => setIsEnableModalOpen(false)}>
                Отмена
              </button>
              <button
                className="btn-primary"
                onClick={handleEnable}
                disabled={enableMutation.isPending}
              >
                Включить
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default MaintenanceManagement;

