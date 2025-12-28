import { GOOGLE_TAG_ID } from '@/config/constants';

/**
 * Инициализирует Google Tag Manager
 * Добавляет скрипты GTM в head и noscript в body
 */
export const initGoogleTagManager = (): void => {
  if (!GOOGLE_TAG_ID) {
    console.warn('Google Tag Manager ID is not configured. GTM will not be initialized.');
    return;
  }

  // Проверяем, не инициализирован ли уже GTM
  if (window.dataLayer) {
    console.warn('Google Tag Manager is already initialized.');
    return;
  }

  // Инициализируем dataLayer
  window.dataLayer = window.dataLayer || [];

  // Функция для работы с dataLayer
  window.gtag = function gtag(...args: any[]) {
    window.dataLayer.push(args);
  };

  // Добавляем скрипт GTM в head
  const script = document.createElement('script');
  script.async = true;
  script.src = `https://www.googletagmanager.com/gtm.js?id=${GOOGLE_TAG_ID}`;
  script.onerror = () => {
    console.error('Failed to load Google Tag Manager script');
  };
  document.head.appendChild(script);

  // Добавляем noscript fallback в body
  const noscript = document.createElement('noscript');
  const iframe = document.createElement('iframe');
  iframe.src = `https://www.googletagmanager.com/ns.html?id=${GOOGLE_TAG_ID}`;
  iframe.height = '0';
  iframe.width = '0';
  iframe.style.display = 'none';
  iframe.style.visibility = 'hidden';
  noscript.appendChild(iframe);
  document.body.insertBefore(noscript, document.body.firstChild);

  console.log('Google Tag Manager initialized with ID:', GOOGLE_TAG_ID);
};

/**
 * Отправляет событие в Google Tag Manager
 * @param eventName - Название события
 * @param eventData - Данные события (опционально)
 */
export const sendGTMEvent = (eventName: string, eventData?: Record<string, any>): void => {
  if (!GOOGLE_TAG_ID || !window.dataLayer) {
    console.warn('Google Tag Manager is not initialized. Event will not be sent.');
    return;
  }

  window.dataLayer.push({
    event: eventName,
    ...eventData,
  });
};

/**
 * Отправляет событие page_view в GTM
 * @param pagePath - Путь страницы
 * @param pageTitle - Заголовок страницы (опционально)
 */
export const sendGTMPageView = (pagePath: string, pageTitle?: string): void => {
  sendGTMEvent('page_view', {
    page_path: pagePath,
    page_title: pageTitle || document.title,
  });
};

// Расширяем Window interface для TypeScript
declare global {
  interface Window {
    dataLayer: any[];
    gtag: (...args: any[]) => void;
  }
}

