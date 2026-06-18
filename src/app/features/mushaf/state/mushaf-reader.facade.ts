import { Injectable, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';

import { MushafAyahStudyApi } from '../data-access/mushaf-ayah-study.api';
import { MushafPagesApi } from '../data-access/mushaf-pages.api';
import { MushafSurahCatalogApi } from '../data-access/mushaf-surah-catalog.api';
import { MushafWordAnalysisApi } from '../data-access/mushaf-word-analysis.api';
import {
  AyahStudyDto,
  AyahStudyTab,
  AyahStudyViewModel,
  DEFAULT_MUSHAF_READER_STATE,
  MUSHAF_URL_KEYS,
  MushafPageDto,
  MushafPageViewModel,
  MushafReaderState,
  MushafSurahCatalogItemDto,
  PanelMode,
  ResourceLoadState,
  SourceOption,
  WordAnalysisDto,
  WordAnalysisTab,
  WordAnalysisViewModel,
} from '../models/mushaf.models';
import { subscribeToApiLoad } from './mushaf-api-load.helpers';
import { MushafReaderCache, MushafReaderCacheKeys } from './mushaf-reader-cache';
import { segmentSlotToColor } from './segment-color-palette';
import {
  MushafUrlSnapshot,
  buildUrlEnumCorrections,
  parseMushafUrlParams,
} from './mushaf-url-sync';
import { applyAuthoritativeUrlSnapshot } from './mushaf-url-hydration';

function toPageViewModel(dto: MushafPageDto): MushafPageViewModel {
  return {
    pageNumber: dto.pageNumber,
    previousPageNumber: dto.previousPageNumber,
    nextPageNumber: dto.nextPageNumber,
    surahs: dto.surahs,
    ayahRange: dto.ayahRange,
    navigation: dto.navigation,
    lines: dto.lines,
    markers: dto.markers,
  };
}

function toAyahStudyViewModel(dto: AyahStudyDto): AyahStudyViewModel {
  return {
    ayah: dto.ayah,
    selectedSources: dto.selectedSources,
    tafsir: dto.tafsir,
    translation: dto.translation,
    fullI3rab: dto.fullI3rab,
  };
}

function toWordAnalysisViewModel(dto: WordAnalysisDto): WordAnalysisViewModel {
  return {
    word: dto.word,
    identity: dto.identity,
    morphology: dto.morphology,
    segments: dto.renderedWordSegments.map((segment) => ({
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

/** v1 source options exposed in the study-area selectors. */
const TAFSIR_SOURCE_OPTIONS: SourceOption[] = [
  { key: 'ar-muyassar', label: 'التفسير الميسر' },
];
const TRANSLATION_SOURCE_OPTIONS: SourceOption[] = [
  { key: 'en-sahih-international', label: 'صحيح إنترناشونال' },
];
const FULL_I3RAB_SOURCE_OPTIONS: SourceOption[] = [
  { key: 'muyassar', label: 'الإعراب الميسر' },
];

/**
 * Mushaf reader page-state facade.
 *
 * Owns all reader view state (selections, sources, tabs, per-resource
 * loading/empty/error primitives) and URL ↔ state synchronization.
 */
@Injectable({ providedIn: 'root' })
export class MushafReaderFacade {
  private readonly pagesApi = inject(MushafPagesApi);
  private readonly ayahStudyApi = inject(MushafAyahStudyApi);
  private readonly surahCatalogApi = inject(MushafSurahCatalogApi);
  private readonly wordAnalysisApi = inject(MushafWordAnalysisApi);
  private readonly readerCache = inject(MushafReaderCache);
  private readonly router = inject(Router);

  private activeRoute: ActivatedRoute | null = null;
  private routeSubscription: Subscription | null = null;

  private readonly _pageNumber = signal(DEFAULT_MUSHAF_READER_STATE.pageNumber);
  private readonly _selectedAyahKey = signal(DEFAULT_MUSHAF_READER_STATE.selectedAyahKey);
  private readonly _selectedWordLocation = signal(DEFAULT_MUSHAF_READER_STATE.selectedWordLocation);
  private readonly _selectedSegmentLocation = signal(DEFAULT_MUSHAF_READER_STATE.selectedSegmentLocation);
  private readonly _panel = signal(DEFAULT_MUSHAF_READER_STATE.panel);
  private readonly _ayahTab = signal(DEFAULT_MUSHAF_READER_STATE.ayahTab);
  private readonly _wordTab = signal(DEFAULT_MUSHAF_READER_STATE.wordTab);
  private readonly _sources = signal(DEFAULT_MUSHAF_READER_STATE.sources);
  private readonly _urlExplicitSources = signal(DEFAULT_MUSHAF_READER_STATE.sources);

  private readonly _page = signal<MushafPageViewModel | null>(null);
  private readonly _ayahStudy = signal<AyahStudyViewModel | null>(null);
  private readonly _wordAnalysis = signal<WordAnalysisViewModel | null>(null);
  private readonly _surahCatalog = signal<MushafSurahCatalogItemDto[]>([]);

  private readonly _pageLoadState = signal<ResourceLoadState>(DEFAULT_MUSHAF_READER_STATE.page);
  private readonly _ayahStudyLoadState = signal<ResourceLoadState>(DEFAULT_MUSHAF_READER_STATE.ayahStudy);
  private readonly _wordAnalysisLoadState = signal<ResourceLoadState>(DEFAULT_MUSHAF_READER_STATE.wordAnalysis);

  readonly pageNumber = this._pageNumber.asReadonly();
  readonly selectedAyahKey = this._selectedAyahKey.asReadonly();
  readonly selectedWordLocation = this._selectedWordLocation.asReadonly();
  readonly selectedSegmentLocation = this._selectedSegmentLocation.asReadonly();
  readonly panel = this._panel.asReadonly();
  readonly ayahTab = this._ayahTab.asReadonly();
  readonly wordTab = this._wordTab.asReadonly();
  readonly sources = this._sources.asReadonly();

  readonly page = this._page.asReadonly();
  readonly ayahStudy = this._ayahStudy.asReadonly();
  readonly wordAnalysis = this._wordAnalysis.asReadonly();
  readonly surahCatalog = this._surahCatalog.asReadonly();

  readonly pageLoadState = this._pageLoadState.asReadonly();
  readonly ayahStudyLoadState = this._ayahStudyLoadState.asReadonly();
  readonly wordAnalysisLoadState = this._wordAnalysisLoadState.asReadonly();

  readonly tafsirSourceOptions = TAFSIR_SOURCE_OPTIONS;
  readonly translationSourceOptions = TRANSLATION_SOURCE_OPTIONS;
  readonly fullI3rabSourceOptions = FULL_I3RAB_SOURCE_OPTIONS;

  readonly state = computed<MushafReaderState>(() => ({
    pageNumber: this._pageNumber(),
    selectedAyahKey: this._selectedAyahKey(),
    selectedWordLocation: this._selectedWordLocation(),
    selectedSegmentLocation: this._selectedSegmentLocation(),
    panel: this._panel(),
    ayahTab: this._ayahTab(),
    wordTab: this._wordTab(),
    sources: this._sources(),
    page: this._pageLoadState(),
    ayahStudy: this._ayahStudyLoadState(),
    wordAnalysis: this._wordAnalysisLoadState(),
  }));

  /** Subscribes to query params and hydrates state for deep links (page → ayah → word). */
  bindToRoute(route: ActivatedRoute): void {
    this.activeRoute = route;
    this.routeSubscription?.unsubscribe();
    this.routeSubscription = route.queryParamMap.subscribe((params) => {
      const snapshot = parseMushafUrlParams(params);
      this.hydrateFromUrl(snapshot);

      const corrections = buildUrlEnumCorrections(params, snapshot);
      if (Object.keys(corrections).length > 0) {
        this.patchUrlQuery(corrections);
      }
    });
  }

  changePage(pageNumber: number): void {
    this.patchUrlQuery({ [MUSHAF_URL_KEYS.page]: pageNumber });
  }

  jumpToSurah(surahNumber: number): void {
    const startPage = this.resolveSurahStartPage(surahNumber);
    if (startPage !== null) {
      this.changePage(startPage);
    }
  }

  selectAyah(verseKey: string): void {
    this.patchUrlQuery({
      [MUSHAF_URL_KEYS.ayah]: verseKey,
      [MUSHAF_URL_KEYS.panel]: 'ayah',
    });
  }

  selectWord(wordLocation: string): void {
    this.patchUrlQuery({
      [MUSHAF_URL_KEYS.word]: wordLocation,
      [MUSHAF_URL_KEYS.panel]: 'word',
    });
  }

  setPanel(panel: PanelMode): void {
    this.patchUrlQuery({
      [MUSHAF_URL_KEYS.panel]: panel === DEFAULT_MUSHAF_READER_STATE.panel ? null : panel,
    });
  }

  setAyahTab(tab: AyahStudyTab): void {
    this.patchUrlQuery({ [MUSHAF_URL_KEYS.ayahTab]: tab });
  }

  setWordTab(tab: WordAnalysisTab): void {
    this.patchUrlQuery({ [MUSHAF_URL_KEYS.wordTab]: tab });
  }

  setTafsirSource(sourceKey: string): void {
    this.patchUrlQuery({ [MUSHAF_URL_KEYS.tafsirSource]: sourceKey });
  }

  setTranslationSource(sourceKey: string): void {
    this.patchUrlQuery({ [MUSHAF_URL_KEYS.translationSource]: sourceKey });
  }

  setFullI3rabSource(sourceKey: string): void {
    this.patchUrlQuery({ [MUSHAF_URL_KEYS.fullI3rabSource]: sourceKey });
  }

  loadSurahCatalog(): void {
    subscribeToApiLoad(this.surahCatalogApi.getCatalog(), {
      onSuccess: (data) => this._surahCatalog.set(data.surahs),
      onSettled: () => undefined,
      emptyMessage: 'تعذّر تحميل فهرس السور.',
      notFoundMessage: 'تعذّر تحميل فهرس السور.',
      connectionMessage: 'تعذّر الاتصال بالخادم.',
    });
  }

  resolveSurahStartPage(surahNumber: number): number | null {
    const entry = this._surahCatalog().find((surah) => surah.surahNumber === surahNumber);
    return entry?.startPageNumber ?? null;
  }

  loadPage(pageNumber: number): void {
    const clamped = Math.min(604, Math.max(1, pageNumber));
    const pageAlreadyRendered = clamped === this._pageNumber() && this._page() !== null;
    if (pageAlreadyRendered) {
      return;
    }

    this._pageNumber.set(clamped);
    this._pageLoadState.set({ isLoading: true, isEmpty: false, errorMessage: null });

    subscribeToApiLoad(
      this.readerCache.getOrLoad(MushafReaderCacheKeys.page(clamped), () => this.pagesApi.getPage(clamped)),
      {
      onSuccess: (data) => {
        this._page.set(toPageViewModel(data));
        this.prefetchAdjacentPages(data.previousPageNumber, data.nextPageNumber);
      },
      onSettled: (loadState) => {
        if (loadState.isEmpty) {
          this._page.set(null);
        }
        this._pageLoadState.set(loadState);
      },
      emptyMessage: 'تعذّر تحميل الصفحة.',
      notFoundMessage: 'الصفحة غير موجودة.',
      connectionMessage: 'تعذّر الاتصال بالخادم.',
    });
  }

  loadAyahStudy(verseKey: string): void {
    this._selectedAyahKey.set(verseKey);
    this._ayahStudyLoadState.set({ isLoading: true, isEmpty: false, errorMessage: null });

    const sources = this._urlExplicitSources();
    const cacheKey = MushafReaderCacheKeys.ayahStudy(verseKey, sources);
    subscribeToApiLoad(
      this.readerCache.getOrLoad(cacheKey, () => this.ayahStudyApi.getAyahStudy(verseKey, sources)),
      {
        onSuccess: (data) => {
          this._ayahStudy.set(toAyahStudyViewModel(data));
          this._sources.set({
            tafsirSource: data.selectedSources.tafsirSource,
            translationSource: data.selectedSources.translationSource,
            fullI3rabSource: data.selectedSources.fullI3rabSource,
          });
        },
        onSettled: (loadState) => {
          if (loadState.isEmpty) {
            this._ayahStudy.set(null);
          }
          this._ayahStudyLoadState.set(loadState);
        },
        emptyMessage: 'تعذّر تحميل دراسة الآية.',
        notFoundMessage: 'الآية غير موجودة.',
        connectionMessage: 'تعذّر الاتصال بالخادم.',
      },
    );
  }

  loadWordAnalysis(wordLocation: string): void {
    if (this.isAyahMarkerOnCurrentPage(wordLocation)) {
      this._selectedWordLocation.set(wordLocation);
      this._wordAnalysis.set(null);
      this._wordAnalysisLoadState.set({
        isLoading: false,
        isEmpty: false,
        errorMessage: 'هذه الكلمة غير قابلة للتحليل (علامة نهاية آية)',
      });
      return;
    }

    this._selectedWordLocation.set(wordLocation);
    this._wordAnalysisLoadState.set({ isLoading: true, isEmpty: false, errorMessage: null });

    subscribeToApiLoad(
      this.readerCache.getOrLoad(
        MushafReaderCacheKeys.wordAnalysis(wordLocation),
        () => this.wordAnalysisApi.getWordAnalysis(wordLocation),
      ),
      {
      onSuccess: (data) => this._wordAnalysis.set(toWordAnalysisViewModel(data)),
      onSettled: (loadState) => {
        if (loadState.isEmpty) {
          this._wordAnalysis.set(null);
        }
        this._wordAnalysisLoadState.set(loadState);
      },
      emptyMessage: 'تعذّر تحميل تحليل الكلمة.',
      notFoundMessage: 'الكلمة غير موجودة.',
      connectionMessage: 'تعذّر الاتصال بالخادم.',
    });
  }

  private hydrateFromUrl(snapshot: MushafUrlSnapshot): void {
    this.loadPage(snapshot.pageNumber);
    this.applyUrlState(snapshot);
  }

  private patchUrlQuery(
    params: Partial<Record<(typeof MUSHAF_URL_KEYS)[keyof typeof MUSHAF_URL_KEYS], string | number | null>>,
  ): void {
    if (!this.activeRoute) {
      return;
    }

    void this.router.navigate([], {
      relativeTo: this.activeRoute,
      queryParams: params,
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  /** Applies an authoritative URL snapshot (used by route hydration and tests). */
  applyUrlState(snapshot: Pick<MushafUrlSnapshot, 'panel' | 'ayah' | 'word' | 'segment' | 'ayahTab' | 'wordTab' | 'sources'>): void {
    applyAuthoritativeUrlSnapshot(
      snapshot,
      {
        selectedAyahKey: this._selectedAyahKey(),
        selectedWordLocation: this._selectedWordLocation(),
        urlExplicitSources: this._urlExplicitSources(),
      },
      {
        setUiState: (panel, ayahTab, wordTab, segmentLocation) => {
          this._panel.set(panel);
          this._ayahTab.set(ayahTab);
          this._wordTab.set(wordTab);
          this._selectedSegmentLocation.set(segmentLocation);
        },
        clearWordSelection: () => {
          this._selectedWordLocation.set(null);
          this._selectedSegmentLocation.set(null);
          this._wordAnalysis.set(null);
          this._wordAnalysisLoadState.set({ isLoading: false, isEmpty: false, errorMessage: null });
        },
        setWord: (wordLocation, reload) => {
          if (reload) {
            this.loadWordAnalysis(wordLocation);
          } else {
            this._selectedWordLocation.set(wordLocation);
          }
        },
        setUrlExplicitSources: (sources) => this._urlExplicitSources.set(sources),
        clearAyahSelection: () => {
          this._selectedAyahKey.set(null);
          this._ayahStudy.set(null);
          this._ayahStudyLoadState.set({ isLoading: false, isEmpty: false, errorMessage: null });
        },
        setAyah: (verseKey, reload) => {
          this._selectedAyahKey.set(verseKey);
          if (reload) {
            this.loadAyahStudy(verseKey);
          }
        },
      },
    );
  }

  private prefetchAdjacentPages(previousPageNumber: number | null, nextPageNumber: number | null): void {
    if (previousPageNumber !== null) {
      this.readerCache.prefetch(
        MushafReaderCacheKeys.page(previousPageNumber),
        () => this.pagesApi.getPage(previousPageNumber),
      );
    }

    if (nextPageNumber !== null) {
      this.readerCache.prefetch(
        MushafReaderCacheKeys.page(nextPageNumber),
        () => this.pagesApi.getPage(nextPageNumber),
      );
    }
  }

  private isAyahMarkerOnCurrentPage(wordLocation: string): boolean {
    const page = this._page();
    if (!page) {
      return false;
    }

    for (const line of page.lines) {
      const word = line.words.find((candidate) => candidate.wordLocation === wordLocation);
      if (word) {
        return word.isAyahMarker;
      }
    }

    return false;
  }
}
