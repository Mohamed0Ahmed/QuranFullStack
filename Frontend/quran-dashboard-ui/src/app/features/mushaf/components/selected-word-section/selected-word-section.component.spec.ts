import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { Router, provideRouter } from '@angular/router';

import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { SelectedWordSectionComponent } from './selected-word-section.component';
import { ResourceLoadState, WordAnalysisViewModel } from '../../models/mushaf.models';
import { segmentSlotToColor } from '../../state/segment-color-palette';

const WORD_TEXT_PLACEHOLDER = 'كلمة-تجريبية-١';
const WORD_SIMPLE_PLACEHOLDER = 'كلمة-مبسطة-١';
const SEGMENT_TEXT_PLACEHOLDER = 'قطعة-تجريبية-١';
const I3RAB_PLACEHOLDER = 'إعراب-تجريبي-١';
const WORD_KEY_PLACEHOLDER = 'مفتاح-كلمة-تجريبي';

const IDLE: ResourceLoadState = { isLoading: false, isEmpty: false, errorMessage: null };

@Component({ standalone: true, template: '' })
class BlankPageComponent {}

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
  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([{ path: '**', component: BlankPageComponent }]), provideLocationMocks()],
    });
    const router = TestBed.inject(Router);
    sessionStorage.clear();
    router.initialNavigation();
    await router.navigateByUrl('/dashboard/mushaf?page=5');
    TestBed.inject(DetailOverlayHistoryService).start();
  });

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

  it('renders the root anchor as a detail-overlay link when morphology includes a root id', () => {
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

    const href = rootLink?.getAttribute('href') ?? '';
    expect(href).toContain('/dashboard/mushaf');
    expect(href).toContain('page=5');
    expect(href).toContain(`qdDetail=${encodeURIComponent('v1~root~999~words~simple~mentioned~1')}`);
    expect(href).toContain('qdDetailOpen=1');
    expect(rootLink?.getAttribute('target')).toBeNull();
    expect(rootLink?.getAttribute('rel')).toBeNull();
  });

  it('renders root, lemma, and stem detail-overlay links over the Mushaf base when morphology includes ids', () => {
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

    expect(rootLink?.getAttribute('href')).toContain(
      `qdDetail=${encodeURIComponent('v1~root~999~words~simple~mentioned~1')}`,
    );
    expect(rootLink?.getAttribute('target')).toBeNull();
    expect(rootLink?.getAttribute('rel')).toBeNull();

    expect(lemmaLink?.getAttribute('href')).toContain(
      `qdDetail=${encodeURIComponent('v1~lemma~555~words~simple~mentioned~1~-')}`,
    );
    expect(lemmaLink?.getAttribute('href')).toContain('qdDetailOpen=1');
    expect(lemmaLink?.getAttribute('target')).toBeNull();
    expect(lemmaLink?.getAttribute('rel')).toBeNull();

    expect(stemLink?.getAttribute('href')).toContain(
      `qdDetail=${encodeURIComponent('v1~stem~777~words~simple~mentioned~1~-')}`,
    );
    expect(stemLink?.getAttribute('href')).toContain('qdDetailOpen=1');
    expect(stemLink?.getAttribute('target')).toBeNull();
    expect(stemLink?.getAttribute('rel')).toBeNull();
  });

  it('renders the type label as a word-type detail-overlay link for a verb with a tense', () => {
    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    setInputs(fixture, {
      analysis: buildWordAnalysisViewModel(),
      loadState: IDLE,
      selectedWordLocation: '2:25:3',
    });

    const root = fixture.nativeElement as HTMLElement;
    const typeLink = root.querySelector(
      '[data-testid="word-morphology-type-link"]',
    ) as HTMLAnchorElement | null;

    const href = typeLink?.getAttribute('href') ?? '';
    expect(href).toContain('/dashboard/mushaf');
    expect(href).toContain(`qdDetail=${encodeURIComponent('v1~wordType~101~past~all~all~all~ayahs~1')}`);
    expect(href).toContain('qdDetailOpen=1');
    expect(typeLink?.getAttribute('target')).toBeNull();
    expect(typeLink?.getAttribute('rel')).toBeNull();
    expect(typeLink?.textContent).toContain('فعل');
  });

  it('derives the word-type context from head POS for a non-verb', () => {
    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    const analysis = buildWordAnalysisViewModel();
    analysis.morphology = {
      ...analysis.morphology,
      headPos: 'N',
      headPosLabel: { ar: 'اسم', en: 'Noun' },
      isVerb: false,
      verbTense: null,
      verbVoice: null,
    };
    setInputs(fixture, {
      analysis,
      loadState: IDLE,
      selectedWordLocation: '2:25:3',
    });

    const typeLink = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-testid="word-morphology-type-link"]',
    ) as HTMLAnchorElement | null;

    expect(typeLink?.getAttribute('href')).toContain(
      `qdDetail=${encodeURIComponent('v1~wordType~101~N~all~all~all~ayahs~1')}`,
    );
  });

  it('keeps the type column as plain text when no word-type identity can be derived', () => {
    const fixture = TestBed.createComponent(SelectedWordSectionComponent);
    const analysis = buildWordAnalysisViewModel();
    analysis.morphology = {
      ...analysis.morphology,
      headPos: '',
      headPosLabel: { ar: 'اسم', en: 'Noun' },
      isVerb: false,
      verbTense: null,
      verbVoice: null,
    };
    setInputs(fixture, {
      analysis,
      loadState: IDLE,
      selectedWordLocation: '2:25:3',
    });

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="word-morphology-type-link"]')).toBeNull();
    const summary = root.querySelector('[data-testid="word-morphology-summary"]');
    expect(summary?.textContent).toContain('نوع الكلمة');
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

  it('renders both unique-word identity rows as detail-overlay links using their nested ids', () => {
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

    const tashkeelHref = tashkeelLink?.getAttribute('href') ?? '';
    expect(tashkeelHref).toContain('/dashboard/mushaf');
    expect(tashkeelHref).toContain('page=5');
    expect(tashkeelHref).toContain(`qdDetail=${encodeURIComponent('v1~unique~tashkeel~101~ayahs~1')}`);
    expect(tashkeelHref).toContain('qdDetailOpen=1');
    expect(tashkeelLink?.getAttribute('target')).toBeNull();
    expect(tashkeelLink?.getAttribute('rel')).toBeNull();

    const simpleHref = simpleLink?.getAttribute('href') ?? '';
    expect(simpleHref).toContain(`qdDetail=${encodeURIComponent('v1~unique~simple~202~ayahs~1')}`);
    expect(simpleHref).toContain('qdDetailOpen=1');
    expect(simpleLink?.getAttribute('target')).toBeNull();
    expect(simpleLink?.getAttribute('rel')).toBeNull();
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

  describe('loading layout reservation (U1, Feature 029)', () => {
    class FakeResizeObserver {
      static instances: FakeResizeObserver[] = [];

      private observed: Element[] = [];

      constructor(private readonly callback: ResizeObserverCallback) {
        FakeResizeObserver.instances.push(this);
      }

      observe(element: Element): void {
        this.observed.push(element);
      }

      unobserve(element: Element): void {
        this.observed = this.observed.filter((observedElement) => observedElement !== element);
      }

      disconnect(): void {
        this.observed = [];
      }

      trigger(width: number, height: number): void {
        const target = this.observed[this.observed.length - 1];
        if (!target) {
          throw new Error('FakeResizeObserver.trigger called before observe');
        }
        vi.spyOn(target, 'getBoundingClientRect').mockReturnValue({
          width,
          height,
          top: 0,
          left: 0,
          right: width,
          bottom: height,
          x: 0,
          y: 0,
          toJSON: () => ({}),
        } as DOMRect);
        this.callback([{ target } as unknown as ResizeObserverEntry], this as unknown as ResizeObserver);
      }
    }

    beforeEach(() => {
      FakeResizeObserver.instances = [];
      vi.stubGlobal('ResizeObserver', FakeResizeObserver);
    });

    afterEach(() => {
      vi.unstubAllGlobals();
    });

    function lastObserver(): FakeResizeObserver {
      const observer = FakeResizeObserver.instances[FakeResizeObserver.instances.length - 1];
      expect(observer).toBeTruthy();
      return observer;
    }

    function sectionOf(fixture: ComponentFixture<SelectedWordSectionComponent>): HTMLElement {
      return fixture.nativeElement.querySelector('[data-testid="selected-word-section"]') as HTMLElement;
    }

    it('reserves the last successful natural block size while loading, with no stale loaded DOM', () => {
      const fixture = TestBed.createComponent(SelectedWordSectionComponent);
      setInputs(fixture, {
        analysis: buildWordAnalysisViewModel(),
        loadState: IDLE,
        selectedWordLocation: '2:25:3',
      });
      lastObserver().trigger(600, 420);
      fixture.detectChanges();

      setInputs(fixture, {
        analysis: null,
        loadState: { isLoading: true, isEmpty: false, errorMessage: null },
        selectedWordLocation: '2:25:4',
      });

      const section = sectionOf(fixture);
      expect(section.classList.contains('selected-word-section--loading')).toBe(true);
      expect(section.style.getPropertyValue('--qd-selected-word-reserved-block-size')).toBe('420px');

      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('qd-segment-rendered-word')).toBeNull();
      expect(root.querySelector('qd-segment-data-rows')).toBeNull();
      expect(root.querySelector('[data-testid="word-identity-summary"]')).toBeNull();
      expect(root.textContent).not.toContain(WORD_TEXT_PLACEHOLDER);
    });

    it('clears the loading reservation on the next successful render and updates the segment-count metadata', () => {
      const fixture = TestBed.createComponent(SelectedWordSectionComponent);
      setInputs(fixture, {
        analysis: buildWordAnalysisViewModel(),
        loadState: IDLE,
        selectedWordLocation: '2:25:3',
      });
      lastObserver().trigger(600, 420);
      fixture.detectChanges();

      const twoSegments = buildWordAnalysisViewModel();
      twoSegments.segments = [
        twoSegments.segments[0],
        { ...twoSegments.segments[0], segmentNumber: 2, segmentLocation: '2:25:3:2', segmentColorSlot: 2 },
      ];
      setInputs(fixture, {
        analysis: twoSegments,
        loadState: IDLE,
        selectedWordLocation: '2:25:4',
      });

      const section = sectionOf(fixture);
      expect(section.classList.contains('selected-word-section--loading')).toBe(false);
      expect(section.style.getPropertyValue('--qd-selected-word-reserved-block-size')).toBe('');

      setInputs(fixture, {
        analysis: null,
        loadState: { isLoading: true, isEmpty: false, errorMessage: null },
        selectedWordLocation: '2:25:5',
      });
      expect(fixture.nativeElement.querySelectorAll('.selected-word-section__segment-skeleton')).toHaveLength(2);
    });

    it('drops a stale reservation when the available width changes mid-loading', () => {
      const fixture = TestBed.createComponent(SelectedWordSectionComponent);
      setInputs(fixture, {
        analysis: buildWordAnalysisViewModel(),
        loadState: IDLE,
        selectedWordLocation: '2:25:3',
      });
      lastObserver().trigger(600, 420);
      fixture.detectChanges();

      setInputs(fixture, {
        analysis: null,
        loadState: { isLoading: true, isEmpty: false, errorMessage: null },
        selectedWordLocation: '2:25:4',
      });
      expect(sectionOf(fixture).style.getPropertyValue('--qd-selected-word-reserved-block-size')).toBe('420px');

      lastObserver().trigger(300, 480);
      fixture.detectChanges();

      expect(sectionOf(fixture).style.getPropertyValue('--qd-selected-word-reserved-block-size')).toBe('');
      expect(sectionOf(fixture).classList.contains('selected-word-section--loading')).toBe(true);
    });

    it('keeps the first-load fallback of three segment placeholders before any successful render', () => {
      const fixture = TestBed.createComponent(SelectedWordSectionComponent);
      setInputs(fixture, {
        analysis: null,
        loadState: { isLoading: true, isEmpty: false, errorMessage: null },
        selectedWordLocation: '2:25:3',
      });

      expect(fixture.nativeElement.querySelectorAll('.selected-word-section__segment-skeleton')).toHaveLength(3);
      expect(sectionOf(fixture).style.getPropertyValue('--qd-selected-word-reserved-block-size')).toBe('');
    });
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
