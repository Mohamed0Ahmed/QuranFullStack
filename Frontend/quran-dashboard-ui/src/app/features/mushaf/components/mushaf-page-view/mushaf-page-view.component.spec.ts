import { beforeEach, describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MushafPageViewComponent } from './mushaf-page-view.component';
import { MushafPageViewModel } from '../../models/mushaf.models';

const pageFixture: MushafPageViewModel = {
  pageNumber: 5,
  previousPageNumber: 4,
  nextPageNumber: 6,
  surahs: [{ surahNumber: 2, nameArabic: 'البقرة', firstAyahOnPage: 25, lastAyahOnPage: 26 }],
  ayahRange: { firstVerseKey: '2:25', lastVerseKey: '2:26' },
  navigation: { juzNumbers: [1], hizbNumbers: [1], rubNumbers: [1, 2] },
  lines: [
    {
      lineNumber: 1,
      lineType: 'ayah',
      isCentered: false,
      surahNumber: null,
      words: [
        {
          wordLocation: '2:25:1',
          verseKey: '2:25',
          wordNumber: 1,
          lineWordOrder: 1,
          textUthmani: 'وَبَشِّرِ',
          isAyahMarker: false,
        },
        {
          wordLocation: '2:25:2',
          verseKey: '2:25',
          wordNumber: 2,
          lineWordOrder: 2,
          textUthmani: 'ٱلَّذِينَ',
          isAyahMarker: false,
        },
      ],
    },
    {
      lineNumber: 2,
      lineType: 'ayah',
      isCentered: false,
      surahNumber: null,
      words: [
        {
          wordLocation: '2:26:1',
          verseKey: '2:26',
          wordNumber: 1,
          lineWordOrder: 1,
          textUthmani: 'ٱللَّهُ',
          isAyahMarker: false,
        },
      ],
    },
  ],
  markers: [
    {
      markerType: 'rub',
      markerNumber: 2,
      verseKey: '2:26',
      lineNumber: 2,
      wordLocation: '2:26:1',
      sajdahType: null,
    },
  ],
};

describe('MushafPageViewComponent', () => {
  let fixture: ComponentFixture<MushafPageViewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MushafPageViewComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(MushafPageViewComponent);
    fixture.componentRef.setInput('page', pageFixture);
    fixture.detectChanges();
  });

  it('renders lines and words from the view model using textUthmani', () => {
    const root = fixture.nativeElement as HTMLElement;
    const text = root.textContent ?? '';

    expect(text).toContain('وَبَشِّرِ');
    expect(text).toContain('ٱلَّذِينَ');
    expect(text).toContain('ٱللَّهُ');
    expect(root.querySelectorAll('qd-mushaf-line').length).toBe(2);
  });

  it('does not render segment forms in the Mushaf area', () => {
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-segment-form]')).toBeNull();
  });

  it('does not render temporarily hidden rub/hizb/sajda page markers', () => {
    const lines = fixture.nativeElement.querySelectorAll('qd-mushaf-line');
    const firstLineText = lines[0]?.textContent ?? '';
    const secondLineText = lines[1]?.textContent ?? '';

    expect(firstLineText).not.toContain('ربع 2');
    expect(secondLineText).not.toContain('ربع 2');
  });

  it('uses a fixed-width text column for Mushaf lines', () => {
    const column = fixture.nativeElement.querySelector(
      '.mushaf-page-view__text-column',
    ) as HTMLElement;

    expect(column).not.toBeNull();
    expect(getComputedStyle(column).width).toContain('var(--qd-mushaf-text-column-width)');
  });

  it('keeps a scroll fallback and stable gutter on the page view container', () => {
    const root = fixture.nativeElement.querySelector('.mushaf-page-view') as HTMLElement;

    expect(getComputedStyle(root).overflowY).toBe('auto');
    expect(root.classList.contains('qd-scroll-stable')).toBe(true);
  });

  it('renders the surah icon, all surah glyphs, and juz chrome above the lines', () => {
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="mushaf-page-surah-icon-glyph"]')).toBeTruthy();
    expect(root.querySelectorAll('[data-testid="mushaf-page-surah-glyph"]').length).toBe(1);
    expect(root.querySelector('[data-testid="mushaf-page-juz-glyph"]')).toBeTruthy();
    expect(root.textContent).toContain('جزء 1');
    expect(root.textContent).toContain('سورة البقرة');
  });

  it('renders every surah on the page with Arabic comma separators', () => {
    fixture.componentRef.setInput('page', {
      ...pageFixture,
      surahs: [
        { surahNumber: 105, nameArabic: 'سورة-تجريبية-١', firstAyahOnPage: 1, lastAyahOnPage: 5 },
        { surahNumber: 106, nameArabic: 'سورة-تجريبية-٢', firstAyahOnPage: 1, lastAyahOnPage: 4 },
      ],
    });
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelectorAll('[data-testid="mushaf-page-surah-glyph"]').length).toBe(2);
    expect(root.textContent).toContain('،');
    expect(root.textContent).toContain('سورة سورة-تجريبية-١، سورة-تجريبية-٢');
  });

  function wordButtons(): HTMLButtonElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('button.mushaf-word'));
  }

  it('renders a centered page jump trigger below the lines', () => {
    const trigger = fixture.nativeElement.querySelector(
      '[data-testid="mushaf-page-jump-trigger"]',
    ) as HTMLButtonElement;

    expect(trigger).toBeTruthy();
    expect(trigger.textContent?.trim()).toBe('5');
  });

  it('expands the page jump trigger to the minimum hit target without resizing its box (D47)', () => {
    const trigger = fixture.nativeElement.querySelector(
      '[data-testid="mushaf-page-jump-trigger"]',
    ) as HTMLButtonElement;

    const style = getComputedStyle(trigger);

    expect(style.position).toBe('relative');
    expect(style.minWidth).toBe('2.25rem');
  });

  it('emits pageChange when the user enters a new page number', () => {
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((pageNumber) => emitted.push(pageNumber));

    const trigger = fixture.nativeElement.querySelector(
      '[data-testid="mushaf-page-jump-trigger"]',
    ) as HTMLButtonElement;
    trigger.click();
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector(
      '[data-testid="mushaf-page-jump-input"]',
    ) as HTMLInputElement;
    input.value = '12';
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
    fixture.detectChanges();

    expect(emitted).toEqual([12]);
    expect(
      fixture.nativeElement.querySelector('[data-testid="mushaf-page-jump-input"]'),
    ).toBeNull();
  });
});
