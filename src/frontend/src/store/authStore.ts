import { create } from 'zustand';
import type { UserDto } from '@/types';

interface AuthState {
  user: UserDto | null;
  isAuthenticated: boolean;
  setUser: (user: UserDto | null) => void;
  logout: () => void;
  isAdmin: () => boolean;
  initialize: () => void;
  checkAuth: () => void;
}

const USER_STORAGE_KEY = 'user';

// Функция для восстановления состояния из localStorage
const restoreAuthState = (): { user: UserDto | null; isAuthenticated: boolean } => {
  if (typeof window === 'undefined') {
    return { user: null, isAuthenticated: false };
  }

  const token = localStorage.getItem('accessToken');
  const userStr = localStorage.getItem(USER_STORAGE_KEY);
  
  if (token && userStr) {
    try {
      const user = JSON.parse(userStr) as UserDto;
      return { user, isAuthenticated: true };
    } catch (error) {
      // Если не удалось распарсить, очищаем хранилище
      localStorage.removeItem(USER_STORAGE_KEY);
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      return { user: null, isAuthenticated: false };
    }
  }
  
  return { user: null, isAuthenticated: false };
};

export const useAuthStore = create<AuthState>((set, get) => {
  // Восстанавливаем состояние при создании store
  const initialState = restoreAuthState();

  // Подписываемся на события обновления авторизации
  if (typeof window !== 'undefined') {
    // Обработчик события обновления токена
    const handleAuthRefresh = ((e: CustomEvent<UserDto>) => {
      const store = useAuthStore.getState();
      store.setUser(e.detail);
    }) as EventListener;

    // Обработчик события выхода из системы
    const handleAuthLogout = () => {
      const store = useAuthStore.getState();
      store.logout();
    };

    window.addEventListener('auth:refresh', handleAuthRefresh);
    window.addEventListener('auth:logout', handleAuthLogout);

    // Очистка слушателей при размонтировании (опционально, для SSR)
    if (typeof window !== 'undefined' && 'removeEventListener' in window) {
      // Сохраняем ссылки для возможной очистки
      (window as any).__authEventListeners = {
        refresh: handleAuthRefresh,
        logout: handleAuthLogout,
      };
    }
  }

  return {
    user: initialState.user,
    isAuthenticated: initialState.isAuthenticated,

    setUser: (user) => {
      if (user) {
        localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(user));
      } else {
        localStorage.removeItem(USER_STORAGE_KEY);
      }
      set({ user, isAuthenticated: !!user });
    },

    logout: () => {
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem(USER_STORAGE_KEY);
      set({ user: null, isAuthenticated: false });
    },

    isAdmin: () => {
      const { user } = get();
      return user?.role === 'Admin';
    },

    initialize: () => {
      const state = restoreAuthState();
      set(state);
    },

    checkAuth: () => {
      // Проверяем наличие токена и его валидность
      const token = localStorage.getItem('accessToken');
      const userStr = localStorage.getItem(USER_STORAGE_KEY);
      
      if (!token || !userStr) {
        // Если нет токена или пользователя, очищаем состояние
        get().logout();
        return;
      }

      // Если есть токен, восстанавливаем состояние
      try {
        const user = JSON.parse(userStr) as UserDto;
        get().setUser(user);
      } catch (error) {
        // Если не удалось распарсить, очищаем состояние
        get().logout();
      }
    },
  };
});

