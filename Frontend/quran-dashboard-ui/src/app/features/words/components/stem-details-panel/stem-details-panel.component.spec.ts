import { describe, expect, it, afterEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { provideRouter } from '@angular/router';

import { StemDetailsPanelComponent } from './stem-details-panel.component';
import { STEM_VIEW_KEYS, StemView } from '../../models/stems.models';

describe('StemDetailsPanelComponent a11y (T087)', () => {
  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  function createPanel(view: StemView = 'surahs') {
    TestBed.configureTestingModule({
      imports: [StemDetailsPanelComponent],
      // The drawer suspends its focus trap from the router-backed
      // detail-overlay history service.
      providers: [provideRouter([]), provideLocationMocks()],
      teardown: { destroyAfterEach: true },
    });

    const fixture = TestBed.createComponent(StemDetailsPanelComponent);
    fixture.componentRef.setInput('view', view);
    fixture.componentRef.setInput('emptySelection', false);
    fixture.detectChanges();
    return fixture;
  }

  it('composes the shared details workspace and mounts four labeled tabpanels', () => {
    const fixture = createPanel('surahs');
    const host = fixture.nativeElement as HTMLElement;

    const tablist = host.querySelector('[role="tablist"]');
    expect(tablist).toBeTruthy();

    const tabs = host.querySelectorAll('[role="tab"]');
    expect(tabs).toHaveLength(4);

    expect(host.querySelector('qd-details-workspace')).toBeTruthy();
    expect(host.querySelectorAll('[role="tabpanel"]')).toHaveLength(4);

    const active = host.querySelector('[data-testid="stem-details-panel-surface"]') as HTMLElement;
    expect(active.getAttribute('tabindex')).toBe('0');

    for (const tab of Array.from(tabs)) {
      const panel = host.querySelector(`#${tab.getAttribute('aria-controls')}`) as HTMLElement;
      expect(panel).toBeTruthy();
      expect(panel.getAttribute('aria-labelledby')).toBe(tab.id);
    }
  });

  it('marks the active tab selected with roving tabindex and labels the panel', () => {
    const fixture = createPanel('surahs');
    const host = fixture.nativeElement as HTMLElement;

    for (const key of STEM_VIEW_KEYS) {
      const tab = host.querySelector(`[data-stem-tab="${key}"]`) as HTMLElement;
      const isActive = key === 'surahs';
      expect(tab.getAttribute('aria-selected')).toBe(String(isActive));
      expect(tab.getAttribute('tabindex')).toBe(isActive ? '0' : '-1');
    }

    const activeTab = host.querySelector('[data-stem-tab="surahs"]') as HTMLElement;
    const panel = host.querySelector('[data-testid="stem-details-panel-surface"]') as HTMLElement;
    expect(activeTab.id).toBeTruthy();
    expect(panel.getAttribute('aria-labelledby')).toBe(activeTab.id);
  });

  it('moves selection forward in RTL reading order on ArrowLeft', () => {
    const fixture = createPanel('ayahs');
    const host = fixture.nativeElement as HTMLElement;

    let emitted: StemView | undefined;
    fixture.componentInstance.viewChange.subscribe((view) => (emitted = view));

    const activeTab = host.querySelector('[data-stem-tab="ayahs"]') as HTMLElement;
    activeTab.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }));

    expect(emitted).toBe('surahs');
  });

  it('renders the tabpanel surface as a flex column for nested list viewports', () => {
    const fixture = createPanel('surahs');
    const surface = fixture.nativeElement.querySelector(
      '[data-testid="stem-details-panel-surface"]',
    ) as HTMLElement;

    const styles = getComputedStyle(surface);
    expect(styles.display).toBe('flex');
    expect(styles.flexDirection).toBe('column');
  });

  it('renders the empty-selection state with header, disabled tabs, and empty message', () => {
    TestBed.configureTestingModule({
      imports: [StemDetailsPanelComponent],
      // The drawer suspends its focus trap from the router-backed
      // detail-overlay history service.
      providers: [provideRouter([]), provideLocationMocks()],
      teardown: { destroyAfterEach: true },
    });

    const fixture = TestBed.createComponent(StemDetailsPanelComponent);
    fixture.componentRef.setInput('view', 'surahs');
    fixture.componentRef.setInput('emptySelection', true);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('[data-testid="stem-details-panel-label"]')?.textContent?.trim()).toBe(
      'تفاصيل الأصل الصرفي',
    );
    expect(host.querySelector('[data-testid="stem-details-empty-selection"]')).toBeTruthy();
    expect(host.querySelectorAll('[role="tab"]')).toHaveLength(4);
    expect(host.querySelector('[role="tablist"]')).toBeTruthy();

    const tabs = host.querySelectorAll('[role="tab"]');
    for (const tab of Array.from(tabs)) {
      expect((tab as HTMLButtonElement).disabled).toBe(true);
      expect(tab.getAttribute('aria-selected')).toBe('false');
    }
  });

  it('keeps controlled not-found content inside its labeled tabpanel', () => {
    const fixture = createPanel('words');
    fixture.componentRef.setInput('notFound', true);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const notFound = host.querySelector('[data-testid="stem-details-not-found"]');
    expect(notFound).toBeTruthy();
    expect(host.querySelector('[role="tablist"]')).toBeTruthy();
    expect(host.querySelectorAll('[role="tab"]')).toHaveLength(4);

    const activeTab = host.querySelector('[data-stem-tab="words"]') as HTMLElement;
    const surface = host.querySelector('[data-testid="stem-details-panel-surface"]') as HTMLElement;
    expect(surface.getAttribute('role')).toBe('tabpanel');
    expect(surface.getAttribute('aria-labelledby')).toBe(activeTab.id);
    expect(surface.contains(notFound)).toBe(true);
  });
});

