export interface AppInfo {
  appName: string;
  version: string;
  environment: string;
}

export interface HealthStatus {
  status: 'healthy' | 'unhealthy' | 'degraded';
  checks: HealthCheckItem[];
}

export interface HealthCheckItem {
  name: string;
  status: 'healthy' | 'unhealthy' | 'degraded';
}
