import { useEffect, useState, useRef } from 'react';
import { HubConnectionState } from '@microsoft/signalr';
import { createSignalRConnection, SIGNALR_HUB_URL } from '@/config/signalr';
import type { ServiceStatusChangedEvent } from '@/types';
import { isBackendUnavailable } from '@/utils/backend';

// Глобальные переменные для управления одним экземпляром подключения
let globalConnection: ReturnType<typeof createSignalRConnection> | null = null;
let globalConnectionState: HubConnectionState = HubConnectionState.Disconnected;
let stateListeners: Set<(state: HubConnectionState) => void> = new Set();
let isInitialized = false;
let lastError: string | null = null;

export const useSignalR = () => {
  const [connectionState, setConnectionState] = useState<HubConnectionState>(
    globalConnectionState
  );

  useEffect(() => {
    // Определяем isDevelopment на уровне useEffect для доступа во всех функциях
    const isDevelopment = import.meta.env.DEV;
    
    // Инициализируем подключение только один раз
    if (!globalConnection) {
      globalConnection = createSignalRConnection();
    }
    
    const connection = globalConnection;
    
    // Добавляем слушатель изменений состояния
    const updateState = (newState: HubConnectionState) => {
      globalConnectionState = newState;
      stateListeners.forEach((listener) => listener(newState));
    };
    
    stateListeners.add(setConnectionState);
    
    // Инициализируем обработчики только один раз
    if (!isInitialized) {
      isInitialized = true;
      
      const logStateChange = (newState: HubConnectionState, reason?: string, silent = false) => {
        // В production не логируем тихие изменения состояния
        if (silent && !isDevelopment) return;
        
        const stateName = Object.keys(HubConnectionState).find(
          (key) => HubConnectionState[key as keyof typeof HubConnectionState] === newState
        );
        
        // Важные изменения состояния (Connected/Disconnected) логируем всегда
        const isImportantState = 
          newState === HubConnectionState.Connected || 
          newState === HubConnectionState.Disconnected;
        
        if (silent && !isImportantState) {
          // Тихие изменения - только в development
          if (isDevelopment) {
            console.debug(
              `[SignalR] Connection state changed: ${stateName}${reason ? ` (${reason})` : ''}`
            );
          }
        } else {
          // Важные изменения - логируем всегда
          console.info(
            `[SignalR] Connection state changed: ${stateName}${reason ? ` (${reason})` : ''}`
          );
        }
      };

      const startConnection = async () => {
        // Детальная информация только в development
        if (isDevelopment) {
          console.info('[SignalR] Attempting to start connection...', {
            url: SIGNALR_HUB_URL,
            negotiationUrl: `${SIGNALR_HUB_URL}/negotiate`,
            connectionState: connection.state,
            hasToken: !!localStorage.getItem('accessToken'),
          });
        }
        
        try {
          const startTime = performance.now();
          await connection.start();
          const duration = Math.round(performance.now() - startTime);

          updateState(connection.state);
          logStateChange(connection.state);
          // Успешное подключение - логируем всегда
          console.info(`[SignalR] ✅ Connection started successfully in ${duration}ms`);

          // Подписываемся на все сервисы
          if (isDevelopment) {
            console.debug('[SignalR] Subscribing to all services...');
          }
          try {
            await connection.invoke('SubscribeToAllServices');
            // Успешная подписка - только в development
            if (isDevelopment) {
              console.info('[SignalR] ✅ Successfully subscribed to all services');
            }
          } catch (subscribeError) {
            // Ошибка подписки - всегда логируем
            console.error('[SignalR] ❌ Failed to subscribe to all services:', subscribeError);
          }
        } catch (error) {
          updateState(connection.state);
          
          const errorMessage = error instanceof Error ? error.message : String(error);
          const isUnavailable = isBackendUnavailable(error);
          
          // Детальное логирование ошибки только в development
          if (isDevelopment) {
            console.group('[SignalR] Connection Error Details');
            console.error('Error:', error);
            console.error('Error message:', errorMessage);
            console.error('Connection state:', connection.state);
            console.error('Connection ID:', connection.connectionId || 'none');
            console.error('Negotiation URL:', `${SIGNALR_HUB_URL}/negotiate`);
            console.error('Backend URL:', SIGNALR_HUB_URL.replace('/hubs/status', ''));
            if (error instanceof Error) {
              console.error('Error name:', error.name);
              console.error('Error stack:', error.stack);
              if ('cause' in error) {
                console.error('Error cause:', (error as any).cause);
              }
            }
            console.groupEnd();
            
            // Полезные подсказки для диагностики
            console.group('[SignalR] Troubleshooting Tips');
            console.info('1. Проверьте, что backend запущен на:', SIGNALR_HUB_URL.replace('/hubs/status', ''));
            console.info('2. Проверьте доступность negotiation endpoint:', `${SIGNALR_HUB_URL}/negotiate`);
            console.info('3. Проверьте CORS настройки на backend');
            console.info('4. Проверьте, что JWT Bearer настроен для SignalR в backend');
            console.info('5. Откройте Network tab в DevTools и проверьте запрос к /negotiate');
            console.groupEnd();
          }
          
          // Предотвращаем дублирование одинаковых ошибок
          if (lastError === errorMessage) {
            // Повторяющиеся ошибки - только в development
            if (isDevelopment) {
              console.debug('[SignalR] Connection failed (same error, retrying silently...)');
            }
          } else {
            lastError = errorMessage;
            
            if (isUnavailable) {
              // Проверяем, не подключилось ли оно уже (может быть race condition)
              // Даем небольшую задержку для проверки реального состояния
              setTimeout(() => {
                if (connection.state === HubConnectionState.Connected) {
                  if (isDevelopment) {
                    console.info('[SignalR] Connection succeeded after initial error (race condition resolved)');
                  }
                  return;
                }
                
                // Предупреждение о недоступности backend - логируем всегда, но кратко
                if (isDevelopment) {
                  console.warn(
                    '[SignalR] ⚠️ Backend appears to be unavailable. Connection will retry automatically.',
                    {
                      url: SIGNALR_HUB_URL,
                      connectionId: connection.connectionId || 'negotiating...',
                      currentState: connection.state,
                      hint: 'Make sure the backend API is running and accessible',
                    }
                  );
                } else {
                  // В production - краткое предупреждение
                  console.warn('[SignalR] ⚠️ Backend unavailable, retrying...');
                }
                
                if (isDevelopment && error instanceof Error) {
                  console.debug('[SignalR] Error details:', {
                    name: error.name,
                    message: error.message,
                    cause: error.cause,
                  });
                }
              }, 100);
            } else {
              // Другие ошибки - всегда логируем
              logStateChange(connection.state, 'connection failed');
              console.error('[SignalR] ❌ Failed to start connection:', {
                error: errorMessage,
                state: connection.state,
                connectionId: connection.connectionId || 'none',
              });

              if (error instanceof Error) {
                console.error('[SignalR] Error details:', {
                  name: error.name,
                  message: error.message,
                  stack: isDevelopment ? error.stack : undefined,
                });
              }
            }
          }
        }
      };

      // Обработчики событий состояния подключения
      connection.onclose((error) => {
        updateState(HubConnectionState.Disconnected);
        
        if (error) {
          const isUnavailable = isBackendUnavailable(error);
          if (isUnavailable) {
            // Тихая обработка для недоступности backend
            logStateChange(HubConnectionState.Disconnected, 'backend unavailable', true);
          } else {
            logStateChange(HubConnectionState.Disconnected, 'connection closed');
            console.warn('[SignalR] ⚠️ Connection closed with error:', {
              error: error.message || String(error),
              allowReconnect: error?.allowReconnect ?? false,
            });
          }
        } else {
          logStateChange(HubConnectionState.Disconnected, 'closed normally');
          console.info('[SignalR] Connection closed normally');
        }
      });

      connection.onreconnecting((error) => {
        updateState(HubConnectionState.Reconnecting);
        
        const isUnavailable = error ? isBackendUnavailable(error) : false;
        if (isUnavailable) {
          // Тихая обработка для недоступности backend
          logStateChange(HubConnectionState.Reconnecting, 'backend unavailable', true);
        } else {
          logStateChange(HubConnectionState.Reconnecting, 'reconnecting');
          if (error) {
            console.warn('[SignalR] ⚠️ Reconnecting due to error:', error.message || String(error));
          } else {
            console.info('[SignalR] 🔄 Reconnecting...');
          }
        }
      });

      connection.onreconnected((connectionId) => {
        updateState(HubConnectionState.Connected);
        lastError = null; // Сбрасываем ошибку при успешном переподключении
        logStateChange(HubConnectionState.Connected, 'reconnected');
        // Переподключение - логируем всегда
        console.info(`[SignalR] ✅ Reconnected successfully. Connection ID: ${connectionId}`);
        
        // Переподписываемся после переподключения
        if (isDevelopment) {
          console.debug('[SignalR] Resubscribing to all services after reconnect...');
        }
        connection.invoke('SubscribeToAllServices')
          .then(() => {
            // Успешная переподписка - только в development
            if (isDevelopment) {
              console.info('[SignalR] ✅ Successfully resubscribed to all services');
            }
          })
          .catch((error) => {
            // Ошибка переподписки - всегда логируем
            console.error('[SignalR] ❌ Failed to resubscribe to all services:', error);
          });
      });

      // Запускаем подключение
      if (connection.state === HubConnectionState.Disconnected) {
        if (isDevelopment) {
          console.info('[SignalR] Initializing connection...');
        }
        startConnection();
      } else {
        updateState(connection.state);
        if (isDevelopment) {
          console.info(`[SignalR] Connection already in state: ${connection.state}`);
        }
      }
    }

    return () => {
      // Удаляем слушатель при размонтировании компонента
      stateListeners.delete(setConnectionState);
      // Логируем только в development
      if (isDevelopment) {
        console.debug('[SignalR] Component unmounted, listener removed');
      }
    };
  }, []);

  return {
    connection: globalConnection,
    connectionState,
  };
};

