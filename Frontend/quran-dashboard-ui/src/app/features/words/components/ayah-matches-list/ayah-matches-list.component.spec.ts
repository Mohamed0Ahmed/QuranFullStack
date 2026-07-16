import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { AyahMatchesListComponent } from './ayah-matches-list.component';
import { AyahMatchDto, PagedResultDto } from '../../models/unique-words.models';

const PAGE: PagedResultDto<AyahMatchDto> = {
  page: 1,
  pageSize: 10,
  totalCount: 1,
  items: [
    {
      ayahId: 7001,
      verseKey: '4:57',
      surahNameArabic: 'النساء',
      ayahNumber: 57,
      pageNumber: 92,
      matchedQuranWordIds: [9001],
      words: [
        {
          quranWordId: 9001,
          textUthmani: 'كلمة-تجريبية-١',
          isAyahMarker: false,
        },
      ],
    },
  ],
};

function setInputs(
  fixture: ComponentFixture<AyahMatchesListComponent>,
  inputs: { page: PagedResultDto<AyahMatchDto>; currentPage: number },
): void {
  fixture.componentRef.setInput('page', inputs.page);
  fixture.componentRef.setInput('currentPage', inputs.currentPage);
  fixture.detectChanges();
}

describe('AyahMatchesListComponent', () => {
  it('opens the matching ayah in Mushaf in a new tab when the ayah text is clicked', () => {
    const fixture = TestBed.createComponent(AyahMatchesListComponent);
    setInputs(fixture, { page: PAGE, currentPage: 1 });

    const root = fixture.nativeElement as HTMLElement;
    const actionLink = root.querySelector(
      '[data-testid="ayah-matches-open-mushaf"]',
    ) as HTMLAnchorElement | null;

    expect(actionLink?.getAttribute('href')).toBe(
      '/dashboard/mushaf?page=92&ayah=4:57&focusAyah=4:57&panel=ayah',
    );
    expect(actionLink?.getAttribute('aria-label')).toBe('فتح الآية في المصحف');
    expect(actionLink?.getAttribute('target')).toBe('_blank');
    expect(actionLink?.getAttribute('rel')).toBe('noopener noreferrer');
    expect(actionLink?.querySelector('[data-testid="highlighted-ayah"]')).not.toBeNull();
  });

  it('renders every Word Type-shaped row when all rows share ayahId 0 (stable verseKey tracking)', () => {
    // Word Type ayah mapping supplies ayahId: 0 for every row; tracking must key on verseKey.
    const wordTypePage: PagedResultDto<AyahMatchDto> = {
      page: 1,
      pageSize: 10,
      totalCount: 2,
      items: [
        { ...PAGE.items[0], ayahId: 0, verseKey: '2:10', ayahNumber: 10 },
        { ...PAGE.items[0], ayahId: 0, verseKey: '3:12', ayahNumber: 12 },
      ],
    };

    const fixture = TestBed.createComponent(AyahMatchesListComponent);
    setInputs(fixture, { page: wordTypePage, currentPage: 1 });

    const cards = (fixture.nativeElement as HTMLElement).querySelectorAll('[data-testid="ayah-match-card"]');
    expect(cards).toHaveLength(2);
    expect(cards[0].textContent).toContain('10');
    expect(cards[1].textContent).toContain('12');
  });

  it('frames loaded and loading cards with the shared qdAyahCard primitive (no qd-card, no alternating fill)', () => {
    const fixture = TestBed.createComponent(AyahMatchesListComponent);
    setInputs(fixture, { page: PAGE, currentPage: 1 });

    const loaded = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="ayah-match-card"]')!;
    expect(loaded.classList.contains('qd-ayah-card')).toBe(true);
    expect(loaded.classList.contains('qd-card')).toBe(false);
    expect(loaded.classList.contains('ayah-matches-list__card--alt')).toBe(false);

    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();

    const loadingCard = (fixture.nativeElement as HTMLElement).querySelector('.ayah-matches-list__card--loading')!;
    expect(loadingCard.classList.contains('qd-ayah-card')).toBe(true);
    expect(loadingCard.classList.contains('qd-card')).toBe(false);
  });

  it('retains the analysis action inside the shared frame and emits its location', () => {
    const pageWithAnalysis: PagedResultDto<AyahMatchDto> = {
      ...PAGE,
      items: [{ ...PAGE.items[0], analysisLocation: '4:57:3' }],
    };

    const fixture = TestBed.createComponent(AyahMatchesListComponent);
    fixture.componentRef.setInput('showAnalysisAction', true);
    fixture.componentRef.setInput('analysisActionLabel', 'SYNTH_ANALYSIS_LABEL');
    setInputs(fixture, { page: pageWithAnalysis, currentPage: 1 });

    const emitted: string[] = [];
    fixture.componentInstance.analysisRequested.subscribe((location) => emitted.push(location));

    const action = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-testid="ayah-match-analysis"]',
    ) as HTMLButtonElement;
    expect(action.closest('[data-testid="ayah-match-card"]')).not.toBeNull();
    action.click();

    expect(emitted).toEqual(['4:57:3']);
  });
});
