import { formatDistanceToNow, format, type Locale } from 'date-fns';
import ru from 'date-fns/locale/ru';
import uk from 'date-fns/locale/uk';
import pl from 'date-fns/locale/pl';
import enUS from 'date-fns/locale/en-US';

// Маппинг языков i18n на локали date-fns
// Для белорусского используем русскую локаль, так как в date-fns нет белорусской локали
const localeMap: Record<string, Locale> = {
  en: enUS,
  ru: ru,
  uk: uk,
  pl: pl,
  be: ru, // Используем русскую локаль для белорусского языка
};

export const formatRelativeTime = (date: string | Date): string => {
  try {
    // Пытаемся получить текущий язык из i18n
    let locale = ru; // По умолчанию русский
    try {
      const i18n = (window as any).__i18n__;
      if (i18n) {
        const currentLang = i18n.language || 'ru';
        locale = localeMap[currentLang] || localeMap[currentLang.split('-')[0]] || ru;
      }
    } catch {
      // Если i18n недоступен, используем русский по умолчанию
    }

    return formatDistanceToNow(new Date(date), {
      addSuffix: true,
      locale: locale,
    });
  } catch {
    // Если i18n недоступен, возвращаем перевод из i18n или fallback
    try {
      const i18n = (window as any).__i18n__;
      if (i18n) {
        return i18n.t('public.status.unknown', { defaultValue: 'Unknown' });
      }
    } catch {
      // Fallback
    }
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
