/**
 * Определяет язык браузера пользователя
 * Поддерживаемые языки: en, ru, uk, pl, be
 * По умолчанию возвращает 'en'
 */
export const detectBrowserLanguage = (): string => {
  // Получаем язык браузера
  const browserLang = navigator.language || (navigator as any).userLanguage || 'en';
  
  // Извлекаем код языка (например, 'ru' из 'ru-RU')
  const langCode = browserLang.split('-')[0].toLowerCase();
  
  // Поддерживаемые языки
  const supportedLanguages = ['en', 'ru', 'uk', 'pl', 'be'];
  
  // Проверяем, поддерживается ли язык
  if (supportedLanguages.includes(langCode)) {
    return langCode;
  }
  
  // Если язык не поддерживается, возвращаем английский по умолчанию
  return 'en';
};

