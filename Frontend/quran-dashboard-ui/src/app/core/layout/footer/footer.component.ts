import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { SystemApi } from '../../data-access/system.api';
import { HealthCheckItem, HealthStatus } from '../../data-access/system.models';
import { QdActionDirective } from '../../../shared/ui/action/action.directive';

type ViewState = 'loading' | 'success' | 'error';

@Component({
  selector: 'qd-footer',
  standalone: true,
  imports: [QdActionDirective],
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.scss',
})
export class FooterComponent implements OnInit {
  private readonly systemApi = inject(SystemApi);
  private readonly destroyRef = inject(DestroyRef);

  viewState: ViewState = 'loading';
  healthStatus: HealthStatus | null = null;
  errorMessage = '';

  get healthClass(): string {
    switch (this.healthStatus?.status) {
      case 'healthy':
        return 'health-healthy';
      case 'unhealthy':
        return 'health-unhealthy';
      case 'degraded':
        return 'health-degraded';
      default:
        return '';
    }
  }

  get healthDotClass(): string {
    switch (this.healthStatus?.status) {
      case 'healthy':
        return 'dot-healthy';
      case 'unhealthy':
        return 'dot-unhealthy';
      case 'degraded':
        return 'dot-degraded';
      default:
        return '';
    }
  }

  get healthLabel(): string {
    return this.healthStatus ? `الحالة: ${this.statusLabel(this.healthStatus.status)}` : '';
  }

  get databaseCheck(): HealthCheckItem | null {
    return this.healthStatus?.checks.find((check) => check.name === 'database') ?? null;
  }

  get databaseDotClass(): string {
    return this.statusDotClass(this.databaseCheck?.status);
  }

  get databaseLabel(): string {
    return this.databaseCheck ? `قاعدة البيانات: ${this.statusLabel(this.databaseCheck.status)}` : '';
  }

  ngOnInit(): void {
    this.fetchHealth();
  }

  retry(): void {
    this.viewState = 'loading';
    this.errorMessage = '';
    this.healthStatus = null;
    this.fetchHealth();
  }

  private fetchHealth(): void {
    this.systemApi
      .getHealth()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.healthStatus = data;
          this.viewState = 'success';
        },
        error: (err: Error) => {
          this.errorMessage = err.message;
          this.viewState = 'error';
        },
      });
  }

  private statusDotClass(status: HealthStatus['status'] | undefined): string {
    switch (status) {
      case 'healthy':
        return 'dot-healthy';
      case 'unhealthy':
        return 'dot-unhealthy';
      case 'degraded':
        return 'dot-degraded';
      default:
        return '';
    }
  }

  private statusLabel(status: HealthStatus['status']): string {
    switch (status) {
      case 'healthy':
        return 'سليم';
      case 'unhealthy':
        return 'غير متاح';
      case 'degraded':
        return 'تنبيهات';
    }
  }
}
