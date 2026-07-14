import type { HealthCheckItem as HealthCheckItemDto, HealthReportData } from '../api/generated/models';

export type { AppInfoData as AppInfo } from '../api/generated/models';

export type HealthLevel = 'healthy' | 'unhealthy' | 'degraded';

export interface HealthCheckItem extends Omit<HealthCheckItemDto, 'status'> {
  status: HealthLevel;
}

export interface HealthStatus extends Omit<HealthReportData, 'status' | 'checks'> {
  status: HealthLevel;
  checks: HealthCheckItem[];
}
