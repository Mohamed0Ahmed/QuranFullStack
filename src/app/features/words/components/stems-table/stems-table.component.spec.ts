import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { StemsTableComponent } from './stems-table.component';
import { StemListItemViewModel, TypeSummaryDto } from '../../models/stems.models';

function typeSummary(code: string, label: string, count: number): TypeSummaryDto {
  return {
    code,
    arabicLabel: label,
    englishLabel: code,
    occurrencesCount: count,
    firstSurahNumber: 1,
    firstAyahNumber: 1,
    firstWordNumber: 1,
  };
}

function row(id: number, overrides: Partial<StemListItemViewModel> = {}): StemListItemViewModel {
  return {
    id,
    stemText: `أصل-${id}`,
    displayText: `أصل-${id}`,
    lemmaId: id === 601 ? null : 700,
    lemmaText: id === 601 ? null : 'صيغة-700',
    lemmaBuckwalter: null,
    rootId: id === 602 ? null : 800,
    rootText: id === 602 ? null : 'جذر-800',
    rootBuckwalter: null,
    dominantType: typeSummary('N', 'اسم', id * 10),
    otherTypesCount: id === 600 ? 1 : 0,
    occurrencesCount: id * 10,
    ayahsCount: id * 2,
    surahsCount: id,
    simpleWordsCount: id + 1,
    tashkeelWordsCount: id + 2,
    firstVerseKey: '1:1',
    ...overrides,
  };
}

describe('StemsTableComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [StemsTableComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  function setup(rows: readonly StemListItemViewModel[], overrides: Record<string, unknown> = {}) {
    const fixture = TestBed.createComponent(StemsTableComponent);
    fixture.componentRef.setInput('rows', rows);
    for (const [key, value] of Object.entries(overrides)) {
      (fixture.componentRef as unknown as { setInput: (k: string, v: unknown) => void }).setInput(key, value);
    }
    fixture.detectChanges();
    return fixture;
  }

  it('renders the semantic column headers', () => {
    const fixture = setup([], { loading: false });
    const root = fixture.nativeElement as HTMLElement;

    const headers = Array.from(root.querySelectorAll('[role="columnheader"]')).map((h) =>
      h.textContent?.trim() ?? '',
    );
    expect(headers).toContain('م');
    expect(headers).toContain('الأصل الصرفي');
    expect(headers).toContain('الصيغ');
    expect(headers).toContain('الجذور');
    expect(headers).toContain('النوع');
    expect(headers).toContain('المواضع');
    expect(headers).toContain('الآيات');
    expect(headers).toContain('السور');
    expect(headers).toContain('بدون تشكيل');
    expect(headers).toContain('بالتشكيل');
  });

  it('renders counts and row numbers without exposing backend ids', () => {
    const fixture = setup([row(1), row(2)], { currentPage: 1 });
    const root = fixture.nativeElement as HTMLElement;

    const rowNumbers = Array.from(root.querySelectorAll('.stems-table__cell--row-number')).map((c) =>
      c.textContent?.trim() ?? '',
    );
    expect(rowNumbers).toEqual(['1', '2']);

    expect(root.textContent).not.toContain('id-');
    expect(root.querySelectorAll('qd-word-count-chip')).toHaveLength(10);
  });

  it('renders safe lemma and root anchors or calm empty values', () => {
    const fixture = setup([row(601), row(602)]);
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="stems-table-lemma-missing"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="stems-table-root-missing"]')).toBeTruthy();

    const lemmaLink = root.querySelector('[data-testid="stems-table-lemma-link"]') as HTMLAnchorElement | null;
    expect(lemmaLink).toBeTruthy();
    expect(lemmaLink?.getAttribute('target')).toBe('_blank');
    expect(lemmaLink?.getAttribute('rel')).toBe('noopener noreferrer');
    expect(lemmaLink?.getAttribute('href')).toContain('/dashboard/words/lemmas');
    expect(lemmaLink?.getAttribute('href')).toContain('lemma=700');

    const rootLink = root.querySelector('[data-testid="stems-table-root-link"]') as HTMLAnchorElement | null;
    expect(rootLink).toBeTruthy();
    expect(rootLink?.getAttribute('target')).toBe('_blank');
    expect(rootLink?.getAttribute('rel')).toBe('noopener noreferrer');
    expect(rootLink?.getAttribute('href')).toContain('/dashboard/words/roots');
    expect(rootLink?.getAttribute('href')).toContain('root=800');
  });

  it('shows dominant type and an additional-types indicator when more types exist', () => {
    const fixture = setup([row(600)]);
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="stems-table-type"]')?.textContent).toContain('اسم');
    const indicator = root.querySelector('[data-testid="stems-table-additional-types"]');
    expect(indicator).toBeTruthy();
    expect(indicator?.getAttribute('aria-label')).toContain('نوع إضافي');
  });

  it('emits rowSelected when the stem button is clicked', () => {
    const fixture = setup([row(1), row(2)]);
    const emitted: StemListItemViewModel[] = [];
    fixture.componentInstance.rowSelected.subscribe((stem) => emitted.push(stem));

    const buttons = fixture.nativeElement.querySelectorAll(
      '[data-testid="stems-table-stem-button"]',
    ) as NodeListOf<HTMLElement>;
    buttons[1].click();
    fixture.detectChanges();

    expect(emitted).toHaveLength(1);
    expect(emitted[0].id).toBe(2);
  });

  it('marks the selected row with aria-selected', () => {
    const fixture = setup([row(1), row(2)], { selectedStemId: 2 });
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[aria-selected="true"]')).toBeTruthy();
  });

  it('keeps zero-count controls enabled and maps them to the expected detail event', () => {
    const fixture = setup([row(1, { occurrencesCount: 0, ayahsCount: 0, simpleWordsCount: 0 })]);
    const emitted: {
      stem: StemListItemViewModel;
      view: string;
      wordView?: string;
      surahView?: string;
    }[] = [];
    fixture.componentInstance.countOpened.subscribe((event) => emitted.push(event));

    const chips = fixture.nativeElement.querySelectorAll('qd-word-count-chip') as NodeListOf<HTMLElement>;
    const expected = [
      { view: 'ayahs' },
      { view: 'ayahs' },
      { view: 'surahs', surahView: 'mentioned' },
      { view: 'words', wordView: 'simple' },
      { view: 'words', wordView: 'tashkeel' },
    ];

    chips.forEach((chip) => {
      const btn = chip.querySelector('button');
      expect(btn?.hasAttribute('disabled')).toBe(false);
      btn?.click();
      fixture.detectChanges();
    });

    expect(emitted.map((e) => e.view)).toEqual(expected.map((e) => e.view));
    expect(emitted.map((e) => e.wordView ?? null)).toEqual(expected.map((e) => e.wordView ?? null));
    expect(emitted.map((e) => e.surahView ?? null)).toEqual(expected.map((e) => e.surahView ?? null));
  });
});
