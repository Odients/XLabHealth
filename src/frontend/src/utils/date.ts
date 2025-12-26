import { formatDistanceToNow, format } from 'date-fns';
import ru from 'date-fns/locale/ru';

export const formatRelativeTime = (date: string | Date): string => {
  try {
    return formatDistanceToNow(new Date(date), {
      addSuffix: true,
      locale: ru,
    });
  } catch {
    return 'Неизвестно';
  }
};

export const formatDateTime = (date: string | Date): string => {
  try {
    return format(new Date(date), 'dd.MM.yyyy HH:mm:ss', { locale: ru });
  } catch {
    return 'Неизвестно';
  }
};

export const formatDate = (date: string | Date): string => {
  try {
    return format(new Date(date), 'dd.MM.yyyy', { locale: ru });
  } catch {
    return 'Неизвестно';
  }
};

/**
 * Форматирует дату с учетом часового пояса пользователя
 * @param date - дата в UTC (ISO строка или Date объект)
 * @returns отформатированная строка даты и времени в локальном часовом поясе
 */
export const formatDateTimeWithTimezone = (
  date: string | Date | null | undefined
): string => {
  if (!date) return 'Никогда';

  try {
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    
    // Проверяем, что дата валидна
    if (isNaN(dateObj.getTime())) {
      return 'Неизвестно';
    }
    
    // Используем встроенный toLocaleString для автоматического определения часового пояса пользователя
    // JavaScript автоматически конвертирует UTC в локальный часовой пояс
    return dateObj.toLocaleString('ru-RU', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      timeZoneName: 'short',
    });
  } catch {
    return 'Неизвестно';
  }
};
