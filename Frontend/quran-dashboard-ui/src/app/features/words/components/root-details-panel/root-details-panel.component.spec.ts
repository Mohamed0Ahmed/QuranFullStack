import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { provideRouter } from '@angular/router';

import { RootDetailsPanelComponent } from './root-details-panel.component';
import { ROOT_VIEW_KEYS, RootView } from '../../models/roots.models';
import { ROOTS_NOT_FOUND_LABEL, ROOTS_PANEL_LABEL } from '../../models/roots.labels';

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

  it('renders a tablist with exactly the five tabs linked to a single tabpanel', () => {
    const fixture = createPanel('ayahs');
    const host = fixture.nativeElement as HTMLElement;

    const tablist = host.querySelector('[role="tablist"]');
    expect(tablist).toBeTruthy();

    const tabs = host.querySelectorAll('[role="tab"]');
    expect(tabs).toHaveLength(5);

    const panel = host.querySelector('[role="tabpanel"]') as HTMLElement;
    // The surface id is per-instance (not a shared constant); the tabs must resolve to it.
    expect(panel.id).toBeTruthy();

    expect(panel.getAttribute('tabindex')).toBe('0');

    for (const tab of Array.from(tabs)) {
      expect(tab.getAttribute('aria-controls')).toBe(panel.id);
    }
  });

  it('gives each panel instance distinct surface and tab ids', () => {
    TestBed.configureTestingModule({
      imports: [RootDetailsPanelComponent],
      providers: [provideRouter([]), provideLocationMocks()],
      teardown: { destroyAfterEach: true },
    });

    const makeSurface = () => {
      const fixture = TestBed.createComponent(RootDetailsPanelComponent);
      fixture.componentRef.setInput('view', 'ayahs');
      fixture.componentRef.setInput('emptySelection', false);
      fixture.detectChanges();
      return (fixture.nativeElement as HTMLElement).querySelector(
        '[data-testid="root-details-panel-surface"]',
      ) as HTMLElement;
    };

    const firstSurface = makeSurface();
    const secondSurface = makeSurface();

    expect(firstSurface.id).toBeTruthy();
    expect(secondSurface.id).toBeTruthy();
    expect(firstSurface.id).not.toBe(secondSurface.id);
  });

  // Feature 030, N3 row 5: notFound is a PANEL/selection state (the list is fine and populated), so
  // it renders here — a mounted, fixed-height shell — instead of a page banner that pushed the grid.
  it('renders controlled not-found content without detail tabs', () => {
    const fixture = createPanel('words');
    fixture.componentRef.setInput('notFound', true);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const notFound = host.querySelector('[data-testid="root-details-not-found"]');
    expect(notFound).toBeTruthy();
    expect(notFound?.getAttribute('role')).toBe('status');
    expect(notFound?.textContent?.trim()).toBe(ROOTS_NOT_FOUND_LABEL);
    expect(host.querySelector('[role="tablist"]')).toBeNull();
    expect(host.querySelectorAll('[role="tab"]')).toHaveLength(0);
  });

  it('prefers the server not-found message and drops tabpanel semantics once the tabs are gone', () => {
    const fixture = createPanel('words');
    fixture.componentRef.setInput('notFound', true);
    fixture.componentRef.setInput('notFoundMessage', 'الجذر غير موجود');
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('[data-testid="root-details-not-found"]')?.textContent?.trim()).toBe(
      'الجذر غير موجود',
    );

    // No tablist is rendered, so exposing a tabpanel would orphan it: the surface
    // becomes a self-labelled region instead of a tabpanel pointing at a missing tab.
    expect(host.querySelector('[role="tabpanel"]')).toBeNull();
    const surface = host.querySelector('[data-testid="root-details-panel-surface"]') as HTMLElement;
    expect(surface.getAttribute('role')).toBe('region');
    expect(surface.getAttribute('aria-labelledby')).toBeNull();
    expect(surface.getAttribute('aria-label')).toBe(ROOTS_PANEL_LABEL);
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
    const panel = host.querySelector('[role="tabpanel"]') as HTMLElement;
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
      // The drawer suspends its focus trap from the router-backed
      // detail-overlay history service.
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