describe('StemDetailsPanelComponent modal drawer mode (T118)', () => {
  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  function createModalPanel() {
    TestBed.configureTestingModule({
      imports: [StemDetailsPanelComponent],
      // The drawer suspends its focus trap from the router-backed
      // detail-overlay history service.
      providers: [provideRouter([]), provideLocationMocks()],
      teardown: { destroyAfterEach: true },
    });

    const fixture = TestBed.createComponent(StemDetailsPanelComponent);
    fixture.componentRef.setInput('view', 'surahs');
    fixture.componentRef.setInput('inline', false);
    fixture.componentRef.setInput('emptySelection', false);
    fixture.componentRef.setInput('selectionTitle', 'عَلِمَ');
    fixture.detectChanges();
    return fixture;
  }

  it('renders a modal dialog with backdrop, role=dialog, and aria-modal when inline=false', () => {
    const fixture = createModalPanel();
    const host = fixture.nativeElement as HTMLElement;

    expect(host.querySelector('[data-testid="stem-details-panel-backdrop"]')).toBeTruthy();
    const dialog = host.querySelector('[data-testid="stem-details-modal"]') as HTMLElement;
    expect(dialog).toBeTruthy();
    expect(dialog.getAttribute('role')).toBe('dialog');
    expect(dialog.getAttribute('aria-modal')).toBe('true');
    const labelledBy = dialog.getAttribute('aria-labelledby') as string;
    expect(labelledBy).toBeTruthy();
    expect(host.querySelector('#' + labelledBy)?.textContent?.trim()).toBe('تفاصيل الأصل الصرفي');
  });

  it('emits close when the backdrop is clicked directly', () => {
    const fixture = createModalPanel();
    const host = fixture.nativeElement as HTMLElement;

    let closed = false;
    fixture.componentInstance.close.subscribe(() => (closed = true));

    const backdrop = host.querySelector('[data-testid="stem-details-panel-backdrop"]') as HTMLElement;
    backdrop.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(closed).toBe(true);
  });

  it('does not emit close when a click inside the modal bubbles to the backdrop', () => {
    const fixture = createModalPanel();
    const host = fixture.nativeElement as HTMLElement;

    let closed = false;
    fixture.componentInstance.close.subscribe(() => (closed = true));

    const dialog = host.querySelector('[data-testid="stem-details-modal"]') as HTMLElement;
    dialog.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(closed).toBe(false);
  });

  it('emits close on Escape key from inside the modal', () => {
    const fixture = createModalPanel();
    const host = fixture.nativeElement as HTMLElement;

    let closed = false;
    fixture.componentInstance.close.subscribe(() => (closed = true));

    const dialog = host.querySelector('[data-testid="stem-details-modal"]') as HTMLElement;
    dialog.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));

    expect(closed).toBe(true);
  });

  it('emits close from the header close button', () => {
    const fixture = createModalPanel();
    const host = fixture.nativeElement as HTMLElement;

    let closed = false;
    fixture.componentInstance.close.subscribe(() => (closed = true));

    const closeBtn = host.querySelector('[data-testid="stem-details-panel-close"]') as HTMLButtonElement;
    closeBtn.click();

    expect(closed).toBe(true);
  });

  it('renders no modal chrome when empty selection even in modal mode', () => {
    TestBed.configureTestingModule({
      imports: [StemDetailsPanelComponent],
      // The drawer suspends its focus trap from the router-backed
      // detail-overlay history service.
      providers: [provideRouter([]), provideLocationMocks()],
      teardown: { destroyAfterEach: true },
    });

    const fixture = TestBed.createComponent(StemDetailsPanelComponent);
    fixture.componentRef.setInput('view', 'surahs');
    fixture.componentRef.setInput('inline', false);
    fixture.componentRef.setInput('emptySelection', true);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('[data-testid="stem-details-modal"]')).toBeNull();
    expect(host.querySelector('[data-testid="stem-details-panel-backdrop"]')).toBeNull();
  });
});
