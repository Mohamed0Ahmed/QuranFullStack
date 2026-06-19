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
  it('joins multiple surah names with Arabic comma separators', () => {
    const fixture = TestBed.createComponent(MushafHeaderNavigationComponent);
    fixture.componentRef.setInput('page', pageFixture);
    fixture.componentRef.setInput('surahCatalogByJuz', []);
    fixture.detectChanges();

    const surahs = fixture.nativeElement.querySelector('.mushaf-header__surahs') as HTMLElement;
    expect(surahs.textContent?.trim()).toBe(
      'سورة-تجريبية-١، سورة-تجريبية-٢، سورة-تجريبية-٣',
    );
  });

  it('shows juz only and omits hizb and rub from page context', () => {
    const fixture = TestBed.createComponent(MushafHeaderNavigationComponent);
    fixture.componentRef.setInput('page', pageFixture);
    fixture.componentRef.setInput('surahCatalogByJuz', []);
    fixture.detectChanges();

    const context = fixture.nativeElement.querySelector('.mushaf-header__deck-zone--context') as HTMLElement;
    const contextText = context.textContent ?? '';

    expect(contextText).toContain('جزء 30');
    expect(contextText).not.toContain('حزب');
    expect(contextText).not.toContain('ربع');
  });

  it('renders the searchable surah jump picker', () => {
    const fixture = TestBed.createComponent(MushafHeaderNavigationComponent);
    fixture.componentRef.setInput('page', pageFixture);
    fixture.componentRef.setInput('surahCatalogByJuz', surahCatalogByJuzFixture);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('qd-surah-jump-picker')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('select')).toBeNull();
  });
});
