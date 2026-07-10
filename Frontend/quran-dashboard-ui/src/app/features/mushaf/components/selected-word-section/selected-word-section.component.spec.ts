import { describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SelectedWordSectionComponent } from './selected-word-section.component';
import { ResourceLoadState, WordAnalysisViewModel } from '../../models/mushaf.models';
import { segmentSlotToColor } from '../../state/segment-color-palette';

const WORD_TEXT_PLACEHOLDER = 'كلمة-تجريبية-١';
const WORD_SIMPLE_PLACEHOLDER = 'كلمة-مبسطة-١';
const SEGMENT_TEXT_PLACEHOLDER = 'قطعة-تجريبية-١';
const I3RAB_PLACEHOLDER = 'إعراب-تجريبي-١';
const WORD_KEY_PLACEHOLDER = 'مفتاح-كلمة-تجريبي';

const IDLE: ResourceLoadState = { isLoading: false, isEmpty: false, errorMessage: null };

function buildWordAnalysisViewModel(): WordAnalysisViewModel {
  return {
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
      orderedTashkeel: { occurrencesCount: 7, ayahsCount: 7, surahsCount: 3 },
      orderedSimple: { occurrencesCount: 9, ayahsCount: 9, surahsCount: 4 },
      uniqueTashkeel: { id: 101, occurrencesCount: 7, ayahsCount: 7, surahsCount: 3 },
      uniqueSimple: {
        id: 202,
        occurrencesCount: 9,
        ayahsCount: 9,
        surahsCount: 4,
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
    segments: [
      {
        segmentLocation: '2:25:3:1',
        segmentNumber: 1,
        segmentColorSlot: 1,
        color: segmentSlotToColor(1),
        segmentKind: 'STEM',
        segmentDisplayText: SEGMENT_TEXT_PLACEHOLDER,
        isMissing: false,
        segmentPos: 'V',
        segmentPosLabel: { ar: 'فعل', en: 'Verb' },
        segmentI3rabArabic: I3RAB_PLACEHOLDER,
        i3rabStatus: 'approved',
      },
    ],
  };
}

function setInputs(
  fixture: ComponentFixture<SelectedWordSectionComponent>,
  inputs: {
    analysis?: WordAnalysisViewModel | null;
    loadState: ResourceLoadState;
    selectedWordLocation?: string | null;
    embedded?: boolean;
  },
): void {
  fixture.componentRef.setInput('analysis', inputs.analysis ?? null);
  fixture.componentRef.setInput('loadState', inputs.loadState);
  fixture.componentRef.setInput('selectedWordLocation', inputs.selectedWordLocation ?? null);
  fixture.componentRef.setInput('embedded', inputs.embedded ?? false);
  fixture.detectChanges();
}

describe('SelectedWordSectionComponent — stable loading (UI-001)', () => {
  it('keeps the shell + static labels mounted and shows structured block skeletons while loading', () => {
    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    setInputs(fixture, {
      analysis: null,
      loadState: { isLoading: true, isEmpty: false, errorMessage: null },
      selectedWordLocation: '2:25:3',
    });

    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="word-analysis-loading"]')).toBeTruthy();
    expect(root.querySelector('.qd-loading-state')).toBeNull();

    expect(root.querySelector('.selected-word-section__header')).toBeTruthy();
    expect(root.querySelector('.selected-word-section__content')).toBeTruthy();

    const morphology = root.querySelector('[data-testid="word-morphology-loading"]');
    expect(morphology).toBeTruthy();
    for (const label of ['نوع الكلمة', 'الجذر', 'الصيغة المعجمية', 'الأصل الصرفي']) {
      expect(morphology?.textContent).toContain(label);
    }
    const identity = root.querySelector('[data-testid="word-identity-loading"]');
    expect(identity?.textContent).toContain('التكرار (بالتشكيل)');
    expect(identity?.textContent).toContain('التكرار (مبسّط)');

    const segmentSkeletons = root.querySelector('[data-testid="segment-skeletons"]');
    expect(segmentSkeletons).toBeTruthy();
    expect(segmentSkeletons?.querySelectorAll('.selected-word-section__segment-skeleton').length).toBeGreaterThan(0);

    expect(root.querySelectorAll('.qd-skeleton').length).toBeGreaterThan(0);
    expect(root.querySelector('.qd-loading-overlay')).toBeNull();
  });

  it('keeps each fixed info card mounted with a static label and shimmers only its value', () => {
    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    setInputs(fixture, {
      analysis: null,
      loadState: { isLoading: true, isEmpty: false, errorMessage: null },
      selectedWordLocation: '2:25:3',
    });

    const root = fixture.nativeElement as HTMLElement;

    const morphologyCells = Array.from(
      root.querySelectorAll('.selected-word-section__morphology-skeleton-cell'),
    );
    expect(morphologyCells).toHaveLength(4);
    for (const cell of morphologyCells) {
      const label = cell.querySelector('dt');
      expect(label?.textContent?.trim().length).toBeGreaterThan(0);
      expect(label?.querySelector('.qd-skeleton')).toBeNull();
      expect(cell.querySelector('dd .qd-skeleton')).toBeTruthy();
    }

    const identity = root.querySelector('[data-testid="word-identity-loading"]');
    expect(identity?.classList.contains('selected-word-section__identity--loading')).toBe(true);
    const identityRows = Array.from(root.querySelectorAll('.selected-word-section__identity-row'));
    expect(identityRows).toHaveLength(2);
    for (const row of identityRows) {
      expect(row.querySelector('dt')?.querySelector('.qd-skeleton')).toBeNull();
      expect(row.querySelector('dd .qd-skeleton')).toBeTruthy();
    }

    expect(root.querySelectorAll('.selected-word-section__segment-skeleton')).toHaveLength(3);
  });

  it('never exposes the previous word glyph in the header while loading (UI-001 refinement)', () => {

    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    setInputs(fixture, {
      analysis: buildWordAnalysisViewModel(),
      loadState: { isLoading: true, isEmpty: false, errorMessage: null },
      selectedWordLocation: '2:25:3',
    });

    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('qd-segment-rendered-word')).toBeNull();
    expect(root.querySelector('.selected-word-section__word-skeleton')).toBeTruthy();

    expect(root.querySelector('qd-segment-data-rows')).toBeNull();
    expect(root.querySelector('[data-testid="word-identity-summary"]')).toBeNull();
    expect(root.querySelector('[data-testid="word-morphology-loading"]')).toBeTruthy();
  });

  it('uses structured block skeletons, not a single overlay block, while loading', () => {
    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    setInputs(fixture, {
      analysis: buildWordAnalysisViewModel(),
      loadState: { isLoading: true, isEmpty: false, errorMessage: null },
      selectedWordLocation: '2:25:3',
    });

    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('.qd-loading-overlay')).toBeNull();
    expect(root.querySelector('[data-testid="segment-skeletons"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="word-morphology-loading"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="word-identity-loading"]')).toBeTruthy();
  });

  it('renders real analysis data and no skeletons when loaded', () => {
    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    setInputs(fixture, {
      analysis: buildWordAnalysisViewModel(),
      loadState: IDLE,
      selectedWordLocation: '2:25:3',
    });

    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="word-analysis-loading"]')).toBeNull();
    expect(root.querySelector('qd-segment-rendered-word')).toBeTruthy();
    expect(root.querySelector('[data-testid="word-identity-summary"]')).toBeTruthy();
    expect(root.querySelectorAll('.qd-skeleton').length).toBe(0);
  });

  it('opens the root explorer in a new tab when morphology includes a root id', () => {
    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    const analysis = buildWordAnalysisViewModel();
    analysis.morphology = {
      ...analysis.morphology,
      root: { id: 999, text: 'جذر-تجريبي', buckwalter: 'jhr-test' },
    };
    setInputs(fixture, {
      analysis,
      loadState: IDLE,
      selectedWordLocation: '2:25:3',
    });

    const root = fixture.nativeElement as HTMLElement;
    const rootLink = root.querySelector(
      '[data-testid="word-morphology-root-link"]',
    ) as HTMLAnchorElement | null;

    expect(rootLink?.getAttribute('href')).toBe('/dashboard/words/roots?root=999');
    expect(rootLink?.getAttribute('target')).toBe('_blank');
    expect(rootLink?.getAttribute('rel')).toBe('noopener noreferrer');
  });

  it('opens the root, lemma, and stem explorers in new tabs when morphology includes ids', () => {
    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    const analysis = buildWordAnalysisViewModel();
    analysis.morphology = {
      ...analysis.morphology,
      root: { id: 999, text: 'جذر-تجريبي', buckwalter: 'jhr-test' },
      lemma: { id: 555, text: 'لِمَة-تجريبية', buckwalter: 'lemma-test' },
      stem: { id: 777, text: 'سِتَم-تجريبي' },
    };
    setInputs(fixture, {
      analysis,
      loadState: IDLE,
      selectedWordLocation: '2:25:3',
    });

    const root = fixture.nativeElement as HTMLElement;
    const rootLink = root.querySelector('[data-testid="word-morphology-root-link"]') as HTMLAnchorElement | null;
    const lemmaLink = root.querySelector('[data-testid="word-morphology-lemma-link"]') as HTMLAnchorElement | null;
    const stemLink = root.querySelector('[data-testid="word-morphology-stem-link"]') as HTMLAnchorElement | null;

    expect(rootLink?.getAttribute('href')).toBe('/dashboard/words/roots?root=999');
    expect(rootLink?.getAttribute('target')).toBe('_blank');
    expect(rootLink?.getAttribute('rel')).toBe('noopener noreferrer');

    expect(lemmaLink?.getAttribute('href')).toBe('/dashboard/words/lemmas?lemma=555&view=words&wordView=simple');
    expect(lemmaLink?.getAttribute('target')).toBe('_blank');
    expect(lemmaLink?.getAttribute('rel')).toBe('noopener noreferrer');

    expect(stemLink?.getAttribute('href')).toBe('/dashboard/words/stems?stem=777&view=words&wordView=simple');
    expect(stemLink?.getAttribute('target')).toBe('_blank');
    expect(stemLink?.getAttribute('rel')).toBe('noopener noreferrer');
  });

  it('does not render a root explorer link when morphology has no root id', () => {
    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    setInputs(fixture, {
      analysis: buildWordAnalysisViewModel(),
      loadState: IDLE,
      selectedWordLocation: '2:25:3',
    });

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="word-morphology-root-link"]')).toBeNull();
  });

  it('opens both unique-word identity rows in a new tab using their nested ids', () => {
    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    setInputs(fixture, {
      analysis: buildWordAnalysisViewModel(),
      loadState: IDLE,
      selectedWordLocation: '2:25:3',
    });

    const root = fixture.nativeElement as HTMLElement;
    const tashkeelLink = root.querySelector(
      '[data-testid="word-identity-tashkeel-link"]',
    ) as HTMLAnchorElement | null;
    const simpleLink = root.querySelector(
      '[data-testid="word-identity-simple-link"]',
    ) as HTMLAnchorElement | null;

    expect(tashkeelLink?.getAttribute('href')).toBe(
      '/dashboard/words/unique/tashkeel?word=101&view=ayahs',
    );
    expect(tashkeelLink?.getAttribute('target')).toBe('_blank');
    expect(tashkeelLink?.getAttribute('rel')).toBe('noopener noreferrer');

    expect(simpleLink?.getAttribute('href')).toBe(
      '/dashboard/words/unique/simple?word=202&view=ayahs',
    );
    expect(simpleLink?.getAttribute('target')).toBe('_blank');
    expect(simpleLink?.getAttribute('rel')).toBe('noopener noreferrer');
  });

  it('renders the empty "select a word" state when no word is selected', () => {
    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    setInputs(fixture, {
      analysis: null,
      loadState: IDLE,
      selectedWordLocation: null,
    });

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('.qd-empty-state')).toBeTruthy();
    expect(root.querySelector('[data-testid="word-analysis-loading"]')).toBeNull();
    expect(root.querySelector('.qd-skeleton')).toBeNull();
  });

  it('renders the error state and does not hide it behind a skeleton', () => {
    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    setInputs(fixture, {
      analysis: null,
      loadState: { isLoading: false, isEmpty: false, errorMessage: 'تعذّر الاتصال' },
      selectedWordLocation: '2:25:3',
    });

    const root = fixture.nativeElement as HTMLElement;
    const error = root.querySelector('[data-testid="word-analysis-error"]');
    expect(error).toBeTruthy();
    expect(error?.textContent).toContain('تعذّر الاتصال');

    expect(root.querySelector('.qd-skeleton')).toBeNull();
  });

  it('renders the failed-to-load empty state (word selected, not loading, no analysis) without a skeleton', () => {
    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    setInputs(fixture, {
      analysis: null,
      loadState: { isLoading: false, isEmpty: true, errorMessage: null },
      selectedWordLocation: '2:25:3',
    });

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('.qd-empty-state')).toBeTruthy();
    expect(root.querySelector('[data-testid="word-analysis-loading"]')).toBeNull();
    expect(root.querySelector('.qd-skeleton')).toBeNull();
  });

  it('does not flip from failed-empty back to skeleton when loadState reports isEmpty without isLoading', () => {

    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    setInputs(fixture, {
      analysis: null,
      loadState: { isLoading: false, isEmpty: true, errorMessage: null },
      selectedWordLocation: '2:25:3',
    });

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('.qd-empty-state')?.textContent).toContain('تعذّر');
  });

  it('applies segment accent tint on loaded rows and matches skeleton min-height', () => {
    const loadingFixture = TestBed.createComponent(SelectedWordSectionComponent);
    setInputs(loadingFixture, {
      analysis: null,
      loadState: { isLoading: true, isEmpty: false, errorMessage: null },
      selectedWordLocation: '2:25:3',
    });

    const skeleton = loadingFixture.nativeElement.querySelector(
      '.selected-word-section__segment-skeleton',
    ) as HTMLElement;
    const skeletonMinHeight = getComputedStyle(skeleton).minHeight;

    const loadedFixture = TestBed.createComponent(SelectedWordSectionComponent);
    setInputs(loadedFixture, {
      analysis: buildWordAnalysisViewModel(),
      loadState: IDLE,
      selectedWordLocation: '2:25:3',
    });

    const row = loadedFixture.nativeElement.querySelector(
      '.segment-data-rows__row',
    ) as HTMLElement;

    expect(skeletonMinHeight).not.toBe('0px');
    expect(getComputedStyle(row).minHeight).toBe(skeletonMinHeight);
    expect(row.style.getPropertyValue('--segment-accent')).toBe(segmentSlotToColor(1));
    expect(getComputedStyle(row).display).toBe('grid');
    expect(getComputedStyle(skeleton).display).toBe('grid');
  });
});
