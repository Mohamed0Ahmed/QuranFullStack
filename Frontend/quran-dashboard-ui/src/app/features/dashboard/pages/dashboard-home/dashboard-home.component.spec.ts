import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { DashboardHomeComponent } from './dashboard-home.component';
import { SystemApi } from '../../../../core/data-access/system.api';
import { AppInfo } from '../../../../core/data-access/system.models';

const APP_INFO: AppInfo = {
  appName: 'المنهج القرآني',
  version: '1.0.0',
  environment: 'Development',
};

function renderWith(dashboardInfo$: Observable<AppInfo>): HTMLElement {
  TestBed.configureTestingModule({
    providers: [
      provideRouter([]),
      { provide: SystemApi, useValue: { getDashboardInfo: () => dashboardInfo$ } },
    ],
  });

  const fixture = TestBed.createComponent(DashboardHomeComponent);
  fixture.detectChanges();
  return fixture.nativeElement as HTMLElement;
}

describe('DashboardHomeComponent — stable app-meta loading (N3 row 14)', () => {
  it('renders the badge strip once the app info arrives', () => {
    const root = renderWith(of(APP_INFO));

    expect(root.querySelector('[data-testid="dashboard-app-meta-loading"]')).toBeNull();
    const badges = Array.from(root.querySelectorAll('.app-meta .qd-badge')).map((node) =>
      node.textContent?.trim(),
    );
    expect(badges).toEqual(['المنهج القرآني', 'الإصدار 1.0.0', 'Development']);
  });

  it('reserves the badge strip box while loading, so the cards below do not shift on settle', () => {
    // A never-emitting source holds the component in its loading state.
    const loadingRoot = renderWith(new Observable<AppInfo>());
    const skeleton = loadingRoot.querySelector<HTMLElement>('[data-testid="dashboard-app-meta-loading"]');
    expect(skeleton).toBeTruthy();

    // The badge line box must be reserved: without a --qd-skeleton-h the row would
    // fall back to the 0.75rem text-skeleton default and come up ~1.3rem short.
    const reservedHeight = getComputedStyle(skeleton!).getPropertyValue('--qd-skeleton-h').trim();
    expect(reservedHeight).not.toBe('');
    expect(reservedHeight).not.toBe('0.75rem');

    TestBed.resetTestingModule();
    const loadedRoot = renderWith(of(APP_INFO));
    const appMeta = loadedRoot.querySelector<HTMLElement>('.app-meta');
    expect(appMeta).toBeTruthy();

    // The two states sit in the same slot, so they must carry the same leading
    // margin — the original defect was the skeleton having none at all.
    const skeletonMargin = getComputedStyle(skeleton!).marginBlockStart;
    const loadedMargin = getComputedStyle(appMeta!).marginBlockStart;
    expect(skeletonMargin).not.toBe('0px');
    expect(skeletonMargin).toBe(loadedMargin);
  });

  it('keeps the sr-only status announcement while loading', () => {
    const root = renderWith(new Observable<AppInfo>());

    const status = root.querySelector('[data-testid="dashboard-app-meta-loading"] [role="status"]');
    expect(status?.classList.contains('qd-sr-only')).toBe(true);
    expect(status?.textContent?.trim()).toBe('جارٍ تحميل بيانات التطبيق...');
  });

  it('renders the calm error state with a retry action when app info fails', () => {
    const root = renderWith(throwError(() => new Error('تعذر تحميل بيانات التطبيق. حاول مرة أخرى.')));

    const error = root.querySelector('.app-meta-error');
    expect(error?.getAttribute('role')).toBe('alert');
    expect(error?.textContent).toContain('تعذر تحميل بيانات التطبيق');
  });
});
