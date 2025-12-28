import { useCallback } from 'react';
import { getRecaptchaToken, initRecaptcha } from '@/utils/recaptcha';
import { useEffect, useState } from 'react';

/**
 * Хук для работы с Google reCAPTCHA v3
 * Автоматически инициализирует reCAPTCHA при монтировании компонента
 * 
 * @example
 * ```tsx
 * const MyComponent = () => {
 *   const { getToken, isReady } = useRecaptcha();
 *   
 *   const handleSubmit = async () => {
 *     const token = await getToken('login');
 *     // Отправить токен на сервер
 *   };
 *   
 *   return <button onClick={handleSubmit}>Submit</button>;
 * };
 * ```
 */
export const useRecaptcha = () => {
  const [isReady, setIsReady] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    let mounted = true;

    const initialize = async () => {
      try {
        await initRecaptcha();
        if (mounted) {
          setIsReady(true);
        }
      } catch (error) {
        console.error('Failed to initialize reCAPTCHA:', error);
        if (mounted) {
          setIsReady(false);
        }
      }
    };

    initialize();

    return () => {
      mounted = false;
    };
  }, []);

  const getToken = useCallback(
    async (action: string = 'submit'): Promise<string | null> => {
      if (!isReady) {
        console.warn('reCAPTCHA is not ready yet');
        return null;
      }

      setIsLoading(true);
      try {
        const token = await getRecaptchaToken(action);
        return token;
      } catch (error) {
        console.error('Error getting reCAPTCHA token:', error);
        return null;
      } finally {
        setIsLoading(false);
      }
    },
    [isReady]
  );

  return {
    getToken,
    isReady,
    isLoading,
  };
};

