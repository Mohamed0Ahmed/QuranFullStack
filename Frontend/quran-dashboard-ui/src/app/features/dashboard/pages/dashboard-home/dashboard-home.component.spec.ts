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

const REM_FALLBACK_PX = 16;

function rootLength(property: string, fallback: string): string {
  const declared = getComputedStyle(document.documentElement).getPropertyValue(property).trim();
  return declared === '' ? fallback : declared;
}

function factorToPx(factor: string): number {
  const token = factor.trim().replace('var(--qd-space-1)', rootLength('--qd-space-1', '0.25rem'));
  if (token.endsWith('rem')) {
    const remPx = parseFloat(getComputedStyle(document.documentElement).fontSize) || REM_FALLBACK_PX;
    return parseFloat(token) * remPx;
  }
  return parseFloat(token);
}

function reservedBoxHeightPx(expression: string): number {
  return expression
    .replace(/^\s*calc\(/, '')
    .replace(/\)\s*$/, '')
    .split('+')
    .reduce(
      (sum, term) => sum + term.split('*').reduce((product, factor) => product * factorToPx(factor), 1),
      0,
    );
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

  it('reserves the loaded badge line box while loading, so the cards below do not shift on settle', () => {
    const loadingRoot = renderWith(new Observable<AppInfo>());
    const skeleton = loadingRoot.querySelector<HTMLElement>('[data-testid="dashboard-app-meta-loading"]');
    expect(skeleton).toBeTruthy();

    const reservedHeightPx = reservedBoxHeightPx(
      getComputedStyle(skeleton!).getPropertyValue('--qd-skeleton-h'),
    );
    const badgeLineBoxPx = 2 * factorToPx('var(--qd-space-1)') + factorToPx('0.75rem') * 1.4 + 2 * 1;
    expect(reservedHeightPx).toBeCloseTo(badgeLineBoxPx, 5);
    expect(reservedHeightPx).toBeGreaterThan(factorToPx('0.75rem'));

    TestBed.resetTestingModule();
    const loadedRoot = renderWith(of(APP_INFO));
    const appMeta = loadedRoot.querySelector<HTMLElement>('.app-meta');
    expect(appMeta).toBeTruthy();

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
