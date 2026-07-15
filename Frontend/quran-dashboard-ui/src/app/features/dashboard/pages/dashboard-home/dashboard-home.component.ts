import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { SystemApi } from '../../../../core/data-access/system.api';
import { AppInfo } from '../../../../core/data-access/system.models';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';

type ViewState = 'loading' | 'success' | 'error';

@Component({
  selector: 'qd-dashboard-home',
  standalone: true,
  imports: [CommonModule, RouterLink, QdSkeletonRowsComponent],
  templateUrl: './dashboard-home.component.html',
  styleUrls: ['./dashboard-home.component.scss'],
})
export class DashboardHomeComponent implements OnInit {
  private readonly systemApi = inject(SystemApi);
  private readonly destroyRef = inject(DestroyRef);

  viewState: ViewState = 'loading';
  appInfo: AppInfo | null = null;
  errorMessage = '';

  ngOnInit(): void {
    this.fetchAppInfo();
  }

  retry(): void {
    this.viewState = 'loading';
    this.errorMessage = '';
    this.appInfo = null;
    this.fetchAppInfo();
  }

  private fetchAppInfo(): void {
    this.systemApi
      .getDashboardInfo()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.appInfo = data;
          this.viewState = 'success';
        },
        error: (err: Error) => {
          this.errorMessage = err.message;
          this.viewState = 'error';
        },
      });
  }
}
