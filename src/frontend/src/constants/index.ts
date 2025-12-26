// API endpoints
export const API_ENDPOINTS = {
  PUBLIC: {
    STATUS: '/api/public/status',
    SERVICES: '/api/public/services',
    SERVICE: (id: string) => `/api/public/services/${id}`,
    SUMMARY: '/api/public/summary',
  },
  PRIVATE: {
    SERVICES: '/api/services',
    SERVICE: (id: string) => `/api/services/${id}`,
    SERVICE_STATUS: (id: string) => `/api/services/${id}/status`,
    SERVICE_HISTORY: (id: string) => `/api/services/${id}/history`,
    SERVICE_METRICS: (id: string) => `/api/services/${id}/metrics`,
  },
  AUTH: {
    LOGIN: '/api/auth/login',
    REFRESH: '/api/auth/refresh',
    LOGOUT: '/api/auth/logout',
  },
} as const;

// Local storage keys
export const STORAGE_KEYS = {
  ACCESS_TOKEN: 'accessToken',
  REFRESH_TOKEN: 'refreshToken',
} as const;

// Query keys for React Query
export const QUERY_KEYS = {
  PUBLIC_STATUS: ['public-status'],
  PUBLIC_SERVICES: ['public-services'],
  SERVICES: ['services'],
  SERVICE: (id: string) => ['service', id],
  SERVICE_HISTORY: (id: string) => ['service-history', id],
} as const;

// Refresh intervals (in milliseconds)
export const REFRESH_INTERVALS = {
  PUBLIC_DASHBOARD: 30000, // 30 seconds
  PRIVATE_DASHBOARD: 30000, // 30 seconds
} as const;

