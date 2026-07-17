import { signal } from '@angular/core';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { UniqueWordsPageComponent } from './unique-words-page.component';
import { UniqueWordsFacade } from '../../state/unique-words.facade';
import { WordsAssociationOptionsService } from '../../data-access/words-association-options.service';
import { UniqueWordKind, UniqueWordListItemViewModel, UniqueWordsAssociation, UniqueWordsListState, WordDrilldownState } from '../../models/unique-words.models';
import { UNIQUE_WORDS_RESULT_COUNT_LABEL } from '../../models/unique-words.labels';

const STUB_ASSOCIATION_OPTIONS = {
  searchRoots: () => of([]),
  searchLemmas: () => of([]),
  wordTypeOptions: () => of([]),
};

const CLOSED_DRILLDOWN: WordDrilldownState = { isOpen: false, selectedWordId: null, view: 'surahs', summary: null, surahs: null, missingSurahs: null, ayahs: null, ayahPage: 1, status: 'idle', errorMessage: '' };

function item(id: number): UniqueWordListItemViewModel {
  return { id, kind: 'tashkeel', displayText: `كلمة-تجريبية-${id}`, occurrencesCount: id, ayahsCount: id, surahsCount: 1, missingSurahsCount: 113, primaryWordTypeCode: null, primaryWordTypeBroadArabicLabel: null, rootId: null, rootText: null };
}

function stateFor(overrides: Partial<UniqueWordsListState>): UniqueWordsListState {
  return { status: 'success', items: [item(1), item(2)], page: 1, pageSize: 50, totalCount: 2, mode: 'tashkeel', search: '', sort: 'mushaf-order', errorMessage: '', ...overrides };
}

class StubFacade {
  readonly listState = signal<UniqueWordsListState>(stateFor({}));
  readonly drilldownState = signal<WordDrilldownState>(CLOSED_DRILLDOWN);
  readonly mode = signal<UniqueWordKind>('tashkeel');
  readonly search = signal<string>('');
  readonly association = signal<UniqueWordsAssociation>({ primaryType: null, rootId: null });
  readonly bindToRoute = vi.fn();
  readonly unbindFromRoute = vi.fn();
  readonly openDrilldown = vi.fn();
  readonly closeDrilldown = vi.fn();
  readonly setDrilldownView = vi.fn();
  readonly setAyahPage = vi.fn();
}

