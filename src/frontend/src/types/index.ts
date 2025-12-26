// Enums
export enum HealthStatus {
  Healthy = 0,
  Degraded = 1,
  Unhealthy = 2,
  Unknown = 3,
}

export enum ServiceType {
  Http = 0,
  Tcp = 1,
  Database = 2,
  Redis = 3,
  WindowsService = 4,
  Kafka = 5,
  Custom = 6,
}

// DTOs
export interface PublicServiceDto {
  id: string;
  name: string;
  status: HealthStatus;
  lastCheckedAt?: string;
}

export interface PublicStatusDto {
  status: HealthStatus;
  totalServices: number;
  healthyServices: number;
  degradedServices: number;
  unhealthyServices: number;
  lastUpdated?: string;
  availabilityPercentage: number;
}

export interface ServiceDto {
  id: string;
  name: string;
  description: string;
  url: string;
  type: ServiceType;
  checkInterval: number;
  timeout: number;
  retryCount: number;
  isEnabled: boolean;
  isPublic: boolean;
  lastStatus?: HealthStatus;
  lastCheckedAt?: string;
  createdAt: string;
  updatedAt: string;
  configuration?: ServiceConfigurationDto;
}

export interface HealthCheckResultDto {
  id: string;
  serviceId: string;
  status: HealthStatus;
  responseTime: number;
  message?: string;
  exception?: string;
  checkedAt: string;
  metadata?: Record<string, unknown>;
}

export interface LoginDto {
  username: string;
  password: string;
}

export interface AuthResponseDto {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: UserDto;
}

export interface UserDto {
  id: string;
  username: string;
  email?: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
}

export interface ServiceConfigurationDto {
  checkType?: string;
  parameters?: string;
  headers?: string;
  expectedStatusCode?: number;
  expectedResponse?: string;
}

export interface ServiceCreateDto {
  name: string;
  description: string;
  url: string;
  type: ServiceType;
  checkInterval: number;
  timeout: number;
  retryCount: number;
  isEnabled: boolean;
  isPublic: boolean;
  configuration?: ServiceConfigurationDto;
}

export interface ServiceUpdateDto {
  name?: string;
  description?: string;
  url?: string;
  type?: ServiceType;
  checkInterval?: number;
  timeout?: number;
  retryCount?: number;
  isEnabled?: boolean;
  isPublic?: boolean;
  configuration?: ServiceConfigurationDto;
}

export interface UserCreateDto {
  username: string;
  email: string;
  password: string;
  role: string;
  isActive: boolean;
}

export interface UserUpdateDto {
  username?: string;
  email?: string;
  password?: string;
  role?: string;
  isActive?: boolean;
}

export interface MaintenanceModeDto {
  id: string;
  isEnabled: boolean;
  message?: string;
  scheduledStartTime?: string;
  scheduledEndTime?: string;
  startedAt?: string;
  endedAt?: string;
  startedByUserId?: string;
  endedByUserId?: string;
  createdAt: string;
  updatedAt: string;
}

export interface MaintenanceModeEnableDto {
  message?: string;
  scheduledStartTime?: string;
  scheduledEndTime?: string;
}

// IP Status
export interface IpStatusDto {
  ipAddress?: string;
  isBlocked: boolean;
  blockedDate?: string;
}

// SignalR
export interface ServiceStatusChangedEvent {
  serviceId: string;
  status: HealthStatus;
  checkedAt: string;
  responseTime?: number;
  message?: string;
}

// Analytics
export interface AnalyticsDto {
  period: string;
  fromDate: string;
  toDate: string;
  systemStatistics: SystemStatisticsDto;
  services: ServiceAnalyticsDto[];
  timeSeries: TimeSeriesDataDto;
  incidents: IncidentDto[];
  serviceTypeStatistics: ServiceTypeStatisticsDto[];
  topServices: TopServicesDto;
}

export interface SystemStatisticsDto {
  uptimePercentage: number;
  totalDowntimeMinutes: number;
  statusStatistics: StatusStatisticsDto;
  responseTimeStatistics: ResponseTimeStatisticsDto;
  checkStatistics: CheckStatisticsDto;
  incidentStatistics: IncidentStatisticsDto;
}

