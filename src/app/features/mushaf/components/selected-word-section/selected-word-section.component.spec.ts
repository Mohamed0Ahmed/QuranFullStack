import { describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SelectedWordSectionComponent } from './selected-word-section.component';
import { ResourceLoadState, WordAnalysisViewModel } from '../../models/mushaf.models';
import { segmentSlotToColor } from '../../state/segment-color-palette';

/*
 * Source-safe synthetic placeholders — not Quranic text. Mirrors the
 * placeholders used in mushaf-reader.facade.word-analysis.spec.ts.
 */
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
      uniqueTashkeel: { id: 1, occurrencesCount: 7, ayahsCount: 7, surahsCount: 3 },
      uniqueSimple: {
        id: 1,
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
  it('keeps the shell + content sections mounted and shows skeletons instead of a one-line loading state', () => {
    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    setInputs(fixture, {
      analysis: null,
      loadState: { isLoading: true, isEmpty: false, errorMessage: null },
      selectedWordLocation: '2:25:3',
    });

    const root = fixture.nativeElement as HTMLElement;

    // The loading testid is present (replaces the old one-line qd-loading-state).
    expect(root.querySelector('[data-testid="word-analysis-loading"]')).toBeTruthy();

    // The OLD behavior rendered ONLY a one-line loading state and removed the
    // shell. That must no longer happen.
    expect(root.querySelector('.qd-loading-state')).toBeNull();

    // The card shell stays mounted: the section wrapper and its header/content
    // structure are present even while loading.
    expect(root.querySelector('.selected-word-section__header')).toBeTruthy();
    expect(root.querySelector('.selected-word-section__content')).toBeTruthy();
    expect(root.querySelector('.selected-word-section__identity')).toBeTruthy();

    // Skeleton placeholders fill the content regions.
    expect(root.querySelectorAll('.qd-skeleton').length).toBeGreaterThan(0);

    // Real data rows are NOT mounted while loading (avoid stale content).
    expect(root.querySelector('qd-segment-rendered-word')).toBeNull();
    expect(root.querySelector('[data-testid="word-identity-summary"]')).toBeNull();
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
    // Error wins over skeleton.
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
    // Guards the boundary in the template: empty + not-loading must win over
    // the skeleton branch even when a word location is present.
    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    setInputs(fixture, {
      analysis: null,
      loadState: { isLoading: false, isEmpty: true, errorMessage: null },
      selectedWordLocation: '2:25:3',
    });

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('.qd-empty-state')?.textContent).toContain('تعذّر');
  });
});
