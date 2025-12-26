import { useEffect, useState, useRef } from 'react';
import { publicApi } from '@/services/api';
import { isBackendUnavailable } from '@/utils/backend';

// Глобальные переменные для управления одним экземпляром проверки
let globalBackendAvailable: boolean = false;
let healthListeners: Set<(available: boolean) => void> = new Set();
let healthCheckInterval: ReturnType<typeof setInterval> | null = null;
let isHealthCheckInitialized = false;

const HEALTH_CHECK_INTERVAL = 5000; // Проверяем каждые 5 секунд
const HEALTH_CHECK_TIMEOUT = 3000; // Таймаут для проверки - 3 секунды

/**
 * Хук для проверки доступности бэкенда
 * Использует глобальное состояние для избежания множественных проверок
 */
export const useBackendHealth = () => {
  const [backendAvailable, setBackendAvailable] = useState<boolean>(globalBackendAvailable);

  useEffect(() => {
    // Добавляем слушатель изменений состояния
    const updateState = (available: boolean) => {
      globalBackendAvailable = available;
      healthListeners.forEach((listener) => listener(available));
    };

    healthListeners.add(setBackendAvailable);

    // Инициализируем проверку только один раз
    if (!isHealthCheckInitialized) {
      isHealthCheckInitialized = true;

      const checkBackendHealth = async () => {
        try {
          // Используем Promise.race для таймаута
          const timeoutPromise = new Promise<never>((_, reject) => {
            setTimeout(() => reject(new Error('Health check timeout')), HEALTH_CHECK_TIMEOUT);
          });

          const healthPromise = publicApi.getStatus();

          await Promise.race([healthPromise, timeoutPromise]);

          // Если запрос успешен, бэкенд доступен
          if (globalBackendAvailable !== true) {
            updateState(true);
          }
        } catch (error) {
          // Проверяем, является ли ошибка следствием недоступности бэкенда
          const unavailable = isBackendUnavailable(error);
          
          if (unavailable) {
            // Бэкенд недоступен
            if (globalBackendAvailable !== false) {
              updateState(false);
            }
          } else {
            // Другие ошибки (например, 401, 403) - бэкенд доступен, но запрос не прошел
            // В этом случае считаем бэкенд доступным, так как он ответил
            if (globalBackendAvailable !== true) {
              updateState(true);
            }
          }
        }
      };

      // Выполняем первую проверку сразу
      checkBackendHealth();

      // Настраиваем периодическую проверку
      healthCheckInterval = setInterval(checkBackendHealth, HEALTH_CHECK_INTERVAL);
    }

    return () => {
      // Удаляем слушатель при размонтировании компонента
      healthListeners.delete(setBackendAvailable);
    };
  }, []);

  return {
    backendAvailable,
  };
};

