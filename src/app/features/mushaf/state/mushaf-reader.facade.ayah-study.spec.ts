import { describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { MushafAyahStudyApi } from '../data-access/mushaf-ayah-study.api';
import { MushafPagesApi } from '../data-access/mushaf-pages.api';
import { MushafWordAnalysisApi } from '../data-access/mushaf-word-analysis.api';
import { AyahStudyDto } from '../models/mushaf.models';
import { MushafReaderFacade } from './mushaf-reader.facade';
import { mushafStudySourceCatalogApiProvider } from './mushaf-study-source-catalog.api.mock';
import { SelectedAyahSectionComponent } from '../components/selected-ayah-section/selected-ayah-section.component';

const pageDto = {
  pageNumber: 5,
  previousPageNumber: 4,
  nextPageNumber: 6,
  surahs: [],
  ayahRange: { firstVerseKey: '2:25', lastVerseKey: '2:26' },
  navigation: { juzNumbers: [], hizbNumbers: [], rubNumbers: [] },
  lines: [],
  markers: [],
};

const ayahStudyDto: AyahStudyDto = {
  ayah: {
    verseKey: '2:25',
    surahNumber: 2,
    surahNameArabic: 'البقرة',
    ayahNumber: 25,
    textUthmani: 'نص تجريبي للآية',
    wordsCount: 5,
    pageFrom: 5,
    pageTo: 5,
    juzNumber: 1,
    hizbNumber: 1,
    rubNumber: 1,
    sajda: null,
  },
  selectedSources: {
    tafsirSource: 'ar-muyassar',
    translationSource: 'en-sahih-international',
    fullI3rabSource: 'muyassar',
  },
  tafsir: {
    sourceKey: 'ar-muyassar',
    displayNameAr: 'التفسير الميسر',
    shortNameAr: null,
    languageCode: 'ar',
    direction: 'rtl',
    tafsirKind: 'brief',
    sourceValueKind: 'leader',
    sourceLeaderVerseKey: '2:25',
    isGroupLeader: true,
    coveredAyahCount: 2,
    coveredAyahKeys: ['2:25', '2:26'],
    text: '<p>تفسير تجريبي</p>',
  },
  translation: {
    sourceKey: 'en-sahih-international',
    displayNameAr: null,
    displayNameEn: 'Sahih International',
    languageCode: 'en',
    direction: 'ltr',
    translationType: 'simple',
    containsHtmlMarkup: false,
    text: 'Sample translation text',
  },
  fullI3rab: {
    sourceKey: 'muyassar',
    displayNameAr: 'الإعراب الميسر',
    shortNameAr: null,
    markupFormat: 'html',
    sourceValueKind: 'flat',
    sourceLeaderVerseKey: '2:25',
    isGroupLeader: true,
    coveredAyahCount: 1,
    coveredAyahKeys: ['2:25'],
    html: '<p>إعراب تجريبي</p>',
  },
};

describe('MushafReaderFacade.loadPage', () => {
  it('does not refetch or enter loading when the same page is already rendered', () => {
    const getPage = vi.fn(() => of({ isSuccess: true, message: 'ok', data: pageDto }));

    TestBed.configureTestingModule({
      providers: [
        MushafReaderFacade,
        { provide: MushafPagesApi, useValue: { getPage } },
        { provide: MushafAyahStudyApi, useValue: { getAyahStudy: vi.fn() } },
        { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
        mushafStudySourceCatalogApiProvider,
      ],
    });

    const facade = TestBed.inject(MushafReaderFacade);
    facade.loadPage(5);
    // Initial load plus optional adjacent-page prefetch (pages 4 and 6).
    expect(getPage).toHaveBeenCalledTimes(3);
    expect(facade.pageLoadState().isLoading).toBe(false);

    facade.loadPage(5);
    expect(getPage).toHaveBeenCalledTimes(3);
    expect(facade.pageLoadState().isLoading).toBe(false);
  });
});

describe('MushafReaderFacade.loadAyahStudy', () => {
  it('maps a successful response with all three source blocks', () => {
    const getAyahStudy = vi.fn(() => of({ isSuccess: true, message: 'تم', data: ayahStudyDto }));

    TestBed.configureTestingModule({
      providers: [
        MushafReaderFacade,
        { provide: MushafPagesApi, useValue: { getPage: vi.fn() } },
        { provide: MushafAyahStudyApi, useValue: { getAyahStudy } },
        { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
        mushafStudySourceCatalogApiProvider,
      ],
    });

    const facade = TestBed.inject(MushafReaderFacade);
    facade.loadAyahStudy('2:25');

    expect(facade.ayahStudy()?.tafsir?.sourceKey).toBe('ar-muyassar');
    expect(facade.ayahStudy()?.translation?.sourceKey).toBe('en-sahih-international');
    expect(facade.ayahStudy()?.fullI3rab?.sourceKey).toBe('muyassar');
    expect(facade.sources().tafsirSource).toBe('ar-muyassar');
  });
});

describe('MushafReaderFacade.applyUrlState', () => {
  it('reloads ayah study when the URL source key changes', () => {
    const getAyahStudy = vi.fn(() => of({ isSuccess: true, message: 'تم', data: ayahStudyDto }));

    TestBed.configureTestingModule({
      providers: [
        MushafReaderFacade,
        { provide: MushafPagesApi, useValue: { getPage: vi.fn() } },
        { provide: MushafAyahStudyApi, useValue: { getAyahStudy } },
        { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
        mushafStudySourceCatalogApiProvider,
      ],
    });

    const facade = TestBed.inject(MushafReaderFacade);
    facade.applyUrlState({
      panel: 'none',
      ayah: '2:25',
      sources: {
        tafsirSource: null,
        translationSource: null,
        fullI3rabSource: null,
      },
      ayahTab: 'tafsir',
      word: null,
      segment: null,
      wordTab: 'segments',
    });
    facade.applyUrlState({
      panel: 'none',
      ayah: '2:25',
      sources: {
        tafsirSource: null,
        translationSource: 'another-translation',
        fullI3rabSource: null,
      },
      ayahTab: 'tafsir',
      word: null,
      segment: null,
      wordTab: 'segments',
    });

    expect(getAyahStudy).toHaveBeenCalledTimes(2);
    expect(getAyahStudy).toHaveBeenLastCalledWith('2:25', expect.objectContaining({
      translationSource: 'another-translation',
    }));
  });

  it('does not reload ayah study when only the ayah tab changes', () => {
    const getAyahStudy = vi.fn(() => of({ isSuccess: true, message: 'تم', data: ayahStudyDto }));

    TestBed.configureTestingModule({
      providers: [
        MushafReaderFacade,
        { provide: MushafPagesApi, useValue: { getPage: vi.fn() } },
        { provide: MushafAyahStudyApi, useValue: { getAyahStudy } },
        { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis: vi.fn() } },
        mushafStudySourceCatalogApiProvider,
      ],
    });

    const facade = TestBed.inject(MushafReaderFacade);
    facade.applyUrlState({
      panel: 'none',
      ayah: '2:25',
      sources: {
        tafsirSource: null,
        translationSource: null,
        fullI3rabSource: null,
      },
      ayahTab: 'tafsir',
      word: null,
      segment: null,
      wordTab: 'segments',
    });
    facade.applyUrlState({
      panel: 'none',
      ayah: '2:25',
      sources: {
        tafsirSource: null,
        translationSource: null,
        fullI3rabSource: null,
      },
      ayahTab: 'translation',
      word: null,
      segment: null,
      wordTab: 'segments',
    });

    expect(getAyahStudy).toHaveBeenCalledTimes(1);
    expect(facade.ayahTab()).toBe('translation');
  });
});

describe('SelectedAyahSectionComponent', () => {
  it('shows grouped coverage note and selected source label for tafsir tab', () => {
    const fixture: ComponentFixture<SelectedAyahSectionComponent> = TestBed.createComponent(
      SelectedAyahSectionComponent,
    );
    fixture.componentRef.setInput('study', {
      ayah: ayahStudyDto.ayah,
      selectedSources: ayahStudyDto.selectedSources,
      tafsir: ayahStudyDto.tafsir,
      translation: ayahStudyDto.translation,
      fullI3rab: ayahStudyDto.fullI3rab,
    });
    fixture.componentRef.setInput('loadState', { isLoading: false, isEmpty: false, errorMessage: null });
    fixture.componentRef.setInput('activeTab', 'tafsir');
    fixture.componentRef.setInput('selectedVerseKey', '2:25');
    fixture.componentRef.setInput('tafsirOptions', [{ key: 'ar-muyassar', label: 'التفسير الميسر' }]);
    fixture.componentRef.setInput('translationOptions', []);
    fixture.componentRef.setInput('fullI3rabOptions', []);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('[data-testid="grouped-coverage"]')?.textContent).toContain('25');
    expect(el.querySelector('[data-testid="selected-ayah-section-ayah"]')?.textContent).toContain('نص تجريبي للآية');
    expect(el.querySelector('[data-testid="source-single-option"]')?.textContent).toContain('التفسير الميسر');
    expect(el.querySelector('[data-testid="tafsir-card"]')).toBeTruthy();
  });
});
