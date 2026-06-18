import { describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { MushafAyahStudyApi } from '../data-access/mushaf-ayah-study.api';
import { MushafPagesApi } from '../data-access/mushaf-pages.api';
import { MushafSurahCatalogApi } from '../data-access/mushaf-surah-catalog.api';
import { MushafWordAnalysisApi } from '../data-access/mushaf-word-analysis.api';
import { MushafPageViewComponent } from '../components/mushaf-page-view/mushaf-page-view.component';
import { MushafWordComponent } from '../components/mushaf-word/mushaf-word.component';
import { SelectedWordSectionComponent } from '../components/selected-word-section/selected-word-section.component';
import { WordAnalysisDto } from '../models/mushaf.models';
import { MushafReaderFacade } from './mushaf-reader.facade';
import { segmentSlotToColor } from './segment-color-palette';

/** Source-safe synthetic placeholders — not Quranic text. */
const WORD_TEXT_PLACEHOLDER = 'كلمة-تجريبية-١';
const WORD_SIMPLE_PLACEHOLDER = 'كلمة-مبسطة-١';
const SEGMENT_TEXT_PLACEHOLDER = 'قطعة-تجريبية-١';
const I3RAB_PLACEHOLDER = 'إعراب-تجريبي-١';
const MARKER_TEXT_PLACEHOLDER = 'علامة-تجريبية';
const WORD_KEY_PLACEHOLDER = 'مفتاح-كلمة-تجريبي';

const pageDto = {
  pageNumber: 5,
  previousPageNumber: 4,
  nextPageNumber: 6,
  surahs: [],
  ayahRange: { firstVerseKey: '2:25', lastVerseKey: '2:26' },
  navigation: { juzNumbers: [], hizbNumbers: [], rubNumbers: [] },
  lines: [
    {
      lineNumber: 1,
      lineType: 'ayah' as const,
      isCentered: false,
      surahNumber: null,
      words: [
        {
          wordLocation: '2:25:3',
          verseKey: '2:25',
          wordNumber: 3,
          lineWordOrder: 3,
          textUthmani: WORD_TEXT_PLACEHOLDER,
          isAyahMarker: false,
        },
        {
          wordLocation: '2:25:5',
          verseKey: '2:25',
          wordNumber: 5,
          lineWordOrder: 5,
          textUthmani: MARKER_TEXT_PLACEHOLDER,
          isAyahMarker: true,
        },
      ],
    },
  ],
  markers: [],
};

const wordAnalysisDto: WordAnalysisDto = {
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
    textUthmani: WORD_TEXT_PLACEHOLDER,
    textUthmaniSimple: WORD_SIMPLE_PLACEHOLDER,
    textImlaeiSimple: WORD_SIMPLE_PLACEHOLDER,
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
      wordKeyImlaeiSimple: WORD_KEY_PLACEHOLDER,
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
  renderedWordSegments: [
    {
      segmentLocation: '2:25:3:1',
      segmentNumber: 1,
      segmentColorSlot: 1,
      segmentKind: 'STEM',
      segmentDisplayText: SEGMENT_TEXT_PLACEHOLDER,
      displayTextStatus: 'available',
      segmentPos: 'V',
      segmentPosLabel: { ar: 'فعل', en: 'Verb' },
      segmentI3rabArabic: I3RAB_PLACEHOLDER,
      i3rabRuleId: null,
      i3rabRuleSignature: null,
      i3rabRuleFamily: null,
      i3rabStatus: 'approved',
      segmentFeatures: null,
    },
    {
      segmentLocation: '2:25:3:2',
      segmentNumber: 2,
      segmentColorSlot: 2,
      segmentKind: 'SUFFIX',
      segmentDisplayText: null,
      displayTextStatus: 'missing',
      segmentPos: 'PRON',
      segmentPosLabel: { ar: 'ضمير', en: 'Pronoun' },
      segmentI3rabArabic: null,
      i3rabRuleId: null,
      i3rabRuleSignature: null,
      i3rabRuleFamily: null,
      i3rabStatus: 'unsupported',
      segmentFeatures: null,
    },
  ],
};

function normalizeCssColor(color: string): string {
  const probe = document.createElement('span');
  probe.style.color = color;
  document.body.appendChild(probe);
  const normalized = getComputedStyle(probe).color;
  document.body.removeChild(probe);
  return normalized;
}

function buildWordAnalysisViewModel() {
  return {
    word: wordAnalysisDto.word,
    identity: wordAnalysisDto.identity,
    morphology: wordAnalysisDto.morphology,
    segments: wordAnalysisDto.renderedWordSegments.map((segment) => ({
      segmentLocation: segment.segmentLocation,
      segmentNumber: segment.segmentNumber,
      segmentColorSlot: segment.segmentColorSlot,
      color: segmentSlotToColor(segment.segmentColorSlot),
      segmentKind: segment.segmentKind,
      segmentDisplayText: segment.segmentDisplayText,
      isMissing: segment.displayTextStatus === 'missing',
      segmentPos: segment.segmentPos,
      segmentPosLabel: segment.segmentPosLabel,
      segmentI3rabArabic: segment.segmentI3rabArabic,
      i3rabStatus: segment.i3rabStatus,
    })),
  };
}

describe('MushafReaderFacade.loadWordAnalysis', () => {
  it('maps segment color slots to the visual-linking palette', () => {
    const getWordAnalysis = vi.fn(() => of({ isSuccess: true, message: 'تم', data: wordAnalysisDto }));

    TestBed.configureTestingModule({
      providers: [
        MushafReaderFacade,
        { provide: MushafPagesApi, useValue: { getPage: vi.fn() } },
        { provide: MushafAyahStudyApi, useValue: { getAyahStudy: vi.fn() } },
        { provide: MushafSurahCatalogApi, useValue: { getCatalog: vi.fn() } },
        { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis } },
      ],
    });

    const facade = TestBed.inject(MushafReaderFacade);
    facade.loadWordAnalysis('2:25:3');

    const segments = facade.wordAnalysis()?.segments ?? [];
    expect(segments[0].color).toBe(segmentSlotToColor(1));
    expect(segments[1].color).toBe(segmentSlotToColor(2));
    expect(segments[1].isMissing).toBe(true);
  });

  it('rejects ayah-end markers without calling the API', () => {
    const getWordAnalysis = vi.fn();
    const getPage = vi.fn(() => of({ isSuccess: true, message: 'ok', data: pageDto }));

    TestBed.configureTestingModule({
      providers: [
        MushafReaderFacade,
        { provide: MushafPagesApi, useValue: { getPage } },
        { provide: MushafAyahStudyApi, useValue: { getAyahStudy: vi.fn() } },
        { provide: MushafSurahCatalogApi, useValue: { getCatalog: vi.fn() } },
        { provide: MushafWordAnalysisApi, useValue: { getWordAnalysis } },
      ],
    });

    const facade = TestBed.inject(MushafReaderFacade);
    facade.loadPage(5);
    facade.loadWordAnalysis('2:25:5');

    expect(getWordAnalysis).not.toHaveBeenCalled();
    expect(facade.selectedWordLocation()).toBe('2:25:5');
    expect(facade.wordAnalysisLoadState().errorMessage).toContain('غير قابلة للتحليل');
  });
});

