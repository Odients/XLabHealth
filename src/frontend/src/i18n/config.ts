import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import enTranslations from './locales/en.json';
import ruTranslations from './locales/ru.json';
import ukTranslations from './locales/uk.json';
import plTranslations from './locales/pl.json';
import beTranslations from './locales/be.json';
import { detectBrowserLanguage } from './utils';

// Определяем язык браузера
const browserLanguage = detectBrowserLanguage();

i18n
  .use(initReactI18next)
  .init({
    resources: {
      en: {
        translation: enTranslations,
      },
      ru: {
        translation: ruTranslations,
      },
      uk: {
        translation: ukTranslations,
      },
      pl: {
        translation: plTranslations,
      },
      be: {
        translation: beTranslations,
      },
    },
    lng: browserLanguage, // Язык по умолчанию определяется автоматически
    fallbackLng: 'en', // Резервный язык - английский
    interpolation: {
      escapeValue: false, // React уже экранирует значения
    },
    // Отключаем возможность смены языка
    // Язык определяется только автоматически при загрузке
  });

// Экспортируем i18n в глобальную область для использования в утилитах
if (typeof window !== 'undefined') {
  (window as any).__i18n__ = i18n;
}

export default i18n;

