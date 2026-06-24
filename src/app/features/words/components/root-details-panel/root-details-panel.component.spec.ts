import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { RootDetailsPanelComponent } from './root-details-panel.component';
import { ROOT_VIEW_KEYS, RootView } from '../../models/roots.models';

/**
 * US-cross-cutting (T070): accessibility wiring of the persistent detail panel —
 * tablist/tab/tabpanel roles, roving tabindex, `aria-controls`/`aria-labelledby`
 * linkage, and RTL-aware arrow-key navigation.
 */
describe('RootDetailsPanelComponent a11y (T070)', () => {
  function createPanel(view: RootView = 'ayahs') {
    TestBed.configureTestingModule({ imports: [RootDetailsPanelComponent] });
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
    expect(panel.id).toBe('root-details-panel-surface');
    // The panel surface is focusable so keyboard users can scroll it.
    expect(panel.getAttribute('tabindex')).toBe('0');

    for (const tab of Array.from(tabs)) {
      expect(tab.getAttribute('aria-controls')).toBe('root-details-panel-surface');
    }
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

    const panel = host.querySelector('[role="tabpanel"]') as HTMLElement;
    expect(panel.getAttribute('aria-labelledby')).toBe('root-details-tabbtn-ayahs');
  });

  it('moves selection forward in RTL reading order on ArrowLeft', () => {
    const fixture = createPanel('ayahs');
    const host = fixture.nativeElement as HTMLElement;

    let emitted: RootView | undefined;
    fixture.componentInstance.viewChange.subscribe((view) => (emitted = view));

    const activeTab = host.querySelector('[data-root-tab="ayahs"]') as HTMLElement;
    activeTab.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }));

    // DOM order is ['words','ayahs','surahs','lemmas','stems']; forward from
    // 'ayahs' is 'surahs'.
    expect(emitted).toBe('surahs');
  });

  it('renders the empty-selection state without a tablist', () => {
    TestBed.configureTestingModule({ imports: [RootDetailsPanelComponent] });
    const fixture = TestBed.createComponent(RootDetailsPanelComponent);
    fixture.componentRef.setInput('view', 'ayahs');
    fixture.componentRef.setInput('emptySelection', true);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('[data-testid="root-details-empty-selection"]')).toBeTruthy();
    expect(host.querySelector('[role="tablist"]')).toBeNull();
  });
});
