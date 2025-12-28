import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import { sendGTMEvent, sendGTMPageView } from '@/utils/gtm';

/**
 * Хук для работы с Google Tag Manager
 * Автоматически отправляет page_view при изменении маршрута
 * 
 * @example
 * ```tsx
 * const MyComponent = () => {
 *   useGTM();
 *   // Компонент автоматически отслеживает переходы по страницам
 *   return <div>Content</div>;
 * };
 * ```
 */
export const useGTM = (): void => {
  const location = useLocation();

  useEffect(() => {
    // Отправляем событие page_view при изменении маршрута
    sendGTMPageView(location.pathname + location.search);
  }, [location]);
};

/**
 * Хук для отправки кастомных событий в GTM
 * 
 * @example
 * ```tsx
 * const MyComponent = () => {
 *   const sendEvent = useGTMEvent();
 *   
 *   const handleClick = () => {
 *     sendEvent('button_click', { button_name: 'submit' });
 *   };
 *   
 *   return <button onClick={handleClick}>Submit</button>;
 * };
 * ```
 */
export const useGTMEvent = () => {
  return (eventName: string, eventData?: Record<string, any>) => {
    sendGTMEvent(eventName, eventData);
  };
};

