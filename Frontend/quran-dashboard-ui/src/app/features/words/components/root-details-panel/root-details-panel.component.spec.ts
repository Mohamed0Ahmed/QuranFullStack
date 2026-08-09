import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { provideRouter } from '@angular/router';

import { RootDetailsPanelComponent } from './root-details-panel.component';
import { ROOT_VIEW_KEYS, RootView } from '../../models/roots.models';
import { ROOTS_NOT_FOUND_LABEL } from '../../models/roots.labels';

describe('RootDetailsPanelComponent a11y (T070)', () => {
  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  function createPanel(view: RootView = 'ayahs') {
    TestBed.configureTestingModule({
      imports: [RootDetailsPanelComponent],
      // The drawer suspends its focus trap from the router-backed
      // detail-overlay history service.
      providers: [provideRouter([]), provideLocationMocks()],
      teardown: { destroyAfterEach: true },
    });
    const fixture = TestBed.createComponent(RootDetailsPanelComponent);
    fixture.componentRef.setInput('view', view);
    fixture.componentRef.setInput('emptySelection', false);
    fixture.detectChanges();
    return fixture;
  }

  it('composes the shared details workspace and mounts five labeled tabpanels', () => {
    const fixture = createPanel('ayahs');
    const host = fixture.nativeElement as HTMLElement;

    const tablist = host.querySelector('[role="tablist"]');
    expect(tablist).toBeTruthy();

    const tabs = host.querySelectorAll('[role="tab"]');
    expect(tabs).toHaveLength(5);

    expect(host.querySelector('qd-details-workspace')).toBeTruthy();
    const panels = host.querySelectorAll('[role="tabpanel"]');
    expect(panels).toHaveLength(5);

    for (const tab of Array.from(tabs)) {
      const panel = host.querySelector(`#${tab.getAttribute('aria-controls')}`) as HTMLElement;
      expect(panel).toBeTruthy();
      expect(panel.getAttribute('aria-labelledby')).toBe(tab.id);
    }
  });

  it('gives each panel instance distinct surface and tab ids', () => {
    TestBed.configureTestingModule({
      imports: [RootDetailsPanelComponent],
      providers: [provideRouter([]), provideLocationMocks()],
      teardown: { destroyAfterEach: true },
    });

    const makeIds = () => {
      const fixture = TestBed.createComponent(RootDetailsPanelComponent);
      fixture.componentRef.setInput('view', 'ayahs');
      fixture.componentRef.setInput('emptySelection', false);
      fixture.detectChanges();
      const host = fixture.nativeElement as HTMLElement;
      return Array.from(host.querySelectorAll('[role="tab"], [role="tabpanel"]')).map((element) => element.id);
    };

    const firstIds = makeIds();
    const secondIds = makeIds();

    expect(firstIds.every(Boolean)).toBe(true);
    expect(secondIds.every(Boolean)).toBe(true);
    expect(firstIds.some((id) => secondIds.includes(id))).toBe(false);
  });

  it('keeps controlled not-found content inside its labeled tabpanel', () => {
    const fixture = createPanel('words');
    fixture.componentRef.setInput('notFound', true);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const notFound = host.querySelector('[data-testid="root-details-not-found"]');
    expect(notFound).toBeTruthy();
    expect(notFound?.getAttribute('role')).toBe('status');
    expect(notFound?.textContent?.trim()).toBe(ROOTS_NOT_FOUND_LABEL);
    expect(host.querySelector('[role="tablist"]')).toBeTruthy();
    expect(host.querySelectorAll('[role="tab"]')).toHaveLength(5);
    const activeTab = host.querySelector('[data-root-tab="words"]') as HTMLElement;
    const panel = host.querySelector(`#${activeTab.getAttribute('aria-controls')}`) as HTMLElement;
    expect(panel.getAttribute('aria-labelledby')).toBe(activeTab.id);
    expect(panel.contains(notFound)).toBe(true);
  });

  it('prefers the server not-found message while retaining tabpanel semantics', () => {
    const fixture = createPanel('words');
    fixture.componentRef.setInput('notFound', true);
    fixture.componentRef.setInput('notFoundMessage', 'الجذر غير موجود');
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('[data-testid="root-details-not-found"]')?.textContent?.trim()).toBe(
      'الجذر غير موجود',
    );

    const surface = host.querySelector('[data-testid="root-details-panel-surface"]') as HTMLElement;
    expect(surface.getAttribute('role')).toBe('tabpanel');
    expect(surface.getAttribute('aria-labelledby')).toBeTruthy();
  });

  it('marks the active tab selected with roving tabindex and labels the panel', () => {
    const fixture = createPanel('ayahs');
    const host = fixture.nativeElement as HTMLElement;

    for (const key of ROOT_VIEW_KEYS) {
      const tab = host.querySelector(`[data-root-tab="${key}"]`) as HTMLElement;
      const isActive = key === 'ayahs';
      expect(tab.getAttribute('aria-selected')).toBe(String(isActive));
      expect(tab.getAttribute('tabindex')).toBe(isActive ? '0' : '-1');
    }

    const activeTab = host.querySelector('[data-root-tab="ayahs"]') as HTMLElement;
    const panel = host.querySelector('[data-testid="root-details-panel-surface"]') as HTMLElement;
    expect(activeTab.id).toBeTruthy();
    expect(panel.getAttribute('aria-labelledby')).toBe(activeTab.id);
  });

  it('moves selection forward in RTL reading order on ArrowLeft', () => {
    const fixture = createPanel('ayahs');
    const host = fixture.nativeElement as HTMLElement;

    let emitted: RootView | undefined;
    fixture.componentInstance.viewChange.subscribe((view) => (emitted = view));

    const activeTab = host.querySelector('[data-root-tab="ayahs"]') as HTMLElement;
    activeTab.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }));

    expect(emitted).toBe('surahs');
  });

  it('renders the tabpanel surface as a flex column for nested list viewports', () => {
    const fixture = createPanel('ayahs');
    const surface = fixture.nativeElement.querySelector(
      '[data-testid="root-details-panel-surface"]',
    ) as HTMLElement;

    const styles = getComputedStyle(surface);
    expect(styles.display).toBe('flex');
    expect(styles.flexDirection).toBe('column');
  });

  it('renders the empty-selection state with header, disabled tabs, and empty message', () => {
    TestBed.configureTestingModule({
      imports: [RootDetailsPanelComponent],
      providers: [provideRouter([]), provideLocationMocks()],
      teardown: { destroyAfterEach: true },
    });
    const fixture = TestBed.createComponent(RootDetailsPanelComponent);
    fixture.componentRef.setInput('view', 'ayahs');
    fixture.componentRef.setInput('emptySelection', true);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('[data-testid="root-details-panel-label"]')?.textContent?.trim()).toBe(
      'تفاصيل الجذر',
    );
    expect(host.querySelector('[data-testid="root-details-empty-selection"]')).toBeTruthy();
    expect(host.querySelectorAll('[role="tab"]')).toHaveLength(5);
    expect(host.querySelector('[role="tablist"]')).toBeTruthy();

    const tabs = host.querySelectorAll('[role="tab"]');
    for (const tab of Array.from(tabs)) {
      expect((tab as HTMLButtonElement).disabled).toBe(true);
      expect(tab.getAttribute('aria-selected')).toBe('false');
    }
  });
});