describe('UniqueWordsPageComponent', () => {
  let stub: StubFacade;
  let router: Router;

  beforeEach(async () => {
    stub = new StubFacade();
    getTestBed().resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [UniqueWordsPageComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({})), queryParamMap: of(convertToParamMap({})) } },
        { provide: UniqueWordsFacade, useValue: stub },
        { provide: WordsAssociationOptionsService, useValue: STUB_ASSOCIATION_OPTIONS },
      ],
      teardown: { destroyAfterEach: true },
    }).compileComponents();
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  async function render(stateOverride?: Partial<UniqueWordsListState>): Promise<HTMLElement> {
    if (stateOverride) stub.listState.set(stateFor(stateOverride));
    const fixture = TestBed.createComponent(UniqueWordsPageComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders the page title and active mode label', async () => {
    const root = await render();
    expect(root.querySelector('[data-testid="unique-words-page-title"]')?.textContent).toContain('الكلمات الفريدة');
  });

  it('mounts the explainer hero inside the intro band, after the mode line and above the toolbar (Feature 031)', async () => {
    const root = await render();

    const band = root.querySelector('.uw-intro-band') as HTMLElement;
    const header = band.querySelector('.qd-page-header') as HTMLElement;
    const hero = band.querySelector('[data-testid="words-explainer--unique"]');
    expect(hero).toBeTruthy();
    // The mode line stays adjacent to the title it qualifies; the hero follows the whole header.
    expect(band.querySelector('[data-testid="unique-words-page-mode"]')).toBeTruthy();
    expect(header.compareDocumentPosition(hero!) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    // The toolbar recess stays below the band, unchanged.
    expect(root.querySelector('.uw-toolbar-recess')).toBeTruthy();
    expect(band.querySelector('.uw-toolbar-recess')).toBeNull();
  });

  it('renders the mode tabs', async () => {
    const root = await render();
    expect(root.querySelector('[data-testid="unique-words-tab--tashkeel"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="unique-words-tab--simple"]')).toBeTruthy();
  });

  it('shows the headline result count equal to the list totalCount (US4)', async () => {
    const root = await render({ totalCount: 2 });
    const stat = root.querySelector('[data-testid="unique-words-result-count"] [data-testid="explorer-result-count"]');
    expect(stat).toBeTruthy();
    expect(stat?.getAttribute('aria-label')).toBe(`${UNIQUE_WORDS_RESULT_COUNT_LABEL}: 2`);
    expect(
      root
        .querySelector('[data-testid="unique-words-result-count"] [data-testid="explorer-result-count-value"]')
        ?.textContent?.trim(),
    ).toBe('2');
  });

  it('shows 0 in the headline result count for an empty scope (US4)', async () => {
    const root = await render({ status: 'empty', items: [], totalCount: 0 });
    expect(
      root
        .querySelector('[data-testid="unique-words-result-count"] [data-testid="explorer-result-count-value"]')
        ?.textContent?.trim(),
    ).toBe('0');
  });

  it('hides the headline result count when the list read fails (US4)', async () => {
    const root = await render({ status: 'error', items: [], errorMessage: 'خطأ' });
    expect(
      root.querySelector('[data-testid="unique-words-result-count"] [data-testid="explorer-result-count"]'),
    ).toBeNull();
  });

  it('renders the search input and sort select', async () => {
    const root = await render();
    expect(root.querySelector('[data-testid="unique-words-search-input"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="unique-words-sort-select"]')).toBeTruthy();
  });

  it('renders a virtualized table row per loaded item with the display text', async () => {
    const root = await render();
    const rows = root.querySelectorAll('[data-testid="unique-words-table-word-button"]');
    expect(rows.length).toBeGreaterThanOrEqual(2);
    expect(rows[0]?.textContent).toContain('كلمة-تجريبية-1');
  });

  it('shows the Arabic empty state when status is empty', async () => {
    const root = await render({ status: 'empty', items: [], totalCount: 0 });
    // Feature 030, N3 row 5: the list's own states render inside the table shell, not above the grid.
    expect(
      root.querySelector('[data-testid="unique-words-empty"]')?.closest('.qd-explorer-table'),
    ).toBeTruthy();
    expect(root.querySelector('[data-testid="unique-words-loading"]')).toBeNull();
  });

  it('shows the error message when status is error', async () => {
    const root = await render({ status: 'error', items: [], errorMessage: 'خطأ تجريبي' });
    // Feature 030, N3 row 5: the list's own states render inside the table shell, not above the grid.
    const error = root.querySelector('[data-testid="unique-words-error"]');
    expect(error?.textContent).toContain('خطأ تجريبي');
    expect(error?.closest('.qd-explorer-table')).toBeTruthy();
  });

  it('shows the loading state when status is loading', async () => {
    const root = await render({ status: 'loading', items: [] });
    const loading = root.querySelector('[data-testid="unique-words-loading"]');
    expect(loading).toBeTruthy();
    expect(loading?.querySelectorAll('.unique-words-table__row')).toHaveLength(12);
    expect(root.querySelector('[data-testid="unique-words-table-word-button"]')).toBeNull();
  });

  it('marks the selected row when a drill-down is open', async () => {
    stub.drilldownState.set({ ...CLOSED_DRILLDOWN, isOpen: true, selectedWordId: 1, status: 'success', summary: { id: 1, kind: 'tashkeel', displayText: 'كلمة-تجريبية-1', occurrencesCount: 1, ayahsCount: 1, surahsCount: 1, missingSurahsCount: 113 }, surahs: null });
    const root = await render();
    expect(root.querySelector('[aria-selected="true"]')).toBeTruthy();
  });

  it('renders the desktop selection panel placeholder when no word is selected', async () => {
    const root = await render();
    expect(root.textContent).toContain('اختر كلمة لعرض تفاصيلها');
  });

  it('seeds the search input from the active facade search term', async () => {
    stub.search.set('اسم');
    const root = await render();
    expect(root.querySelector<HTMLInputElement>('[data-testid="unique-words-search-input"]')?.value).toBe('اسم');
  });

  it('renders list pagination when totalCount exceeds pageSize', async () => {
    const root = await render({ totalCount: 120, page: 1 });
    expect(root.querySelector('[data-testid="qd-pagination-prev"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="qd-pagination-jump-input"]')).toBeTruthy();
  });

  it('hides list pagination when totalCount fits in one page', async () => {
    const root = await render({ totalCount: 2, page: 1 });
    expect(root.querySelector('[data-testid="qd-pagination-prev"]')).toBeNull();
  });

  it('serializes a count-range chip to the URL and resets the page (US5)', async () => {
    const root = await render();

    expect(root.querySelector('[data-testid="unique-words-range-filter"]')).toBeTruthy();
    (root.querySelector('[data-testid="range-filter-chip-occurrences-gt"]') as HTMLButtonElement).click();

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({ occ: '101..', page: null }),
      queryParamsHandling: 'merge',
    });
  });

  it('serializes a primary-type selection to the URL and resets the page (US7)', async () => {
    const fixture = TestBed.createComponent(UniqueWordsPageComponent);
    fixture.componentInstance.ngOnInit();
    await fixture.whenStable();

    fixture.componentInstance['onPrimaryTypeChange']({ id: 'PN', label: 'اسم علم' });

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({ primaryType: 'PN', page: null }),
      queryParamsHandling: 'merge',
    });
  });

  it('serializes a primary-root selection to the URL and resets the page (US7)', async () => {
    const fixture = TestBed.createComponent(UniqueWordsPageComponent);
    fixture.componentInstance.ngOnInit();
    await fixture.whenStable();

    fixture.componentInstance['onPrimaryRootChange']({ id: 5001, label: 'ك ل م' });

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({ rootId: '5001', page: null }),
      queryParamsHandling: 'merge',
    });
  });

  it('debounces keyboard drilldown navigation and keeps only the final target', async () => {
    vi.useFakeTimers();
    const fixture = TestBed.createComponent(UniqueWordsPageComponent);
    fixture.componentInstance.ngOnInit();
    await fixture.whenStable();

    fixture.componentInstance['onDrilldownOpen']({ word: item(1), column: 'surahs', view: 'surahs', source: 'keyboard' });
    fixture.componentInstance['onDrilldownOpen']({ word: item(1), column: 'missing', view: 'missing', source: 'keyboard' });

    expect(router.navigate).not.toHaveBeenCalled();
    vi.advanceTimersByTime(499);
    expect(router.navigate).not.toHaveBeenCalled();
    vi.advanceTimersByTime(1);

    expect(stub.openDrilldown).toHaveBeenCalledTimes(1);
    expect(stub.openDrilldown).toHaveBeenCalledWith(expect.objectContaining({ id: 1 }), 'missing');
    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({ word: '1', view: 'missing', ap: null }),
      queryParamsHandling: 'merge',
    });
  });

  describe('sorting (Feature 030, N8)', () => {
    it('has no desktop sort dropdown: the only select sits in the ≤1023px fallback wrapper', async () => {
      const root = await render();

      // ≥1024px the table header row is visible and owns sorting, so the fallback is CSS-hidden
      // there; below that the header row is display:none and this select is the only way in.
      const select = root.querySelector('[data-testid="unique-words-sort-select"]') as HTMLElement;
      expect(select).toBeTruthy();
      expect(select.closest('.qd-explorer-sort-fallback')).not.toBeNull();
    });

    it('offers the default order plus every sortable column in both directions', async () => {
      const root = await render();
      const values = Array.from(
        root.querySelectorAll('[data-testid="unique-words-sort-select"] option'),
      ).map((option) => option.getAttribute('value'));

      expect(values).toEqual([
        'mushaf-order',
        'alpha',
        'alpha-desc',
        'occurrences',
        'occurrences-asc',
        'ayahs',
        'ayahs-asc',
        'surahs',
        'surahs-asc',
      ]);
    });

    it('navigates { sort: token, page: null } when a header cycle step is emitted', async () => {
      const root = await render();

      (root.querySelector('[data-testid="unique-words-table-sort-occurrences"]') as HTMLButtonElement).click();

      expect(router.navigate).toHaveBeenCalledWith([], {
        relativeTo: expect.anything(),
        queryParams: expect.objectContaining({ sort: 'occurrences', page: null }),
        queryParamsHandling: 'merge',
      });
    });

    it('navigates { sort: null, page: null } when the cycle releases', async () => {
      const root = await render({ sort: 'occurrences-asc' });

      (root.querySelector('[data-testid="unique-words-table-sort-occurrences"]') as HTMLButtonElement).click();

      // Release removes the param entirely — the default order is never spelled out in the URL.
      expect(router.navigate).toHaveBeenCalledWith([], {
        relativeTo: expect.anything(),
        queryParams: expect.objectContaining({ sort: null, page: null }),
        queryParamsHandling: 'merge',
      });
    });

    it('drives the same URL contract from the fallback select', async () => {
      const root = await render();
      const select = root.querySelector('[data-testid="unique-words-sort-select"]') as HTMLSelectElement;

      select.value = 'alpha-desc';
      select.dispatchEvent(new Event('change'));

      expect(router.navigate).toHaveBeenCalledWith([], {
        relativeTo: expect.anything(),
        queryParams: expect.objectContaining({ sort: 'alpha-desc', page: null }),
        queryParamsHandling: 'merge',
      });
    });

    it('releases the param when the fallback select picks the default order', async () => {
      const root = await render({ sort: 'alpha' });
      const select = root.querySelector('[data-testid="unique-words-sort-select"]') as HTMLSelectElement;

      select.value = 'mushaf-order';
      select.dispatchEvent(new Event('change'));

      expect(router.navigate).toHaveBeenCalledWith([], {
        relativeTo: expect.anything(),
        queryParams: expect.objectContaining({ sort: null, page: null }),
        queryParamsHandling: 'merge',
      });
    });
  });
});
