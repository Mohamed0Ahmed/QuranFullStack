import { describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, Subject, of, throwError } from 'rxjs';

import { FooterComponent } from './footer.component';
import { SystemApi } from '../../data-access/system.api';
import { HealthStatus } from '../../data-access/system.models';

const HEALTHY: HealthStatus = {
  status: 'healthy',
  checks: [{ name: 'database', status: 'healthy' }],
};

function render(health$: Observable<HealthStatus>): ComponentFixture<FooterComponent> {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    imports: [FooterComponent],
    providers: [{ provide: SystemApi, useValue: { getHealth: () => health$ } }],
  });

  const fixture = TestBed.createComponent(FooterComponent);
  fixture.detectChanges();
  return fixture;
}

function host(fixture: ComponentFixture<FooterComponent>): HTMLElement {
  return fixture.nativeElement as HTMLElement;
}

describe('FooterComponent', () => {
  it('owns its measure through the shell measure class and never opens a second gutter', () => {
    const root = host(render(of(HEALTHY)));

    expect(root.querySelectorAll('.qd-container, .qd-page-frame, .qd-explorer-frame')).toHaveLength(
      0,
    );
    expect(root.querySelector('.qd-footer > .qd-footer__inner')).not.toBeNull();
  });

  it('announces the loading probe politely and marks it busy', () => {
    const root = host(render(new Observable<HealthStatus>()));

    const loading = root.querySelector('[data-testid="footer-health-loading"]');
    expect(loading?.getAttribute('role')).toBe('status');
    expect(loading?.getAttribute('aria-live')).toBe('polite');
    expect(loading?.getAttribute('aria-busy')).toBe('true');
  });

  it('renders the reported status and the database check with a label beside every dot', () => {
    const root = host(render(of(HEALTHY)));

    const health = root.querySelector('[data-testid="footer-health"]');
    expect(health?.textContent).toContain('الحالة: سليم');
    expect(health?.textContent).toContain('قاعدة البيانات: سليم');
    expect(health?.querySelectorAll('.health-dot')).toHaveLength(2);
  });

  it('reports a failed health read politely — never as a write alert — and retries in place', () => {
    const fixture = render(throwError(() => new Error('تعذر الوصول إلى الخادم')));
    const root = host(fixture);

    const error = root.querySelector('[data-testid="footer-health-error"]');
    expect(error?.getAttribute('role')).toBe('status');
    expect(root.querySelector('[role="alert"]')).toBeNull();
    expect(error?.textContent).toContain('تعذر الوصول إلى الخادم');

    const retry = root.querySelector<HTMLButtonElement>('[data-testid="footer-health-retry"]');
    expect(retry).not.toBeNull();
    retry!.click();
    fixture.detectChanges();

    // The failing source errors again, so the footer stays in its scoped error block rather
    // than collapsing to a fixed height or losing the retry affordance.
    expect(root.querySelector('[data-testid="footer-health-error"]')).not.toBeNull();
    expect(root.querySelector('[data-testid="footer-health-retry"]')).not.toBeNull();
  });

  it('re-requests health from the same endpoint when retry is activated', () => {
    const responses = new Subject<HealthStatus>();
    const getHealth = vi.fn(() => responses.asObservable());

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [FooterComponent],
      providers: [{ provide: SystemApi, useValue: { getHealth } }],
    });
    const fixture = TestBed.createComponent(FooterComponent);
    fixture.detectChanges();

    expect(getHealth).toHaveBeenCalledTimes(1);

    fixture.componentInstance.retry();
    fixture.detectChanges();

    expect(getHealth).toHaveBeenCalledTimes(2);
  });
});
