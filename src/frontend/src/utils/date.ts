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
 * Получает полную локаль для форматирования даты на основе языка i18n
 */
const getLocaleForDate = (): string => {
  try {
    const i18n = (window as any).__i18n__;
    if (i18n) {
      const currentLang = i18n.language || 'en';
      // Маппинг языков i18n на полные локали для toLocaleString
      const localeMap: Record<string, string> = {
        en: 'en-US',
        ru: 'ru-RU',
        uk: 'uk-UA',
        pl: 'pl-PL',
        be: 'ru-RU', // Используем русскую локаль для белорусского
      };
      return localeMap[currentLang] || localeMap[currentLang.split('-')[0]] || 'en-US';
    }
  } catch {
    // Если i18n недоступен, используем английский по умолчанию
  }
  return 'en-US';
};

/**
 * Форматирует дату и время с учетом часового пояса браузера пользователя и языка i18n
 * @param date - дата в UTC (ISO строка или Date объект)
 * @returns отформатированная строка даты и времени в локальном часовом поясе браузера
 */
export const formatDateTimeLocalized = (
  date: string | Date | null | undefined
): string => {
  if (!date) {
    try {
      const i18n = (window as any).__i18n__;
      if (i18n) {
        return i18n.t('public.serviceCard.neverChecked', { defaultValue: 'Never' });
      }
    } catch {
      // Fallback
    }
    return 'Никогда';
  }

  try {
    let dateObj: Date;
    
    if (typeof date === 'string') {
      // Если дата приходит как строка, убеждаемся, что она правильно парсится как UTC
      // Если строка содержит 'Z', JavaScript автоматически парсит её как UTC
      // Если нет 'Z', но это ISO формат, добавляем 'Z' для явного указания UTC
      let dateString = date.trim();
      
      // Проверяем, есть ли уже указание часового пояса
      if (!dateString.includes('Z') && !dateString.includes('+') && !dateString.includes('-', 10)) {
        // Если нет часового пояса, но это ISO формат (содержит 'T'), предполагаем UTC
        if (dateString.includes('T')) {
          dateString = dateString + 'Z';
        }
      }
      
      dateObj = new Date(dateString);
    } else {
      dateObj = date;
    }
    
    // Проверяем, что дата валидна
    if (isNaN(dateObj.getTime())) {
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
    
    // Получаем полную локаль на основе языка i18n
    const locale = getLocaleForDate();
    
    // Используем toLocaleString с полной локалью
    // JavaScript автоматически использует часовой пояс браузера пользователя
    // Не указываем timeZone явно, чтобы использовался локальный часовой пояс браузера
    return dateObj.toLocaleString(locale, {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      // Не указываем timeZone - будет использован часовой пояс браузера
    });
  } catch {
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
