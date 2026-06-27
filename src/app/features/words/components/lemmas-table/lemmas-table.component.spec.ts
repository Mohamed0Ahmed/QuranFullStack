import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { LemmasTableComponent } from './lemmas-table.component';
import { LemmaListItemViewModel } from '../../models/lemmas.models';

function row(id: number, overrides: Partial<LemmaListItemViewModel> = {}): LemmaListItemViewModel {
  return {
    id,
    lemmaText: `صيغة-${id}`,
    displayText: `صيغة-${id}`,
    rootId: id === 501 ? null : 700,
    rootText: id === 501 ? null : 'ك ل م',
    occurrencesCount: id * 10,
    ayahsCount: id * 2,
    surahsCount: id,
    simpleWordsCount: id + 1,
    tashkeelWordsCount: id + 2,
    stemsCount: id + 3,
    ...overrides,
  };
}

describe('LemmasTableComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [LemmasTableComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  function setup(
    rows: readonly LemmaListItemViewModel[],
    overrides: Record<string, unknown> = {},
  ) {
    const fixture = TestBed.createComponent(LemmasTableComponent);
    fixture.componentRef.setInput('rows', rows);
    for (const [key, value] of Object.entries(overrides)) {
      (fixture.componentRef as unknown as { setInput: (k: string, v: unknown) => void }).setInput(key, value);
    }
    fixture.detectChanges();
    return fixture;
  }

  it('renders the lemma headers without type column', () => {
    const fixture = setup([], { loading: false });
    const root = fixture.nativeElement as HTMLElement;

    const headers = Array.from(root.querySelectorAll('[role="columnheader"]')).map((h) =>
      h.textContent?.trim() ?? '',
    );
    expect(headers).toContain('م');
    expect(headers).toContain('الصيغة المعجمية');
    expect(headers).toContain('الجذر');
    expect(headers).toContain('المواضع');
    expect(headers).toContain('الآيات');
    expect(headers).toContain('السور');
    expect(headers).toContain('كلمات بدون تشكيل');
    expect(headers).toContain('كلمات بالتشكيل');
    expect(headers).toContain('الأصول الصرفية');
  });

  it('renders counts and UI row numbers, never backend ids', () => {
    const fixture = setup([row(1), row(2)], { currentPage: 1 });
    const root = fixture.nativeElement as HTMLElement;

    const dataRows = root.querySelectorAll('.lemmas-table__row:not(.lemmas-table__header-row)');
    expect(dataRows.length).toBeGreaterThanOrEqual(2);

    const rowNumbers = Array.from(
      root.querySelectorAll('.lemmas-table__cell--row-number'),
    ).map((c) => c.textContent?.trim() ?? '');
    expect(rowNumbers).toContain('1');
    expect(rowNumbers).toContain('2');

    expect(root.textContent).not.toContain('id-');
    expect(root.querySelectorAll('qd-word-count-chip')).toHaveLength(12);
  });

  it('shows a dash for null owned root and a safe new-tab root anchor when present', () => {
    const fixture = setup([row(501), row(502)]);
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="lemmas-table-root-missing"]')).toBeTruthy();

    const link = root.querySelector('[data-testid="lemmas-table-root-link"]') as HTMLAnchorElement | null;
    expect(link).toBeTruthy();
    expect(link?.getAttribute('target')).toBe('_blank');
    expect(link?.getAttribute('rel')).toBe('noopener noreferrer');
    expect(link?.getAttribute('href')).toContain('/dashboard/words/roots');
    expect(link?.getAttribute('href')).toContain('root=700');
  });

  it('does not render a type column', () => {
    const fixture = setup([row(500)]);
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="lemmas-table-type"]')).toBeNull();
  });

  it('emits rowSelected when the lemma cell is clicked', () => {
    const fixture = setup([row(1), row(2)]);
    const emitted: LemmaListItemViewModel[] = [];
    fixture.componentInstance.rowSelected.subscribe((lemma) => emitted.push(lemma));

    const buttons = fixture.nativeElement.querySelectorAll(
      '[data-testid="lemmas-table-lemma-button"]',
    ) as NodeListOf<HTMLElement>;
    buttons[1].click();
    fixture.detectChanges();

    expect(emitted).toHaveLength(1);
    expect(emitted[0].id).toBe(2);
  });

  it('marks the selected row with aria-selected', () => {
    const fixture = setup([row(1), row(2)], { selectedLemmaId: 2 });
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[aria-selected="true"]')).toBeTruthy();
  });

  it('emits countOpened with the mapped view and sub-view; zero counts stay enabled', () => {
    const fixture = setup([row(1, { ayahsCount: 0, simpleWordsCount: 0 })]);
    const emitted: {
      lemma: LemmaListItemViewModel;
      view: string;
      wordView?: string;
      surahView?: string;
    }[] = [];
    fixture.componentInstance.countOpened.subscribe((e) => emitted.push(e));

    const chips = fixture.nativeElement.querySelectorAll(
      'qd-word-count-chip',
    ) as NodeListOf<HTMLElement>;

    const expected = [
      { view: 'ayahs' },
      { view: 'ayahs' },
      { view: 'surahs', surahView: 'mentioned' },
      { view: 'words', wordView: 'simple' },
      { view: 'words', wordView: 'tashkeel' },
      { view: 'stems' },
    ];

    chips.forEach((chip) => {
      const btn = chip.querySelector('button');
      expect(btn?.hasAttribute('disabled')).toBe(false);
      btn?.click();
      fixture.detectChanges();
    });

    expect(emitted.map((e) => e.view)).toEqual(expected.map((e) => e.view));
    expect(emitted.map((e) => e.wordView ?? null)).toEqual(
      expected.map((e) => e.wordView ?? null),
    );
    expect(emitted.map((e) => e.surahView ?? null)).toEqual(
      expected.map((e) => e.surahView ?? null),
    );
  });
});