export const useServiceStatusUpdates = (
  onStatusChanged?: (event: ServiceStatusChangedEvent) => void
) => {
  const { connection, connectionState } = useSignalR();

  useEffect(() => {
    const isDevelopment = import.meta.env.DEV;
    
    if (connectionState !== 'Connected') {
      // Логируем только в development
      if (isDevelopment) {
        console.debug(
          `[SignalR] Skipping status updates subscription. Connection state: ${connectionState}`
        );
      }
      return;
    }

    // Подписка на события - только в development
    if (isDevelopment) {
      console.debug('[SignalR] Subscribing to ServiceStatusChanged events');
    }

    const handler = (event: ServiceStatusChangedEvent) => {
      // Логируем получение событий только в development
      if (isDevelopment) {
        console.debug('[SignalR] Received ServiceStatusChanged event:', {
          serviceId: event.serviceId,
          status: event.status,
          timestamp: event.timestamp,
        });
      }
      onStatusChanged?.(event);
    };

    connection.on('ServiceStatusChanged', handler);

    return () => {
      // Отписка - только в development
      if (isDevelopment) {
        console.debug('[SignalR] Unsubscribing from ServiceStatusChanged events');
      }
      connection.off('ServiceStatusChanged', handler);
    };
  }, [connection, connectionState, onStatusChanged]);
};

