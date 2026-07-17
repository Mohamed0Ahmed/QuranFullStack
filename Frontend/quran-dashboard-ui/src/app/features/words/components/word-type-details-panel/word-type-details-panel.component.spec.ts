import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { provideRouter } from '@angular/router';

import { WordTypeDetailsPanelComponent } from './word-type-details-panel.component';
import { WORD_TYPE_DETAIL_VIEW_KEYS, WordTypeDetailView } from '../../models/word-types.models';
import { WordTypeDetailSelectionKind } from '../../models/word-types-detail.models';

const KIND_PRESENTATION_CASES = [
  {
    kind: 'word',
    view: 'ayahs',
    panelLabel: 'تفاصيل كلمة النوع',
    tabs: [
      ['ayahs', 'الآيات الخاصة بالكلمة', 'الآيات الخاصة بالكلمة المحددة'],
      ['surahs', 'سور الكلمة', 'توزيع السور للكلمة المحددة'],
    ],
    emptySelection: 'اختر كلمة من الجدول لعرض تفاصيلها.',
    notFound: 'الكلمة المحددة غير موجودة',
  },
  {
    kind: 'root',
    view: 'words',
    panelLabel: 'تفاصيل الجذر',
    tabs: [
      ['words', 'كلمات الجذر', 'الكلمات المرتبطة بالجذر المحدد'],
      ['ayahs', 'آيات الجذر', 'الآيات المرتبطة بالجذر المحدد'],
      ['surahs', 'سور الجذر', 'توزيع السور للجذر المحدد'],
    ],
    emptySelection: 'اختر جذرًا من الجدول لعرض تفاصيله.',
    notFound: 'الجذر المحدد غير موجود',
  },
  {
    kind: 'stem',
    view: 'words',
    panelLabel: 'تفاصيل الأصل الصرفي',
    tabs: [
      ['words', 'كلمات الأصل الصرفي', 'الكلمات المرتبطة بالأصل الصرفي المحدد'],
      ['ayahs', 'آيات الأصل الصرفي', 'الآيات المرتبطة بالأصل الصرفي المحدد'],
      ['surahs', 'سور الأصل الصرفي', 'توزيع السور للأصل الصرفي المحدد'],
    ],
    emptySelection: 'اختر أصلًا صرفيًا من الجدول لعرض تفاصيله.',
    notFound: 'الأصل الصرفي المحدد غير موجود',
  },
  {
    kind: 'lemma',
    view: 'words',
    panelLabel: 'تفاصيل الصيغة المعجمية',
    tabs: [
      ['words', 'كلمات الصيغة المعجمية', 'الكلمات المرتبطة بالصيغة المعجمية المحددة'],
      ['ayahs', 'آيات الصيغة المعجمية', 'الآيات المرتبطة بالصيغة المعجمية المحددة'],
      ['surahs', 'سور الصيغة المعجمية', 'توزيع السور للصيغة المعجمية المحددة'],
    ],
    emptySelection: 'اختر صيغة معجمية من الجدول لعرض تفاصيلها.',
    notFound: 'الصيغة المعجمية المحددة غير موجودة',
  },
] as const satisfies readonly {
  kind: WordTypeDetailSelectionKind;
  view: WordTypeDetailView;
  panelLabel: string;
  tabs: readonly (readonly [WordTypeDetailView, string, string])[];
  emptySelection: string;
  notFound: string;
}[];

describe('WordTypeDetailsPanelComponent', () => {
  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  function createPanel(view: WordTypeDetailView = 'ayahs', kind: WordTypeDetailSelectionKind = 'word') {
    TestBed.configureTestingModule({
      imports: [WordTypeDetailsPanelComponent],
      // The drawer suspends its focus trap from the router-backed
      // detail-overlay history service.
      providers: [provideRouter([]), provideLocationMocks()],
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
    // The surface id is per-instance (not a shared constant); the tabs must resolve to it.
    const panel = host.querySelector('[role="tabpanel"]') as HTMLElement;
    expect(panel.id).toBeTruthy();
    for (const tab of Array.from(host.querySelectorAll('[role="tab"]'))) {
      expect(tab.getAttribute('aria-controls')).toBe(panel.id);
    }
    expect(host.querySelector('[data-word-type-tab="analysis"]')).toBeNull();
  });

  it('gives each panel instance distinct surface and tab ids', () => {
    TestBed.configureTestingModule({
      imports: [WordTypeDetailsPanelComponent],
      providers: [provideRouter([]), provideLocationMocks()],
      teardown: { destroyAfterEach: true },
    });

    const makeSurface = () => {
      const fixture = TestBed.createComponent(WordTypeDetailsPanelComponent);
      fixture.componentRef.setInput('view', 'ayahs');
      fixture.componentRef.setInput('kind', 'word');
      fixture.componentRef.setInput('emptySelection', false);
      fixture.detectChanges();
      return (fixture.nativeElement as HTMLElement).querySelector(
        '[data-testid="word-type-details-panel-surface"]',
      ) as HTMLElement;
    };

    const firstSurface = makeSurface();
    const secondSurface = makeSurface();

    expect(firstSurface.id).toBeTruthy();
    expect(secondSurface.id).toBeTruthy();
    expect(firstSurface.id).not.toBe(secondSurface.id);
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

  it.each(KIND_PRESENTATION_CASES)(
    'renders kind-aware panel, tab, and ARIA copy for $kind details',
    ({ kind, view, panelLabel, tabs }) => {
      const fixture = createPanel(view, kind);
      const host = fixture.nativeElement as HTMLElement;

      expect(host.querySelector('[data-testid="word-type-details-panel-label"]')?.textContent?.trim()).toBe(panelLabel);
      for (const [key, label, aria] of tabs) {
        const tab = host.querySelector(`[data-word-type-tab="${key}"]`);
        expect(tab?.textContent?.trim()).toBe(label);
        expect(tab?.getAttribute('aria-label')).toBe(aria);
      }
    },
  );

  it.each(KIND_PRESENTATION_CASES)(
    'renders kind-aware empty-selection guidance for $kind details',
    ({ kind, view, emptySelection }) => {
      const fixture = createPanel(view, kind);
      fixture.componentRef.setInput('emptySelection', true);
      fixture.detectChanges();

      const message = (fixture.nativeElement as HTMLElement)
        .querySelector('[data-testid="word-type-details-empty-selection"]');
      expect(message?.textContent?.trim()).toBe(emptySelection);
    },
  );

  it.each(KIND_PRESENTATION_CASES)(
    'renders kind-aware not-found guidance for $kind details',
    ({ kind, view, panelLabel, notFound }) => {
      const fixture = createPanel(view, kind);
      fixture.componentRef.setInput('notFound', true);
      fixture.detectChanges();

      const host = fixture.nativeElement as HTMLElement;
      const surface = host.querySelector('[role="tabpanel"]');
      expect(host.querySelector('[data-testid="word-type-details-not-found"]')?.textContent?.trim()).toBe(notFound);
      expect(surface?.getAttribute('aria-labelledby')).toBeNull();
      expect(surface?.getAttribute('aria-label')).toBe(panelLabel);
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
