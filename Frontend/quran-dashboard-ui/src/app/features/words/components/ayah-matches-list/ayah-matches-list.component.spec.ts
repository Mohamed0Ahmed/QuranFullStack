import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { RootDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
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

const PARENT_FRAME: RootDetailFrame = {
  kind: 'root',
  id: 999,
  view: 'ayahs',
  wordView: 'simple',
  surahView: 'mentioned',
  detailPage: 1,
};

const ROOT_FRAME_SERIALIZED = 'v1~root~999~ayahs~simple~mentioned~1';

function setInputs(
  fixture: ComponentFixture<AyahMatchesListComponent>,
  inputs: { page: PagedResultDto<AyahMatchDto>; currentPage: number },
): void {
  fixture.componentRef.setInput('page', inputs.page);
  fixture.componentRef.setInput('currentPage', inputs.currentPage);
  fixture.detectChanges();
}

describe('AyahMatchesListComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideLocationMocks()],
    });
    TestBed.inject(Router).initialNavigation();
  });

  it('renders the ayah link as a same-tab overlay-continuity anchor to the Mushaf', () => {
    const fixture = TestBed.createComponent(AyahMatchesListComponent);
    setInputs(fixture, { page: PAGE, currentPage: 1 });

    const root = fixture.nativeElement as HTMLElement;
    const actionLink = root.querySelector(
      '[data-testid="ayah-matches-open-mushaf"]',
    ) as HTMLAnchorElement | null;

    const href = actionLink?.getAttribute('href') ?? '';
    expect(href).toContain('/dashboard/mushaf');
    expect(href).toContain('page=92');
    expect(href).toContain('ayah=4:57');
    expect(href).toContain('focusAyah=4:57');
    expect(href).toContain('panel=ayah');
    expect(href).not.toContain('qdDetail');
    expect(actionLink?.getAttribute('aria-label')).toBe('فتح الآية في المصحف');
    expect(actionLink?.getAttribute('target')).toBeNull();
    expect(actionLink?.getAttribute('rel')).toBeNull();
    expect(actionLink?.querySelector('[data-testid="highlighted-ayah"]')).not.toBeNull();
  });

  it('promotes the provided parent frame into the href and the intercepted click', () => {
    const fixture = TestBed.createComponent(AyahMatchesListComponent);
    fixture.componentRef.setInput('parentFrame', PARENT_FRAME);
    setInputs(fixture, { page: PAGE, currentPage: 1 });

    const link = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-testid="ayah-matches-open-mushaf"]',
    ) as HTMLAnchorElement;
    const href = link.getAttribute('href')!;
    expect(href).toContain(`qdDetail=${encodeURIComponent(ROOT_FRAME_SERIALIZED)}`);
    expect(href).toContain('qdDetailOpen=1');

    const navigateSpy = vi
      .spyOn(TestBed.inject(DetailOverlayHistoryService), 'navigateBaseWithOverlay')
      .mockReturnValue(undefined);
    const plainClick = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 });
    link.dispatchEvent(plainClick);

    expect(plainClick.defaultPrevented).toBe(true);
    expect(navigateSpy).toHaveBeenCalledWith(
      '/dashboard/mushaf',
      { page: '92', ayah: '4:57', focusAyah: '4:57', panel: 'ayah' },
      { promoteFrame: PARENT_FRAME },
    );

    const modifiedClick = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0, ctrlKey: true });
    link.dispatchEvent(modifiedClick);
    expect(modifiedClick.defaultPrevented).toBe(false);
    expect(navigateSpy).toHaveBeenCalledTimes(1);
  });

  it('renders every Word Type-shaped row when all rows share ayahId 0 (stable verseKey tracking)', () => {
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

  it('keeps the pagination slot rendered while loading and once loaded', () => {
    const fixture = TestBed.createComponent(AyahMatchesListComponent);
    fixture.componentRef.setInput('page', PAGE);
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;

    const slot = () => host.querySelector('[data-testid="ayah-matches-pagination-slot"]');
    expect(slot()).toBeTruthy();
    expect(slot()!.querySelector('qd-pagination')).toBeNull();

    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();
    expect(slot()).toBeTruthy();
    expect(slot()!.querySelector('qd-pagination')).toBeTruthy();
  });
});
