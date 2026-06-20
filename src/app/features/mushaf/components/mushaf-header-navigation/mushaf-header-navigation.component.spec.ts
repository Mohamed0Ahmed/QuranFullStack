import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { MushafPageViewModel, MushafSurahJuzGroupDto } from '../../models/mushaf.models';
import { MushafHeaderNavigationComponent } from './mushaf-header-navigation.component';

const surahCatalogByJuzFixture: readonly MushafSurahJuzGroupDto[] = [
  {
    juzNumber: 30,
    surahs: [
      { surahNumber: 101, nameArabic: 'سورة-تجريبية-١', startPageNumber: 600 },
      { surahNumber: 102, nameArabic: 'سورة-تجريبية-٢', startPageNumber: 601 },
    ],
  },
];
/** Source-safe synthetic placeholders — not Quranic text. */
const pageFixture: MushafPageViewModel = {
  pageNumber: 6,
  previousPageNumber: 5,
  nextPageNumber: 7,
  surahs: [
    { surahNumber: 101, nameArabic: 'سورة-تجريبية-١', firstAyahOnPage: 1, lastAyahOnPage: 5 },
    { surahNumber: 102, nameArabic: 'سورة-تجريبية-٢', firstAyahOnPage: 1, lastAyahOnPage: 4 },
    { surahNumber: 103, nameArabic: 'سورة-تجريبية-٣', firstAyahOnPage: 1, lastAyahOnPage: 3 },
  ],
  ayahRange: { firstVerseKey: '101:1', lastVerseKey: '103:3' },
  navigation: { juzNumbers: [30], hizbNumbers: [60], rubNumbers: [240] },
  lines: [],
  markers: [],
};

describe('MushafHeaderNavigationComponent', () => {
  it('does not render the removed page context zone', () => {
    const fixture = TestBed.createComponent(MushafHeaderNavigationComponent);
    fixture.componentRef.setInput('page', pageFixture);
    fixture.componentRef.setInput('surahCatalogByJuz', []);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.mushaf-header__deck-zone--context')).toBeNull();
  });

  it('renders previous and next before the surah jump picker', () => {
    const fixture = TestBed.createComponent(MushafHeaderNavigationComponent);
    fixture.componentRef.setInput('page', pageFixture);
    fixture.componentRef.setInput('surahCatalogByJuz', surahCatalogByJuzFixture);
    fixture.detectChanges();

    const navZone = fixture.nativeElement.querySelector(
      '.mushaf-header__deck-zone--nav',
    ) as HTMLElement;
    const navActions = navZone.querySelector('.mushaf-header__nav-actions') as HTMLElement;
    const surahJump = navZone.querySelector('.mushaf-header__surah-jump') as HTMLElement;
    const buttons = [...navActions.querySelectorAll('button')].map((btn) => btn.textContent?.trim());

    expect(buttons).toEqual(['السابق', 'التالي']);
    expect(navZone.compareDocumentPosition(surahJump) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(fixture.nativeElement.querySelector('qd-surah-jump-picker')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('select')).toBeNull();
  });
});
