import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { RootsTableComponent } from './roots-table.component';
import { RootListItemViewModel } from '../../models/roots.models';

function row(id: number, overrides: Partial<RootListItemViewModel> = {}): RootListItemViewModel {
  return {
    id,
    rootText: `جذر-${id}`,
    displayText: `جذر-${id}`,
    occurrencesCount: id * 10,
    ayahsCount: id * 2,
    surahsCount: id,
    simpleWordsCount: id + 1,
    tashkeelWordsCount: id + 2,
    lemmasCount: id + 3,
    stemsCount: id + 4,
    ...overrides,
  };
}

describe('RootsTableComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [RootsTableComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  function setup(
    rows: readonly RootListItemViewModel[],
    overrides: Record<string, unknown> = {},
  ) {
    const fixture = TestBed.createComponent(RootsTableComponent);
    fixture.componentRef.setInput('rows', rows);
    for (const [key, value] of Object.entries(overrides)) {
      (fixture.componentRef as unknown as { setInput: (k: string, v: unknown) => void }).setInput(key, value);
    }
    fixture.detectChanges();
    return fixture;
  }

  it('renders the column headers', () => {
    const fixture = setup([], { loading: false });
    const root = fixture.nativeElement as HTMLElement;

    const headers = Array.from(root.querySelectorAll('[role="columnheader"]')).map((h) =>
      h.textContent?.trim() ?? '',
    );
    expect(headers).toContain('م');
    expect(headers).toContain('الجذر');
    expect(headers).toContain('المواضع');
    expect(headers).toContain('الآيات');
    expect(headers).toContain('السور');
    expect(headers).toContain('بدون تشكيل');
    expect(headers).toContain('بالتشكيل');
    expect(headers).toContain('الصيغ');
    expect(headers).toContain('الأصول');
  });

  it('renders the eight counts and UI row numbers, never backend ids', () => {
    const fixture = setup([row(1), row(2)], { currentPage: 1 });
    const root = fixture.nativeElement as HTMLElement;

    const rows = root.querySelectorAll('[role="row"]:not(.roots-table__header-row)');
    expect(rows).toHaveLength(2);

    const rowNumbers = Array.from(
      root.querySelectorAll('.roots-table__cell--row-number'),
    ).map((c) => c.textContent?.trim() ?? '');
    expect(rowNumbers).toEqual(['1', '2']);

    expect(root.textContent).not.toContain('id-');
    expect(root.querySelectorAll('qd-word-count-chip')).toHaveLength(14);
  });

  it('keeps count chip aria names semantic while grid headers stay compact', () => {
    const fixture = setup([row(1)]);
    const root = fixture.nativeElement as HTMLElement;

    const chipLabels = Array.from(root.querySelectorAll('qd-word-count-chip button')).map(
      (btn) => btn.getAttribute('aria-label') ?? '',
    );
    expect(chipLabels[3]).toContain('كلمات بدون تشكيل');
    expect(chipLabels[4]).toContain('كلمات بالتشكيل');
    expect(chipLabels[5]).toContain('الصيغ المعجمية');
    expect(chipLabels[6]).toContain('الأصول الصرفية');

    const headers = Array.from(root.querySelectorAll('[role="columnheader"]')).map(
      (h) => h.textContent?.trim() ?? '',
    );
    expect(headers).toContain('الصيغ');
    expect(headers).not.toContain('الصيغ المعجمية');
  });

  it('emits rowSelected with the row when the root cell is clicked', () => {
    const fixture = setup([row(1), row(2)]);
    const emitted: RootListItemViewModel[] = [];
    fixture.componentInstance.rowSelected.subscribe((r) => emitted.push(r));

    const buttons = fixture.nativeElement.querySelectorAll(
      '[data-testid="roots-table-root-button"]',
    ) as NodeListOf<HTMLElement>;
    buttons[1].click();
    fixture.detectChanges();

    expect(emitted).toHaveLength(1);
    expect(emitted[0].id).toBe(2);
  });

  it('marks the selected row with aria-selected', () => {
    const fixture = setup([row(1), row(2)], { selectedRootId: 2 });
    const root = fixture.nativeElement as HTMLElement;

    const selected = root.querySelector('[aria-selected="true"]');
    expect(selected).toBeTruthy();
  });

  it('emits countOpened with the matching view and sub-view per the count-click mapping', () => {
    const fixture = setup([row(1)]);
    const emitted: {
      root: RootListItemViewModel;
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
      { view: 'lemmas' },
      { view: 'stems' },
    ];

    chips.forEach((chip) => {
      const btn = chip.querySelector('button');
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
