import apiClient from '@/config/api';
import type {
  PublicServiceDto,
  PublicStatusDto,
  ServiceDto,
  HealthCheckResultDto,
  LoginDto,
  AuthResponseDto,
  ServiceCreateDto,
  ServiceUpdateDto,
  UserDto,
  UserCreateDto,
  UserUpdateDto,
  MaintenanceModeDto,
  MaintenanceModeEnableDto,
  IpStatusDto,
  AnalyticsDto,
} from '@/types';

// Public API
export const publicApi = {
  getStatus: async (): Promise<PublicStatusDto> => {
    const response = await apiClient.get<PublicStatusDto>('/api/public/status');
    return response.data;
  },

  getServices: async (): Promise<PublicServiceDto[]> => {
    const response = await apiClient.get<PublicServiceDto[]>('/api/public/services');
    return response.data;
  },

  getService: async (id: string): Promise<PublicServiceDto> => {
    const response = await apiClient.get<PublicServiceDto>(`/api/public/services/${id}`);
    return response.data;
  },

  getSummary: async (): Promise<PublicStatusDto> => {
    const response = await apiClient.get<PublicStatusDto>('/api/public/summary');
    return response.data;
  },

  getIpStatus: async (ipAddress: string): Promise<IpStatusDto> => {
    const response = await apiClient.get<IpStatusDto>(`/api/public/ip-status?ipAddress=${encodeURIComponent(ipAddress)}`);
    return response.data;
  },
};

// Private API
export const servicesApi = {
  getAll: async (): Promise<ServiceDto[]> => {
    const response = await apiClient.get<ServiceDto[]>('/api/services');
    return response.data;
  },

  getById: async (id: string): Promise<ServiceDto> => {
    const response = await apiClient.get<ServiceDto>(`/api/services/${id}`);
    return response.data;
  },

  getStatus: async (id: string): Promise<HealthCheckResultDto> => {
    const response = await apiClient.get<HealthCheckResultDto>(
      `/api/services/${id}/status`
    );
    return response.data;
  },

  getHistory: async (id: string, from?: string, to?: string): Promise<HealthCheckResultDto[]> => {
    const params = new URLSearchParams();
    if (from) params.append('from', from);
    if (to) params.append('to', to);
    const response = await apiClient.get<HealthCheckResultDto[]>(
      `/api/services/${id}/history?${params.toString()}`
    );
    return response.data;
  },

  create: async (data: ServiceCreateDto): Promise<ServiceDto> => {
    const response = await apiClient.post<ServiceDto>('/api/services', data);
    return response.data;
  },

  update: async (id: string, data: ServiceUpdateDto): Promise<ServiceDto> => {
    const response = await apiClient.put<ServiceDto>(`/api/services/${id}`, data);
    return response.data;
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/services/${id}`);
  },

  check: async (id: string): Promise<HealthCheckResultDto> => {
    const response = await apiClient.post<HealthCheckResultDto>(`/api/services/${id}/check`);
    return response.data;
  },

  checkAll: async (): Promise<{ message: string }> => {
    const response = await apiClient.post<{ message: string }>('/api/services/check-all');
    return response.data;
  },
};

// Auth API
export const authApi = {
  login: async (credentials: LoginDto): Promise<AuthResponseDto> => {
    const response = await apiClient.post<AuthResponseDto>('/api/auth/login', credentials);
    return response.data;
  },

  refresh: async (refreshToken: string): Promise<AuthResponseDto> => {
    const response = await apiClient.post<AuthResponseDto>('/api/auth/refresh', {
      refreshToken,
    });
    return response.data;
  },

  logout: async (): Promise<void> => {
    await apiClient.post('/api/auth/logout');
  },
};

// Users API
export const usersApi = {
  getAll: async (): Promise<UserDto[]> => {
    const response = await apiClient.get<UserDto[]>('/api/users');
    return response.data;
  },

  getById: async (id: string): Promise<UserDto> => {
    const response = await apiClient.get<UserDto>(`/api/users/${id}`);
    return response.data;
  },

  create: async (data: UserCreateDto): Promise<UserDto> => {
    const response = await apiClient.post<UserDto>('/api/users', data);
    return response.data;
  },

  update: async (id: string, data: UserUpdateDto): Promise<UserDto> => {
    const response = await apiClient.put<UserDto>(`/api/users/${id}`, data);
    return response.data;
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/users/${id}`);
  },
};

// Maintenance API
export const maintenanceApi = {
  getStatus: async (): Promise<MaintenanceModeDto> => {
    const response = await apiClient.get<MaintenanceModeDto>('/api/maintenance/status');
    return response.data;
  },

  enable: async (data: MaintenanceModeEnableDto): Promise<MaintenanceModeDto> => {
    const response = await apiClient.post<MaintenanceModeDto>('/api/maintenance/enable', data);
    return response.data;
  },

  disable: async (): Promise<MaintenanceModeDto> => {
    const response = await apiClient.post<MaintenanceModeDto>('/api/maintenance/disable');
    return response.data;
  },
};

// Analytics API
export const analyticsApi = {
  getAnalytics: async (period: string = '7d'): Promise<AnalyticsDto> => {
    const response = await apiClient.get<AnalyticsDto>(`/api/analytics?period=${period}`);
    return response.data;
  },
};