describe('SelectedWordSectionComponent', () => {
  it('shows the not-analyzable error when a marker word location is rejected', () => {
    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    fixture.componentRef.setInput('analysis', null);
    fixture.componentRef.setInput('loadState', {
      isLoading: false,
      isEmpty: false,
      errorMessage: 'هذه الكلمة غير قابلة للتحليل (علامة نهاية آية)',
    });
    fixture.componentRef.setInput('activeTab', 'segments');
    fixture.componentRef.setInput('selectedWordLocation', '2:25:5');
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('[data-testid="word-analysis-error"]');
    expect(error?.textContent).toContain('غير قابلة للتحليل');
    expect(fixture.nativeElement.querySelector('.qd-empty-state')).toBeNull();
  });

  it('renders matching segment colors across glued word, data rows, and i3rab labels', () => {
    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    const slot1Color = segmentSlotToColor(1);
    const slot2Color = segmentSlotToColor(2);
    const expectedSlot1 = normalizeCssColor(slot1Color);
    const expectedSlot2 = normalizeCssColor(slot2Color);

    fixture.componentRef.setInput('analysis', buildWordAnalysisViewModel());
    fixture.componentRef.setInput('loadState', { isLoading: false, isEmpty: false, errorMessage: null });
    fixture.componentRef.setInput('activeTab', 'segments');
    fixture.componentRef.setInput('selectedWordLocation', '2:25:3');
    fixture.detectChanges();

    const gluedRoot = fixture.nativeElement.querySelector(
      '[data-testid="segment-rendered-word"]',
    ) as HTMLElement;
    const gluedSeg1 = gluedRoot.querySelector('[data-segment-slot="1"]') as HTMLElement;
    const gluedSeg2 = gluedRoot.querySelector('[data-segment-slot="2"]') as HTMLElement;

    expect(gluedSeg1.style.color).toBe(expectedSlot1);
    expect(gluedSeg2.style.color).toBe(expectedSlot2);
    expect(gluedRoot.innerHTML).not.toMatch(/>\s+</);

    const row1 = fixture.nativeElement.querySelector(
      '[data-testid="segment-data-rows"] [data-segment-slot="1"]',
    ) as HTMLElement;
    const row1Marker = row1.querySelector('.segment-data-rows__number') as HTMLElement;
    const row1I3rab = row1.querySelector('[data-testid="segment-i3rab-label"]') as HTMLElement;

    expect(normalizeCssColor(row1.style.borderInlineStartColor)).toBe(expectedSlot1);
    expect(row1Marker.style.color).toBe(expectedSlot1);
    expect(row1I3rab.style.color).toBe(expectedSlot1);
    expect(row1I3rab.textContent).toContain(I3RAB_PLACEHOLDER);

    expect(fixture.nativeElement.querySelector('[data-testid="segment-missing-placeholder"]')).toBeTruthy();
  });
});

