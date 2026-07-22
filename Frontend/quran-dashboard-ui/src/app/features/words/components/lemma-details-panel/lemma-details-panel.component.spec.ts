import { describe, expect, it, afterEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { provideRouter } from '@angular/router';

import { LemmaDetailsPanelComponent } from './lemma-details-panel.component';
import { LEMMA_VIEW_KEYS, LemmaView } from '../../models/lemmas.models';

describe('LemmaDetailsPanelComponent a11y (T087)', () => {
  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  function createPanel(view: LemmaView = 'surahs') {
    TestBed.configureTestingModule({
      imports: [LemmaDetailsPanelComponent],
      providers: [provideRouter([]), provideLocationMocks()],
      teardown: { destroyAfterEach: true },
    });

    const fixture = TestBed.createComponent(LemmaDetailsPanelComponent);
    fixture.componentRef.setInput('view', view);
    fixture.componentRef.setInput('emptySelection', false);
    fixture.detectChanges();
    return fixture;
  }

  it('renders a tablist with exactly the four tabs linked to a single tabpanel', () => {
    const fixture = createPanel('surahs');
    const host = fixture.nativeElement as HTMLElement;

    const tablist = host.querySelector('[role="tablist"]');
    expect(tablist).toBeTruthy();

    const tabs = host.querySelectorAll('[role="tab"]');
    expect(tabs).toHaveLength(4);

    const panel = host.querySelector('[role="tabpanel"]') as HTMLElement;
    expect(panel.id).toBe('lemma-details-panel-surface');
    expect(panel.getAttribute('tabindex')).toBe('0');

    for (const tab of Array.from(tabs)) {
      expect(tab.getAttribute('aria-controls')).toBe('lemma-details-panel-surface');
    }
  });

  it('marks the active tab selected with roving tabindex and labels the panel', () => {
    const fixture = createPanel('surahs');
    const host = fixture.nativeElement as HTMLElement;

    for (const key of LEMMA_VIEW_KEYS) {
      const tab = host.querySelector(`[data-lemma-tab="${key}"]`) as HTMLElement;
      const isActive = key === 'surahs';
      expect(tab.getAttribute('aria-selected')).toBe(String(isActive));
      expect(tab.getAttribute('tabindex')).toBe(isActive ? '0' : '-1');
    }

    const panel = host.querySelector('[role="tabpanel"]') as HTMLElement;
    expect(panel.getAttribute('aria-labelledby')).toBe('lemma-details-tabbtn-surahs');
  });

  it('moves selection forward in RTL reading order on ArrowLeft', () => {
    const fixture = createPanel('ayahs');
    const host = fixture.nativeElement as HTMLElement;

    let emitted: LemmaView | undefined;
    fixture.componentInstance.viewChange.subscribe((view) => (emitted = view));

    const activeTab = host.querySelector('[data-lemma-tab="ayahs"]') as HTMLElement;
    activeTab.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }));

    expect(emitted).toBe('surahs');
  });

  it('renders the tabpanel surface as a flex column for nested list viewports', () => {
    const fixture = createPanel('surahs');
    const surface = fixture.nativeElement.querySelector(
      '[data-testid="lemma-details-panel-surface"]',
    ) as HTMLElement;

    const styles = getComputedStyle(surface);
    expect(styles.display).toBe('flex');
    expect(styles.flexDirection).toBe('column');
  });

  it('renders the empty-selection state with header, disabled tabs, and empty message', () => {
    TestBed.configureTestingModule({
      imports: [LemmaDetailsPanelComponent],
      providers: [provideRouter([]), provideLocationMocks()],
      teardown: { destroyAfterEach: true },
    });

    const fixture = TestBed.createComponent(LemmaDetailsPanelComponent);
    fixture.componentRef.setInput('view', 'surahs');
    fixture.componentRef.setInput('emptySelection', true);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('[data-testid="lemma-details-panel-label"]')?.textContent?.trim()).toBe(
      'تفاصيل الصيغة المعجمية',
    );
    expect(host.querySelector('[data-testid="lemma-details-empty-selection"]')).toBeTruthy();
    expect(host.querySelectorAll('[role="tab"]')).toHaveLength(4);
    expect(host.querySelector('[role="tablist"]')).toBeTruthy();

    const tabs = host.querySelectorAll('[role="tab"]');
    for (const tab of Array.from(tabs)) {
      expect((tab as HTMLButtonElement).disabled).toBe(true);
      expect(tab.getAttribute('aria-selected')).toBe('false');
    }
  });

  it('renders controlled not-found content without detail tabs', () => {
    const fixture = createPanel('words');
    fixture.componentRef.setInput('notFound', true);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('[data-testid="lemma-details-not-found"]')).toBeTruthy();
    expect(host.querySelector('[role="tablist"]')).toBeNull();
    expect(host.querySelectorAll('[role="tab"]')).toHaveLength(0);
  });
});

