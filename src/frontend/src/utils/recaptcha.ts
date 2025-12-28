import { RECAPTCHA_SITE_KEY } from '@/config/constants';

/**
 * Инициализирует Google reCAPTCHA v3
 * Загружает скрипт reCAPTCHA и инициализирует его с site key
 */
export const initRecaptcha = (): Promise<void> => {
  return new Promise((resolve, reject) => {
    if (!RECAPTCHA_SITE_KEY) {
      console.warn('reCAPTCHA Site Key is not configured. reCAPTCHA will not be initialized.');
      resolve();
      return;
    }

    // Проверяем, не загружен ли уже скрипт
    if (window.grecaptcha && window.grecaptcha.ready) {
      console.log('reCAPTCHA is already initialized');
      resolve();
      return;
    }

    // Проверяем, не загружается ли уже скрипт
    if (document.querySelector(`script[src*="recaptcha"]`)) {
      // Скрипт уже загружается, ждем его готовности
      const checkInterval = setInterval(() => {
        if (window.grecaptcha && window.grecaptcha.ready) {
          clearInterval(checkInterval);
          resolve();
        }
      }, 100);

      // Таймаут на случай, если скрипт не загрузится
      setTimeout(() => {
        clearInterval(checkInterval);
        if (!window.grecaptcha) {
          reject(new Error('reCAPTCHA script failed to load'));
        } else {
          resolve();
        }
      }, 10000);
      return;
    }

    // Загружаем скрипт reCAPTCHA
    const script = document.createElement('script');
    script.src = `https://www.google.com/recaptcha/api.js?render=${RECAPTCHA_SITE_KEY}`;
    script.async = true;
    script.defer = true;
    script.onload = () => {
      if (window.grecaptcha && window.grecaptcha.ready) {
        window.grecaptcha.ready(() => {
          console.log('reCAPTCHA v3 initialized successfully');
          resolve();
        });
      } else {
        reject(new Error('reCAPTCHA script loaded but grecaptcha is not available'));
      }
    };
    script.onerror = () => {
      reject(new Error('Failed to load reCAPTCHA script'));
    };
    document.head.appendChild(script);
  });
};

/**
 * Получает токен reCAPTCHA v3 для указанного действия
 * @param action - Действие, для которого запрашивается токен (например, 'login', 'submit')
 * @returns Promise с токеном reCAPTCHA или null, если reCAPTCHA не настроен
 */
export const getRecaptchaToken = async (action: string = 'submit'): Promise<string | null> => {
  if (!RECAPTCHA_SITE_KEY) {
    return null;
  }

  try {
    // Убеждаемся, что reCAPTCHA инициализирован
    if (!window.grecaptcha || !window.grecaptcha.ready) {
      await initRecaptcha();
    }

    // Ждем готовности reCAPTCHA
    return new Promise((resolve, reject) => {
      if (!window.grecaptcha) {
        resolve(null);
        return;
      }

      window.grecaptcha.ready(async () => {
        try {
          const token = await window.grecaptcha.execute(RECAPTCHA_SITE_KEY, { action });
          resolve(token);
        } catch (error) {
          console.error('Error executing reCAPTCHA:', error);
          reject(error);
        }
      });
    });
  } catch (error) {
    console.error('Error getting reCAPTCHA token:', error);
    return null;
  }
};

// Расширяем Window interface для TypeScript
declare global {
  interface Window {
    grecaptcha: {
      ready: (callback: () => void) => void;
      execute: (siteKey: string, options: { action: string }) => Promise<string>;
      render?: (container: string | HTMLElement, options: any) => number;
    };
  }
}

