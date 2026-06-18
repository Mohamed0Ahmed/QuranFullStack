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

  it('does not vertically center regular pages beyond the opening spread', () => {
    const root = fixture.nativeElement.querySelector('.mushaf-page-view') as HTMLElement;

    expect(fixture.componentInstance.page().pageNumber).toBe(5);
    expect(root.classList.contains('mushaf-page-view--opening-pages')).toBe(false);
    expect(getComputedStyle(root).justifyContent).not.toBe('center');
  });
});

describe('MushafPageViewComponent opening pages', () => {
  it('vertically centers pages 1 and 2 within the Mushaf area', () => {
    const openingPageFixture: MushafPageViewModel = {
      ...pageFixture,
      pageNumber: 1,
      previousPageNumber: null,
      nextPageNumber: 2,
      surahs: [{ surahNumber: 1, nameArabic: 'الفاتحة', firstAyahOnPage: 1, lastAyahOnPage: 7 }],
      ayahRange: { firstVerseKey: '1:1', lastVerseKey: '1:7' },
      navigation: { juzNumbers: [1], hizbNumbers: [1], rubNumbers: [1] },
      lines: pageFixture.lines.slice(0, 1),
      markers: [],
    };

    const fixture = TestBed.createComponent(MushafPageViewComponent);
    fixture.componentRef.setInput('page', openingPageFixture);
    fixture.detectChanges();

    const root = fixture.nativeElement.querySelector('.mushaf-page-view') as HTMLElement;
    expect(root.classList.contains('mushaf-page-view--opening-pages')).toBe(true);
    expect(getComputedStyle(root).justifyContent).toBe('center');
  });
});