describe('LemmaDetailsPanelComponent modal drawer mode (T118)', () => {
  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  function createModalPanel() {
    TestBed.configureTestingModule({
      imports: [LemmaDetailsPanelComponent],
      providers: [provideRouter([]), provideLocationMocks()],
      teardown: { destroyAfterEach: true },
    });

    const fixture = TestBed.createComponent(LemmaDetailsPanelComponent);
    fixture.componentRef.setInput('view', 'surahs');
    fixture.componentRef.setInput('inline', false);
    fixture.componentRef.setInput('emptySelection', false);
    fixture.componentRef.setInput('selectionTitle', 'كَلِمَة');
    fixture.detectChanges();
    return fixture;
  }

  it('renders a modal dialog with backdrop, role=dialog, and aria-modal when inline=false', () => {
    const fixture = createModalPanel();
    const host = fixture.nativeElement as HTMLElement;

    expect(host.querySelector('[data-testid="lemma-details-panel-backdrop"]')).toBeTruthy();
    const dialog = host.querySelector('[data-testid="lemma-details-modal"]') as HTMLElement;
    expect(dialog).toBeTruthy();
    expect(dialog.getAttribute('role')).toBe('dialog');
    expect(dialog.getAttribute('aria-modal')).toBe('true');
    expect(dialog.getAttribute('aria-label')).toBe('تفاصيل الصيغة المعجمية');
  });

  it('emits close when the backdrop is clicked directly', () => {
    const fixture = createModalPanel();
    const host = fixture.nativeElement as HTMLElement;

    let closed = false;
    fixture.componentInstance.close.subscribe(() => (closed = true));

    const backdrop = host.querySelector('[data-testid="lemma-details-panel-backdrop"]') as HTMLElement;
    backdrop.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(closed).toBe(true);
  });

  it('does not emit close when a click inside the modal bubbles to the backdrop', () => {
    const fixture = createModalPanel();
    const host = fixture.nativeElement as HTMLElement;

    let closed = false;
    fixture.componentInstance.close.subscribe(() => (closed = true));

    const dialog = host.querySelector('[data-testid="lemma-details-modal"]') as HTMLElement;
    dialog.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(closed).toBe(false);
  });

  it('emits close on Escape key from inside the modal', () => {
    const fixture = createModalPanel();
    const host = fixture.nativeElement as HTMLElement;

    let closed = false;
    fixture.componentInstance.close.subscribe(() => (closed = true));

    const dialog = host.querySelector('[data-testid="lemma-details-modal"]') as HTMLElement;
    dialog.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));

    expect(closed).toBe(true);
  });

  it('emits close from the header close button', () => {
    const fixture = createModalPanel();
    const host = fixture.nativeElement as HTMLElement;

    let closed = false;
    fixture.componentInstance.close.subscribe(() => (closed = true));

    const closeBtn = host.querySelector('[data-testid="lemma-details-panel-close"]') as HTMLButtonElement;
    closeBtn.click();

    expect(closed).toBe(true);
  });

  it('renders no modal chrome when empty selection even in modal mode', () => {
    TestBed.configureTestingModule({
      imports: [LemmaDetailsPanelComponent],
      providers: [provideRouter([]), provideLocationMocks()],
      teardown: { destroyAfterEach: true },
    });

    const fixture = TestBed.createComponent(LemmaDetailsPanelComponent);
    fixture.componentRef.setInput('view', 'surahs');
    fixture.componentRef.setInput('inline', false);
    fixture.componentRef.setInput('emptySelection', true);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('[data-testid="lemma-details-modal"]')).toBeNull();
    expect(host.querySelector('[data-testid="lemma-details-panel-backdrop"]')).toBeNull();
  });
});
