/**
 * Утилиты для работы с бэкендом
 */

import { AxiosError } from 'axios';

/**
 * Проверяет, является ли ошибка следствием недоступности бэкенда
 * @param error - Ошибка для проверки
 * @returns true, если бэкенд недоступен
 */
export const isBackendUnavailable = (error: unknown): boolean => {
  // Проверка для AxiosError
  if (error instanceof AxiosError) {
    // Network errors (нет ответа от сервера)
    if (!error.response) {
      return true;
    }
    
    // Проверка кода ошибки
    const code = error.code;
    if (
      code === 'ECONNREFUSED' ||
      code === 'ETIMEDOUT' ||
      code === 'ENOTFOUND' ||
      code === 'ERR_NETWORK' ||
      code === 'ERR_CONNECTION_REFUSED' ||
      code === 'ERR_CONNECTION_TIMED_OUT'
    ) {
      return true;
    }
    
    // Проверка сообщения об ошибке
    const errorMessage = error.message?.toLowerCase() || '';
    if (
      errorMessage.includes('network error') ||
      errorMessage.includes('failed to fetch') ||
      errorMessage.includes('connection refused') ||
      errorMessage.includes('timeout')
    ) {
      return true;
    }
  }
  
  // Проверка для обычных Error
  if (error instanceof Error) {
    const errorMessage = error.message.toLowerCase();
    return (
      errorMessage.includes('failed to fetch') ||
      errorMessage.includes('network error') ||
      errorMessage.includes('connection refused') ||
      errorMessage.includes('err_connection_refused') ||
      errorMessage.includes('err_network') ||
      errorMessage.includes('timeout')
    );
  }
  
  // Проверка для строк
  if (typeof error === 'string') {
    const errorMessage = error.toLowerCase();
    return (
      errorMessage.includes('failed to fetch') ||
      errorMessage.includes('network error') ||
      errorMessage.includes('connection refused')
    );
  }
  
  return false;
};

/**
 * Получает нейтральное сообщение для недоступности бэкенда
 * @returns Нейтральное сообщение
 * 
 * Примечание: Эта функция используется в местах, где i18n может быть недоступен.
 * Для компонентов React используйте useTranslation() напрямую.
 */
export const getBackendUnavailableMessage = (): string => {
  // Пытаемся получить перевод из i18n, если он доступен
  try {
    // Динамический импорт для избежания циклических зависимостей
    const i18n = (window as any).__i18n__;
    if (i18n) {
      return i18n.t('public.backendUnavailable.message');
    }
  } catch (error) {
    // Если i18n недоступен, возвращаем английский текст по умолчанию
  }
  return 'Service temporarily unavailable. Please try again later.';
};