export interface StatusStatisticsDto {
  totalChecks: number;
  healthyCount: number;
  degradedCount: number;
  unhealthyCount: number;
  unknownCount: number;
  healthyPercentage: number;
  degradedPercentage: number;
  unhealthyPercentage: number;
  unknownPercentage: number;
}

export interface ResponseTimeStatisticsDto {
  average: number;
  median: number;
  min: number;
  max: number;
  p95: number;
  p99: number;
}

export interface CheckStatisticsDto {
  totalChecks: number;
  successfulChecks: number;
  failedChecks: number;
  successPercentage: number;
}

export interface IncidentStatisticsDto {
  totalIncidents: number;
  totalDowntimeMinutes: number;
  averageIncidentDurationMinutes: number;
  maxIncidentDurationMinutes: number;
  criticalIncidents: number;
}

export interface ServiceAnalyticsDto {
  serviceId: string;
  serviceName: string;
  serviceType: ServiceType;
  currentStatus?: HealthStatus;
  lastCheckedAt?: string;
  uptimePercentage: number;
  responseTimeStatistics: ResponseTimeStatisticsDto;
  totalChecks: number;
  incidentCount: number;
  totalDowntimeMinutes: number;
  databaseSizeMetrics?: DatabaseSizeMetricsDto;
}

export interface DatabaseSizeMetricsDto {
  totalSizeMB: number;
  dataSizeMB: number;
  logSizeMB: number;
  usedSpaceMB: number;
  freeSpaceMB: number;
  usagePercentage: number;
  freeSpacePercentage: number;
  sizeChangeMB?: number;
  sizeChangePercentage?: number;
  lastUpdated?: string;
}

export interface TimeSeriesDataDto {
  uptimeSeries: TimeSeriesPointDto[];
  responseTimeSeries: TimeSeriesPointDto[];
  statusDistributionSeries: StatusDistributionPointDto[];
  checkCountSeries: TimeSeriesPointDto[];
  databaseSizeSeries: DatabaseSizeTimeSeriesPointDto[];
  databaseSizeForecast: DatabaseSizeForecastPointDto[];
}

export interface TimeSeriesPointDto {
  timestamp: string;
  value: number;
}

export interface StatusDistributionPointDto {
  timestamp: string;
  healthy: number;
  degraded: number;
  unhealthy: number;
  unknown: number;
}

export interface DatabaseSizeTimeSeriesPointDto {
  timestamp: string;
  serviceId: string;
  serviceName: string;
  totalSizeMB: number;
  dataSizeMB: number;
  logSizeMB: number;
  usedSpaceMB: number;
  freeSpaceMB: number;
  usagePercentage: number;
}

export interface DatabaseSizeForecastPointDto {
  timestamp: string;
  serviceId: string;
  serviceName: string;
  forecastedTotalSizeMB: number;
  forecastedUsedSpaceMB: number;
  forecastedUsagePercentage: number;
  growthRateMBPerDay?: number;
  estimatedFullDate?: string;
}

export interface IncidentDto {
  id: string;
  serviceId: string;
  serviceName: string;
  startTime: string;
  endTime?: string;
  durationMinutes: number;
  statusBefore: HealthStatus;
  statusAfter: HealthStatus;
  reason?: string;
  isCritical: boolean;
}

export interface ServiceTypeStatisticsDto {
  serviceType: ServiceType;
  serviceTypeName: string;
  serviceCount: number;
  averageUptimePercentage: number;
  averageResponseTime: number;
  totalIncidents: number;
}

export interface TopServicesDto {
  topByUptime: ServiceAnalyticsDto[];
  bottomByUptime: ServiceAnalyticsDto[];
  topByResponseTime: ServiceAnalyticsDto[];
  bottomByResponseTime: ServiceAnalyticsDto[];
  topByIncidents: ServiceAnalyticsDto[];
  topDatabaseBySize: ServiceAnalyticsDto[];
}

