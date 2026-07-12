import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { WordTypeDetailsPanelComponent } from './word-type-details-panel.component';
import { WORD_TYPE_DETAIL_VIEW_KEYS, WordTypeDetailView } from '../../models/word-types.models';
import { WordTypeDetailSelectionKind } from '../../models/word-types-detail.models';

describe('WordTypeDetailsPanelComponent', () => {
  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  function createPanel(view: WordTypeDetailView = 'ayahs', kind: WordTypeDetailSelectionKind = 'word') {
    TestBed.configureTestingModule({
      imports: [WordTypeDetailsPanelComponent],
      teardown: { destroyAfterEach: true },
    });
    const fixture = TestBed.createComponent(WordTypeDetailsPanelComponent);
    fixture.componentRef.setInput('view', view);
    fixture.componentRef.setInput('kind', kind);
    fixture.componentRef.setInput('emptySelection', false);
    fixture.detectChanges();
    return fixture;
  }

  function tabKeys(host: HTMLElement): (string | null)[] {
    return Array.from(host.querySelectorAll('[role="tab"]')).map((tab) => tab.getAttribute('data-word-type-tab'));
  }

  it('renders only ayahs and surahs tabs linked to a single tabpanel', () => {
    const fixture = createPanel('ayahs');
    const host = fixture.nativeElement as HTMLElement;

    expect(host.querySelectorAll('[role="tab"]')).toHaveLength(2);
    expect(host.querySelector('[role="tabpanel"]')?.id).toBe('word-type-details-panel-surface');
    expect(host.querySelector('[data-word-type-tab="analysis"]')).toBeNull();
  });

  it('marks the active tab selected with roving tabindex', () => {
    const fixture = createPanel('surahs');
    const host = fixture.nativeElement as HTMLElement;

    for (const key of WORD_TYPE_DETAIL_VIEW_KEYS) {
      const tab = host.querySelector(`[data-word-type-tab="${key}"]`) as HTMLElement;
      expect(tab.getAttribute('aria-selected')).toBe(String(key === 'surahs'));
      expect(tab.getAttribute('tabindex')).toBe(key === 'surahs' ? '0' : '-1');
    }
  });

  it('hides tabs when notFound is true', () => {
    const fixture = createPanel('ayahs');
    fixture.componentRef.setInput('notFound', true);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('[role="tablist"]')).toBeNull();
  });

  it('shows ayahs and surahs tabs for a word kind', () => {
    const fixture = createPanel('ayahs', 'word');
    expect(tabKeys(fixture.nativeElement as HTMLElement)).toEqual(['ayahs', 'surahs']);
  });

  it.each(['root', 'stem', 'lemma'] as const)(
    'shows words, ayahs, and surahs tabs with RTL roving focus for a %s kind',
    (kind) => {
      const fixture = createPanel('words', kind);
      const host = fixture.nativeElement as HTMLElement;
      expect(tabKeys(host)).toEqual(['words', 'ayahs', 'surahs']);

      const wordsTab = host.querySelector('[data-word-type-tab="words"]') as HTMLElement;
      expect(wordsTab.getAttribute('tabindex')).toBe('0');
      expect(host.querySelector('[data-word-type-tab="ayahs"]')?.getAttribute('tabindex')).toBe('-1');

      const views: WordTypeDetailView[] = [];
      fixture.componentInstance.viewChange.subscribe((view) => views.push(view));
      // RTL: ArrowLeft advances to the next tab in reading order.
      wordsTab.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }));
      fixture.detectChanges();

      expect(views).toEqual(['ayahs']);
      expect(document.activeElement?.getAttribute('data-word-type-tab')).toBe('ayahs');
    },
  );

  it('keeps tabs disabled for an empty selection while the panel surface remains', () => {
    const fixture = createPanel('ayahs', 'word');
    fixture.componentRef.setInput('emptySelection', true);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const tabs = Array.from(host.querySelectorAll('[role="tab"]')) as HTMLButtonElement[];
    expect(tabs.length).toBeGreaterThan(0);
    expect(tabs.every((tab) => tab.disabled)).toBe(true);
    expect(host.querySelector('[role="tabpanel"]')).not.toBeNull();
    expect(host.querySelector('[data-testid="word-type-details-empty-selection"]')).not.toBeNull();
  });

  it('renders drawer chrome and emits close on Escape outside inline mode', () => {
    const fixture = createPanel('ayahs');
    let closed = 0;
    fixture.componentRef.setInput('inline', false);
    fixture.componentInstance.close.subscribe(() => closed += 1);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('[data-testid="word-type-details-panel-backdrop"]')).not.toBeNull();

    const modal = host.querySelector('[data-testid="word-type-details-modal"]') as HTMLElement;
    modal.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));

    expect(closed).toBe(1);
  });
});
