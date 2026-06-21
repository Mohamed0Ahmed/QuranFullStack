import { describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';

import { MushafAyahStudyApi } from '../../data-access/mushaf-ayah-study.api';
import { MushafPagesApi } from '../../data-access/mushaf-pages.api';
import { MushafStudySourceCatalogApi } from '../../data-access/mushaf-study-sources.api';
import { MushafWordAnalysisApi } from '../../data-access/mushaf-word-analysis.api';
import { MushafReaderFacade } from '../../state/mushaf-reader.facade';
import { MushafReaderPageComponent } from './mushaf-reader-page.component';
import { ayahStudyDtoMock, mushafSimilarAyahsApiProvider, mushafStudySourceCatalogApiProvider } from '../../state/mushaf-study-source-catalog.api.mock';

const ayahStudyDto = ayahStudyDtoMock;

const wordAnalysisDto = {
  word: {
    quranWordId: 2003,
    wordLocation: '2:25:3',
    verseKey: '2:25',
    surahNumber: 2,
    ayahNumber: 25,
    wordNumber: 3,
    pageNumber: 5,
    lineNumber: 1,
    lineWordOrder: 3,
    textUthmani: 'كلمة-تجريبية',
    textUthmaniSimple: 'كلمة-مبسطة',
    textImlaeiSimple: 'كلمة-مبسطة',
    qpcGlyph: 'glyph-test-1',
  },
  identity: {
    orderedTashkeel: { occurrencesCount: 1, ayahsCount: 1, surahsCount: 1 },
    orderedSimple: { occurrencesCount: 1, ayahsCount: 1, surahsCount: 1 },
    uniqueTashkeel: { id: 1, occurrencesCount: 1, ayahsCount: 1, surahsCount: 1 },
    uniqueSimple: {
      id: 1,
      occurrencesCount: 1,
      ayahsCount: 1,
      surahsCount: 1,
      wordKeyImlaeiSimple: 'مفتاح-تجريبي',
    },
  },
  morphology: {
    headPos: 'V',
    headPosLabel: { ar: 'فعل', en: 'Verb' },
    root: null,
    lemma: null,
    stem: null,
    isVerb: true,
    verbTense: 'past',
    verbVoice: 'active',
    caseFeature: null,
  },
  renderedWordSegments: [],
};

describe('MushafReaderPageComponent study layout', () => {
  it('renders a single unified study context card', () => {
    const queryParamMap$ = new BehaviorSubject(convertToParamMap({ page: '5', word: '2:25:3', ayah: '2:25' }));

    TestBed.configureTestingModule({
      imports: [MushafReaderPageComponent],
      providers: [
        MushafReaderFacade,
        {
          provide: ActivatedRoute,
          useValue: { queryParamMap: queryParamMap$.asObservable() },
        },
        {
          provide: Router,
          useValue: { navigate: vi.fn().mockResolvedValue(true) },
        },
        {
          provide: MushafPagesApi,
          useValue: {
            getPage: vi.fn(() =>
              of({
                isSuccess: true,
                message: 'ok',
                data: {
                  pageNumber: 5,
                  previousPageNumber: 4,
                  nextPageNumber: 6,
                  surahs: [],
                  ayahRange: { firstVerseKey: '2:25', lastVerseKey: '2:26' },
                  navigation: { juzNumbers: [], hizbNumbers: [], rubNumbers: [] },
                  lines: [],
                  markers: [],
                },
              }),
            ),
          },
        },
        {
          provide: MushafAyahStudyApi,
          useValue: {
            getAyahStudy: vi.fn(() => of({ isSuccess: true, message: 'ok', data: ayahStudyDto })),
          },
        },
        {
          provide: MushafWordAnalysisApi,
          useValue: {
            getWordAnalysis: vi.fn(() => of({ isSuccess: true, message: 'ok', data: wordAnalysisDto })),
          },
        },
        {
          provide: MushafStudySourceCatalogApi,
          useValue: {
            getCatalog: vi.fn(() =>
              of({
                isSuccess: true,
                message: 'ok',
                data: { tafsirSources: [], translationSources: [], fullI3rabSources: [] },
              }),
            ),
          },
        },
        mushafSimilarAyahsApiProvider,
      ],
    });

    const fixture: ComponentFixture<MushafReaderPageComponent> = TestBed.createComponent(
      MushafReaderPageComponent,
    );
    fixture.detectChanges();

    const facade = TestBed.inject(MushafReaderFacade);
    expect(facade.selectedAyahKey()).toBe('2:25');
    expect(facade.selectedWordLocation()).toBe('2:25:3');

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="study-context-section"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="selected-word-section"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="selected-ayah-section"]')).toBeTruthy();
    expect(root.querySelector('.mushaf-reader__mobile-tabs')).toBeNull();
  });

  it('moves between words with ArrowLeft and ArrowRight', () => {
    const queryParamMap$ = new BehaviorSubject(convertToParamMap({ page: '5', word: '2:25:3', ayah: '2:25' }));

    TestBed.configureTestingModule({
      imports: [MushafReaderPageComponent],
      providers: [
        MushafReaderFacade,
        {
          provide: ActivatedRoute,
          useValue: { queryParamMap: queryParamMap$.asObservable() },
        },
        {
          provide: Router,
          useValue: { navigate: vi.fn().mockResolvedValue(true) },
        },
        {
          provide: MushafPagesApi,
          useValue: {
            getPage: vi.fn(() =>
              of({
                isSuccess: true,
                message: 'ok',
                data: {
                  pageNumber: 5,
                  previousPageNumber: 4,
                  nextPageNumber: 6,
                  surahs: [],
                  ayahRange: { firstVerseKey: '2:25', lastVerseKey: '2:26' },
                  navigation: { juzNumbers: [], hizbNumbers: [], rubNumbers: [] },
                  lines: [],
                  markers: [],
                },
              }),
            ),
          },
        },
        {
          provide: MushafAyahStudyApi,
          useValue: {
            getAyahStudy: vi.fn(() => of({ isSuccess: true, message: 'ok', data: ayahStudyDto })),
          },
        },
        {
          provide: MushafWordAnalysisApi,
          useValue: {
            getWordAnalysis: vi.fn(() => of({ isSuccess: true, message: 'ok', data: wordAnalysisDto })),
          },
        },
        {
          provide: MushafStudySourceCatalogApi,
          useValue: {
            getCatalog: vi.fn(() =>
              of({
                isSuccess: true,
                message: 'ok',
                data: { tafsirSources: [], translationSources: [], fullI3rabSources: [] },
              }),
            ),
          },
        },
        mushafSimilarAyahsApiProvider,
      ],
    });

    const fixture = TestBed.createComponent(MushafReaderPageComponent);
    fixture.detectChanges();

    const facade = TestBed.inject(MushafReaderFacade);
    const moveSelectedWord = vi.spyOn(facade, 'moveSelectedWord');

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }));
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));

    expect(moveSelectedWord).toHaveBeenCalledWith('next');
    expect(moveSelectedWord).toHaveBeenCalledWith('previous');
  });

  it('ignores arrow keys when focus is inside an input', () => {
    const queryParamMap$ = new BehaviorSubject(convertToParamMap({ page: '5', word: '2:25:3', ayah: '2:25' }));

    TestBed.configureTestingModule({
      imports: [MushafReaderPageComponent],
      providers: [
        MushafReaderFacade,
        {
          provide: ActivatedRoute,
          useValue: { queryParamMap: queryParamMap$.asObservable() },
        },
        {
          provide: Router,
          useValue: { navigate: vi.fn().mockResolvedValue(true) },
        },
        {
          provide: MushafPagesApi,
          useValue: {
            getPage: vi.fn(() =>
              of({
                isSuccess: true,
                message: 'ok',
                data: {
                  pageNumber: 5,
                  previousPageNumber: 4,
                  nextPageNumber: 6,
                  surahs: [],
                  ayahRange: { firstVerseKey: '2:25', lastVerseKey: '2:26' },
                  navigation: { juzNumbers: [], hizbNumbers: [], rubNumbers: [] },
                  lines: [],
                  markers: [],
                },
              }),
            ),
          },
        },
        {
          provide: MushafAyahStudyApi,
          useValue: {
            getAyahStudy: vi.fn(() => of({ isSuccess: true, message: 'ok', data: ayahStudyDto })),
          },
        },
        {
          provide: MushafWordAnalysisApi,
          useValue: {
            getWordAnalysis: vi.fn(() => of({ isSuccess: true, message: 'ok', data: wordAnalysisDto })),
          },
        },
        {
          provide: MushafStudySourceCatalogApi,
          useValue: {
            getCatalog: vi.fn(() =>
              of({
                isSuccess: true,
                message: 'ok',
                data: { tafsirSources: [], translationSources: [], fullI3rabSources: [] },
              }),
            ),
          },
        },
        mushafSimilarAyahsApiProvider,
      ],
    });

    const fixture = TestBed.createComponent(MushafReaderPageComponent);
    fixture.detectChanges();

    const facade = TestBed.inject(MushafReaderFacade);
    const moveSelectedWord = vi.spyOn(facade, 'moveSelectedWord');

    const event = new KeyboardEvent('keydown', { key: 'ArrowLeft' });
    const preventDefault = vi.spyOn(event, 'preventDefault');
    Object.defineProperty(event, 'target', { value: document.createElement('input') });

    (fixture.componentInstance as unknown as { onDocumentKeydown(e: KeyboardEvent): void }).onDocumentKeydown(event);

    expect(moveSelectedWord).not.toHaveBeenCalled();
    expect(preventDefault).not.toHaveBeenCalled();
  });
});