describe('MushafWordComponent marker selection', () => {
  it('does not emit wordSelect for ayah-end markers', () => {
    const fixture = TestBed.createComponent(MushafWordComponent);
    fixture.componentRef.setInput('word', {
      wordLocation: '2:25:5',
      verseKey: '2:25',
      wordNumber: 5,
      lineWordOrder: 5,
      textUthmani: MARKER_TEXT_PLACEHOLDER,
      isAyahMarker: true,
    });
    fixture.detectChanges();

    const wordSelect = vi.fn();
    fixture.componentInstance.wordSelect.subscribe(wordSelect);

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
    button.click();
    expect(wordSelect).not.toHaveBeenCalled();
  });
});

describe('MushafPageViewComponent segment isolation', () => {
  it('does not render segment forms in the Mushaf page area', () => {
    const fixture = TestBed.createComponent(MushafPageViewComponent);
    fixture.componentRef.setInput('page', {
      pageNumber: 5,
      previousPageNumber: 4,
      nextPageNumber: 6,
      surahs: [],
      ayahRange: { firstVerseKey: '2:25', lastVerseKey: '2:26' },
      navigation: { juzNumbers: [], hizbNumbers: [], rubNumbers: [] },
      lines: pageDto.lines,
      markers: [],
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-segment-form]')).toBeNull();
    expect(fixture.nativeElement.querySelector('qd-segment-rendered-word')).toBeNull();
  });
});
