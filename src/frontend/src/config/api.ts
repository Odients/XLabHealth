import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { isBackendUnavailable } from '@/utils/backend';
import { getClientIp } from '@/utils/clientIp';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5021';

export const apiClient = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Кэш для IP клиента (получаем один раз при первом запросе)
let cachedClientIp: string | null = null;
let ipFetchPromise: Promise<string | null> | null = null;

/**
 * Получить IP клиента (с кэшированием)
 */
async function getCachedClientIp(): Promise<string | null> {
  // Если IP уже в кэше, возвращаем его
  if (cachedClientIp) {
    return cachedClientIp;
  }

  // Если уже идет запрос IP, ждем его
  if (ipFetchPromise) {
    return ipFetchPromise;
  }

  // Запускаем получение IP
  ipFetchPromise = getClientIp().then(ip => {
    cachedClientIp = ip;
    return ip;
  }).finally(() => {
    ipFetchPromise = null;
  });

  return ipFetchPromise;
}

// Переменная для отслеживания процесса обновления токена и предотвращения множественных одновременных запросов
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value?: any) => void;
  reject: (error?: any) => void;
}> = [];

// Функция для обработки очереди неудачных запросов после обновления токена
const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

// Отдельный axios instance для refresh запроса (без interceptors, чтобы избежать циклических зависимостей)
const refreshAxiosClient = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Interceptor для добавления токена и IP клиента
apiClient.interceptors.request.use(
  async (config: InternalAxiosRequestConfig) => {
    // Добавляем токен авторизации
    const token = localStorage.getItem('accessToken');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    // Добавляем IP клиента в заголовок X-Client-Ip
    // Это позволяет бэкенду получить реальный IP клиента,
    // когда фронтенд находится на серверах провайдера
    if (config.headers) {
      try {
        const clientIp = await getCachedClientIp();
        if (clientIp) {
          config.headers['X-Client-Ip'] = clientIp;
        }
      } catch (error) {
        // Игнорируем ошибки получения IP, чтобы не блокировать запрос
        console.warn('Failed to get client IP for request:', error);
      }
    }

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Interceptor для обработки ошибок
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    // Проверяем, недоступен ли бэкенд
    if (isBackendUnavailable(error)) {
      // Добавляем флаг для компонентов
      (error as any).isBackendUnavailable = true;
      (error as any).neutralMessage = 'Сервис временно недоступен. Пожалуйста, попробуйте позже.';
      return Promise.reject(error);
    }

    // Обработка 401 Unauthorized
    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      // Если уже идет процесс обновления токена, добавляем запрос в очередь
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${token}`;
            }
            return apiClient(originalRequest);
          })
          .catch((err) => {
            return Promise.reject(err);
          });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const refreshToken = localStorage.getItem('refreshToken');
      
      if (refreshToken) {
        try {
          // Используем отдельный axios instance для refresh запроса
          const response = await refreshAxiosClient.post<{
            accessToken: string;
            refreshToken?: string;
            user: any;
          }>(`${API_URL}/api/auth/refresh`, {
            refreshToken,
          });
          
          const { accessToken, refreshToken: newRefreshToken, user } = response.data;
          
          // Сохраняем новые токены
          localStorage.setItem('accessToken', accessToken);
          if (newRefreshToken) {
            localStorage.setItem('refreshToken', newRefreshToken);
          }
          
          // Обновляем информацию о пользователе, если она изменилась
          if (user) {
            localStorage.setItem('user', JSON.stringify(user));
            // Обновляем store через событие, чтобы избежать циклических зависимостей
            window.dispatchEvent(new CustomEvent('auth:refresh', { detail: user }));
          }
          
          // Обновляем заголовок оригинального запроса
          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${accessToken}`;
          }
          
          // Обрабатываем очередь ожидающих запросов
          processQueue(null, accessToken);
          
          // Повторяем оригинальный запрос
          return apiClient(originalRequest);
        } catch (refreshError) {
          // Если обновление токена не удалось, очищаем хранилище и логируем пользователя
          processQueue(refreshError, null);
          
          localStorage.removeItem('accessToken');
          localStorage.removeItem('refreshToken');
          localStorage.removeItem('user');
          
          // Отправляем событие для обновления store
          window.dispatchEvent(new CustomEvent('auth:logout'));
          
          // Перенаправляем на логин только если мы не на странице логина
          if (window.location.pathname !== '/login') {
            window.location.href = '/login';
          }
          
          return Promise.reject(refreshError);
        } finally {
          isRefreshing = false;
        }
      } else {
        // Нет refresh token, очищаем хранилище и логируем пользователя
        isRefreshing = false;
        processQueue(new Error('No refresh token'), null);
        
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');
        
        // Отправляем событие для обновления store
        window.dispatchEvent(new CustomEvent('auth:logout'));
        
        // Перенаправляем на логин только если мы не на странице логина
        if (window.location.pathname !== '/login') {
          window.location.href = '/login';
        }
      }
    }
    
    return Promise.reject(error);
  }
);

export default apiClient;

