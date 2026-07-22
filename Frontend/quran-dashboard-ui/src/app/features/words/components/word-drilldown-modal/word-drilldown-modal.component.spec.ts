import { describe, expect, it, beforeEach, afterEach, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { provideRouter } from '@angular/router';

import { WordDrilldownModalComponent } from './word-drilldown-modal.component';
import {
  PagedResultDto,
  UniqueWordAyahMatchDto,
  WordDrilldownState,
} from '../../models/unique-words.models';

function state(overrides: Partial<WordDrilldownState>): WordDrilldownState {
  return {
    isOpen: true,
    selectedWordId: 1,
    view: 'surahs',
    summary: {
      id: 1,
      kind: 'tashkeel',
      displayText: 'كلمة-تجريبية',
      occurrencesCount: 1,
      ayahsCount: 1,
      surahsCount: 1,
      missingSurahsCount: 113,
    },
    surahs: {
      surahs: [{ surahNumber: 1, nameArabic: 'سورة-تجريبية', occurrencesInSurah: 1 }],
    },
    missingSurahs: null,
    ayahs: null,
    ayahPage: 1,
    status: 'success',
    errorMessage: '',
    ...overrides,
  };
}

function ayahPage(overrides: Partial<PagedResultDto<UniqueWordAyahMatchDto>> = {}): PagedResultDto<UniqueWordAyahMatchDto> {
  return {
    page: 1,
    pageSize: 1,
    totalCount: 2,
    items: [
      {
        ayahId: 11,
        verseKey: '1:1',
        surahNameArabic: 'سورة-تجريبية',
        ayahNumber: 1,
        pageNumber: 1,
        matchedQuranWordIds: [1001],
        words: [{ quranWordId: 1001, textUthmani: 'كلمة-تجريبية', isAyahMarker: false }],
      },
    ],
    ...overrides,
  };
}

async function createFixture(initialState: WordDrilldownState) {
  getTestBed().resetTestingModule();
  await TestBed.configureTestingModule({
    imports: [WordDrilldownModalComponent],
    providers: [provideRouter([]), provideLocationMocks()],
    teardown: { destroyAfterEach: true },
  }).compileComponents();

  const fixture = TestBed.createComponent(WordDrilldownModalComponent);
  fixture.componentRef.setInput('state', initialState);
  fixture.detectChanges();
  await fixture.whenStable();
  fixture.detectChanges();
  return fixture;
}

async function createInlineFixture(initialState: WordDrilldownState) {
  const fixture = await createFixture(initialState);
  fixture.componentRef.setInput('inline', true);
  fixture.detectChanges();
  await fixture.whenStable();
  fixture.detectChanges();
  return fixture;
}

describe('WordDrilldownModalComponent', () => {
  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  it('renders modal shell when open', async () => {
    const fixture = await createFixture(state({ status: 'loading', surahs: null }));

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="word-drilldown-modal"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="word-drilldown-panel-label"]')?.textContent).toContain('تفاصيل الكلمة');
    expect(root.querySelector('[data-testid="word-drilldown-entity"]')?.textContent).toContain('كلمة-تجريبية');
    const loading = root.querySelector('[data-testid="explorer-panel-skeleton"]');

    expect(loading).toBeTruthy();
    expect(loading?.getAttribute('aria-busy')).toBe('true');
    expect(loading?.classList.contains('qd-skeleton-group')).toBe(true);
    expect(loading?.querySelectorAll('.qd-skeleton--text')).toHaveLength(6);
  });

  it('renders surah list in success state', async () => {
    const fixture = await createFixture(state({}));

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="surah-occurrences-list"]')).toBeTruthy();
  });

  it('uses the simple display text in the title when the selected word is simple mode', async () => {
    const fixture = await createFixture(
      state({
        summary: {
          id: 1,
          kind: 'simple',
          displayText: 'كلمة-بسيطة',
          occurrencesCount: 1,
          ayahsCount: 1,
          surahsCount: 1,
          missingSurahsCount: 113,
        },
      }),
    );

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="word-drilldown-panel-label"]')?.textContent).toContain('تفاصيل الكلمة');
    expect(root.querySelector('[data-testid="word-drilldown-entity"]')?.textContent).toContain('كلمة-بسيطة');
  });

  it('renders missing-surahs list in success state', async () => {
    const fixture = await createFixture(
      state({
        view: 'missing',
        status: 'success',
        surahs: null,
        missingSurahs: {
          surahs: [{ surahNumber: 2, nameArabic: 'سورة-تجريبية-٢' }],
        },
      }),
    );

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="missing-surahs-list"]')).toBeTruthy();
  });

  it('renders missing-surahs empty state', async () => {
    const fixture = await createFixture(
      state({
        view: 'missing',
        status: 'empty',
        surahs: null,
        missingSurahs: null,
      }),
    );

    const root = fixture.nativeElement as HTMLElement;
    expect(fixture.componentInstance.state().status).toBe('empty');
    expect(fixture.componentInstance.state().view).toBe('missing');
    expect(root.querySelector('[data-testid="word-drilldown-empty"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="missing-surahs-list"]')).toBeNull();
  });

  it('renders ayah list and emits pagination', async () => {
    const fixture = await createFixture(
      state({
        view: 'ayahs',
        status: 'success',
        surahs: null,
        ayahs: ayahPage(),
        ayahPage: 1,
      }),
    );
    const pageChange = vi.fn();
    fixture.componentInstance.ayahPageChange.subscribe(pageChange);

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="ayah-matches-list"]')).toBeTruthy();

    const next = root.querySelector('[data-testid="qd-pagination-next"]') as HTMLButtonElement;
    next.click();
    fixture.detectChanges();

    expect(pageChange).toHaveBeenCalledWith(2);
  });

  it('emits close on button and backdrop click', async () => {
    const fixture = await createFixture(state({}));
    const closeModal = vi.fn();
    fixture.componentInstance.closeModal.subscribe(closeModal);

    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('[data-testid="word-drilldown-close"]') as HTMLButtonElement).click();
    expect(closeModal).toHaveBeenCalledTimes(1);

    (root.querySelector('[data-testid="word-drilldown-backdrop"]') as HTMLElement).click();
    expect(closeModal).toHaveBeenCalledTimes(2);
  });

  it('emits view change when segmented button is clicked', async () => {
    const fixture = await createFixture(state({}));
    const viewChange = vi.fn();
    fixture.componentInstance.viewChange.subscribe(viewChange);

    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('[data-testid="word-drilldown-view-missing"]') as HTMLButtonElement).click();

    expect(viewChange).toHaveBeenCalledWith('missing');
  });

  it('renders surah empty state', async () => {
    const fixture = await createFixture(
      state({
        view: 'surahs',
        status: 'empty',
        surahs: null,
      }),
    );

    const root = fixture.nativeElement as HTMLElement;
    expect(fixture.componentInstance.state().status).toBe('empty');
    expect(fixture.componentInstance.state().view).toBe('surahs');
    expect(root.querySelector('[data-testid="word-drilldown-empty"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="surah-occurrences-list"]')).toBeNull();
  });

  it('does not render when closed', async () => {
    const fixture = await createFixture(state({ isOpen: false }));

    expect((fixture.nativeElement as HTMLElement).querySelector('[data-testid="word-drilldown-modal"]')).toBeNull();
  });

  it('renders the inline panel prompt when no word is selected', async () => {
    const fixture = await createInlineFixture(state({ isOpen: false }));

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="word-drilldown-panel-label"]')?.textContent).toContain('تفاصيل الكلمة');
    expect(root.querySelector('[data-testid="word-drilldown-empty"]')?.textContent).toContain('اختر كلمة لعرض تفاصيلها');
    expect(root.querySelector('[data-testid="word-drilldown-modal"]')).toBeNull();
  });
});
