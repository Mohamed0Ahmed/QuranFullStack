import { Injectable, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';

import { MushafAyahStudyApi } from '../data-access/mushaf-ayah-study.api';
import { MushafPagesApi } from '../data-access/mushaf-pages.api';
import { MushafSimilarAyahsApi } from '../data-access/mushaf-similar-ayahs.api';
import { MushafAyahMutashabihatApi } from '../data-access/mushaf-ayah-mutashabihat.api';
import { MushafStudySourceCatalogApi } from '../data-access/mushaf-study-sources.api';
import { MushafWordAnalysisApi } from '../data-access/mushaf-word-analysis.api';
import {
  MUSHAF_SURAH_JUZ_GROUPS,
  resolveMushafSurahStartPage,
} from '../data/mushaf-surah-juz-catalog';
import {
  AyahStudyTab,
  AyahStudyViewModel,
  DEFAULT_MUSHAF_READER_STATE,
  MUSHAF_URL_KEYS,
  MushafPageViewModel,
  MushafReaderState,
  MushafSurahJuzGroupDto,
  PanelMode,
  ResourceLoadState,
  SimilarAyahsDto,
  AyahMutashabihatDto,
  SourceOption,
  WordAnalysisTab,
  WordAnalysisViewModel,
} from '../models/mushaf.models';
import { subscribeToApiLoad } from './mushaf-api-load.helpers';
import { AyahStudyLoadRunner } from './mushaf-ayah-study-load.runner';
import { MushafReaderCache, MushafReaderCacheKeys } from './mushaf-reader-cache';
import { prefetchAdjacentMushafPages } from './mushaf-reader-page.helpers';
import {
  isBareMushafEntry,
  loadMushafReaderSession,
  mushafSnapshotToQueryParams,
  saveMushafReaderSession,
} from './mushaf-reader-session';
import { toPageViewModel } from './mushaf-reader-view-mappers';
import { applyAuthoritativeUrlSnapshot } from './mushaf-url-hydration';
import { verseKeyFromWordLocation } from '../utils/mushaf-location-keys';
import {
  fullI3rabCatalogItemToOption,
  tafsirCatalogItemToOption,
  translationCatalogItemToOption,
} from '../utils/study-source-catalog.labels';
import {
  MushafUrlSnapshot,
  buildUrlEnumCorrections,
  parseMushafUrlParams,
} from './mushaf-url-sync';
import { SimilarAyahsLoadRunner } from './mushaf-similar-ayahs-load.runner';
import { MutashabihatLoadRunner } from './mushaf-mutashabihat-load.runner';
import { WordAnalysisLoadRunner } from './mushaf-word-analysis-load.runner';

const PEEK_FLASH_CLEAR_MS = 3000;

@Injectable({ providedIn: 'root' })
export class MushafReaderFacade {
  private readonly pagesApi = inject(MushafPagesApi);
  private readonly ayahStudyApi = inject(MushafAyahStudyApi);
  private readonly similarAyahsApi = inject(MushafSimilarAyahsApi);
  private readonly mutashabihatApi = inject(MushafAyahMutashabihatApi);
  private readonly studySourceCatalogApi = inject(MushafStudySourceCatalogApi);
  private readonly wordAnalysisApi = inject(MushafWordAnalysisApi);
  private readonly readerCache = inject(MushafReaderCache);
  private readonly router = inject(Router);

  private activeRoute: ActivatedRoute | null = null;
  private routeSubscription: Subscription | null = null;

  private readonly _pageNumber = signal(DEFAULT_MUSHAF_READER_STATE.pageNumber);
  private readonly _selectedAyahKey = signal(DEFAULT_MUSHAF_READER_STATE.selectedAyahKey);
  private readonly _focusAyahKey = signal<string | null>(null);
  private readonly _selectedWordLocation = signal(DEFAULT_MUSHAF_READER_STATE.selectedWordLocation);
  private readonly _selectedSegmentLocation = signal(
    DEFAULT_MUSHAF_READER_STATE.selectedSegmentLocation,
  );
  private readonly _panel = signal(DEFAULT_MUSHAF_READER_STATE.panel);
  private readonly _ayahTab = signal(DEFAULT_MUSHAF_READER_STATE.ayahTab);
  private readonly _wordTab = signal(DEFAULT_MUSHAF_READER_STATE.wordTab);
  private readonly _sources = signal(DEFAULT_MUSHAF_READER_STATE.sources);
  private readonly _urlExplicitSources = signal(DEFAULT_MUSHAF_READER_STATE.sources);

  private readonly _page = signal<MushafPageViewModel | null>(null);
  private readonly _ayahStudy = signal<AyahStudyViewModel | null>(null);
  private readonly _similarAyahs = signal<SimilarAyahsDto | null>(null);
  private readonly _mutashabihat = signal<AyahMutashabihatDto | null>(null);
  private readonly _wordAnalysis = signal<WordAnalysisViewModel | null>(null);
  private readonly _surahCatalogByJuz =
    signal<readonly MushafSurahJuzGroupDto[]>(MUSHAF_SURAH_JUZ_GROUPS);
  private readonly _tafsirSourceOptions = signal<SourceOption[]>([]);
  private readonly _translationSourceOptions = signal<SourceOption[]>([]);
  private readonly _fullI3rabSourceOptions = signal<SourceOption[]>([]);

  private wordAnalysisRequestToken = 0;
  private ayahStudyRequestToken = 0;
  private similarAyahsRequestToken = 0;
  private mutashabihatRequestToken = 0;
  private peekFlashClearTimer: ReturnType<typeof setTimeout> | null = null;

  /**
   * F2: guards `loadStudySourceCatalog` so it fires GET /api/mushaf/study-sources at
   * most once per successful load. `loaded` flips only on a genuine success (even an
   * empty-but-successful catalogue counts) so it is distinguishable from "not loaded
   * yet"; a failure never sets `loaded`, so the next mount can retry.
   */
  private studySourceCatalogLoaded = false;
  private studySourceCatalogLoading = false;

  /**
   * F1: set true by {@link bindToRoute} and consumed by the first URL hydration after a
   * (re)bind. Only that first hydration may treat a still-loading ayah-study/word-analysis
   * resource as a stranded load to recover (reload). Later in-place URL patches on the same
   * binding (e.g. switching a study tab while a request is in flight) must NOT restart the
   * in-flight request or re-arm its debounce.
   */
  private rebindRecoveryPending = false;

  private readonly wordAnalysisLoadRunner = new WordAnalysisLoadRunner({
    getPage: () => this._page(),
    setAnalysis: (value) => this._wordAnalysis.set(value),
    setLoadState: (state) => this._wordAnalysisLoadState.set(state),
    bumpRequestToken: () => ++this.wordAnalysisRequestToken,
    getRequestToken: () => this.wordAnalysisRequestToken,
    wordAnalysisApi: this.wordAnalysisApi,
    readerCache: this.readerCache,
  });

  private readonly ayahStudyLoadRunner = new AyahStudyLoadRunner({
    getUrlExplicitSources: () => this._urlExplicitSources(),
    setAyahStudy: (value) => this._ayahStudy.set(value),
    setSources: (sources) => this._sources.set(sources),
    setLoadState: (state) => this._ayahStudyLoadState.set(state),
    bumpRequestToken: () => ++this.ayahStudyRequestToken,
    getRequestToken: () => this.ayahStudyRequestToken,
    ayahStudyApi: this.ayahStudyApi,
    readerCache: this.readerCache,
  });

  private readonly similarAyahsLoadRunner = new SimilarAyahsLoadRunner({
    setSimilarAyahs: (value) => this._similarAyahs.set(value),
    setLoadState: (state) => this._similarAyahsLoadState.set(state),
    bumpRequestToken: () => ++this.similarAyahsRequestToken,
    getRequestToken: () => this.similarAyahsRequestToken,
    similarAyahsApi: this.similarAyahsApi,
    readerCache: this.readerCache,
  });

  private readonly mutashabihatLoadRunner = new MutashabihatLoadRunner({
    setMutashabihat: (value) => this._mutashabihat.set(value),
    setLoadState: (state) => this._mutashabihatLoadState.set(state),
    bumpRequestToken: () => ++this.mutashabihatRequestToken,
    getRequestToken: () => this.mutashabihatRequestToken,
    mutashabihatApi: this.mutashabihatApi,
    readerCache: this.readerCache,
  });

  private readonly _pageLoadState = signal<ResourceLoadState>(DEFAULT_MUSHAF_READER_STATE.page);
  private readonly _ayahStudyLoadState = signal<ResourceLoadState>(
    DEFAULT_MUSHAF_READER_STATE.ayahStudy,
  );
  private readonly _similarAyahsLoadState = signal<ResourceLoadState>(
    DEFAULT_MUSHAF_READER_STATE.similarAyahs,
  );
  private readonly _mutashabihatLoadState = signal<ResourceLoadState>(
    DEFAULT_MUSHAF_READER_STATE.mutashabihat,
  );
  private readonly _wordAnalysisLoadState = signal<ResourceLoadState>(
    DEFAULT_MUSHAF_READER_STATE.wordAnalysis,
  );

  readonly pageNumber = this._pageNumber.asReadonly();
  readonly selectedAyahKey = this._selectedAyahKey.asReadonly();
  readonly focusAyahKey = this._focusAyahKey.asReadonly();
  readonly selectedWordLocation = this._selectedWordLocation.asReadonly();
  readonly selectedSegmentLocation = this._selectedSegmentLocation.asReadonly();
  readonly panel = this._panel.asReadonly();
  readonly ayahTab = this._ayahTab.asReadonly();
  readonly wordTab = this._wordTab.asReadonly();
  readonly sources = this._sources.asReadonly();

  readonly page = this._page.asReadonly();
  readonly ayahStudy = this._ayahStudy.asReadonly();
  readonly similarAyahs = this._similarAyahs.asReadonly();
  readonly mutashabihat = this._mutashabihat.asReadonly();
  readonly wordAnalysis = this._wordAnalysis.asReadonly();
  readonly surahCatalogByJuz = this._surahCatalogByJuz.asReadonly();
  readonly tafsirSourceOptions = this._tafsirSourceOptions.asReadonly();
  readonly translationSourceOptions = this._translationSourceOptions.asReadonly();
  readonly fullI3rabSourceOptions = this._fullI3rabSourceOptions.asReadonly();

  readonly pageLoadState = this._pageLoadState.asReadonly();
  readonly ayahStudyLoadState = this._ayahStudyLoadState.asReadonly();
  readonly similarAyahsLoadState = this._similarAyahsLoadState.asReadonly();
  readonly mutashabihatLoadState = this._mutashabihatLoadState.asReadonly();
  readonly wordAnalysisLoadState = this._wordAnalysisLoadState.asReadonly();

  readonly mushafHighlightVerseKey = computed(() => this._focusAyahKey());

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
    similarAyahs: this._similarAyahsLoadState(),
    mutashabihat: this._mutashabihatLoadState(),
  }));

  bindToRoute(route: ActivatedRoute): void {
    this.activeRoute = route;
    this.rebindRecoveryPending = true;
    this.routeSubscription?.unsubscribe();
    this.routeSubscription = route.queryParamMap.subscribe((params) => {
      if (isBareMushafEntry(params)) {
        const saved = loadMushafReaderSession();
        if (saved) {
          const restoredParams = mushafSnapshotToQueryParams(saved);
          if (Object.keys(restoredParams).length > 0) {
            void this.router.navigate([], {
              relativeTo: route,
              queryParams: restoredParams,
              replaceUrl: true,
            });
            return;
          }
        }
      }

      const snapshot = parseMushafUrlParams(params);
      this.hydrateFromUrl(snapshot);
      saveMushafReaderSession(snapshot);

      const corrections = buildUrlEnumCorrections(params, snapshot);
      if (Object.keys(corrections).length > 0) {
        this.patchUrlQuery(corrections);
      }
    });
  }

  unbindFromRoute(): void {
    this.cancelPeekFlashClearTimer();
    this.wordAnalysisLoadRunner.clearPending();
    this.ayahStudyLoadRunner.clearPending();
    this.similarAyahsLoadRunner.clearPending();
    this.mutashabihatLoadRunner.clearPending();
    this.routeSubscription?.unsubscribe();
    this.routeSubscription = null;
    this.activeRoute = null;
  }

  changePage(pageNumber: number): void {
    this.cancelPeekFlashClearTimer();
    this.patchUrlQuery({
      [MUSHAF_URL_KEYS.page]: pageNumber,
      [MUSHAF_URL_KEYS.focusAyah]: null,
    });
  }

  jumpToSurah(surahNumber: number): void {
    const startPage = this.resolveSurahStartPage(surahNumber);
    if (startPage !== null) {
      this.changePage(startPage);
    }
  }

  selectAyah(verseKey: string): void {
    this.patchAyahSelectionQuery(verseKey);
  }

  viewAyahOnPage(verseKey: string, pageNumber: number): void {
    this.patchUrlQuery({
      [MUSHAF_URL_KEYS.page]: pageNumber,
      [MUSHAF_URL_KEYS.focusAyah]: verseKey,
    });
  }

  private patchAyahSelectionQuery(verseKey: string): void {
    this.cancelPeekFlashClearTimer();
    this.patchUrlQuery(this.buildAyahSelectionQueryParams(verseKey));
  }

  private buildAyahSelectionQueryParams(
    verseKey: string,
  ): Partial<
    Record<(typeof MUSHAF_URL_KEYS)[keyof typeof MUSHAF_URL_KEYS], string | number | null>
  > {
    const currentWord = this._selectedWordLocation();
    const wordAyah = currentWord ? verseKeyFromWordLocation(currentWord) : null;
    const queryParams: Partial<
      Record<(typeof MUSHAF_URL_KEYS)[keyof typeof MUSHAF_URL_KEYS], string | number | null>
    > = {
      [MUSHAF_URL_KEYS.ayah]: verseKey,
      [MUSHAF_URL_KEYS.panel]: 'ayah',
      [MUSHAF_URL_KEYS.focusAyah]: null,
    };

    if (wordAyah && wordAyah !== verseKey) {
      queryParams[MUSHAF_URL_KEYS.word] = null;
      queryParams[MUSHAF_URL_KEYS.segment] = null;
    }

    return queryParams;
  }

  selectWord(wordLocation: string): void {
    this.cancelPeekFlashClearTimer();
    const verseKey = verseKeyFromWordLocation(wordLocation);
    const queryParams: Partial<
      Record<(typeof MUSHAF_URL_KEYS)[keyof typeof MUSHAF_URL_KEYS], string | number | null>
    > = {
      [MUSHAF_URL_KEYS.word]: wordLocation,
      [MUSHAF_URL_KEYS.panel]: 'word',
      [MUSHAF_URL_KEYS.focusAyah]: null,
    };

    if (verseKey) {
      queryParams[MUSHAF_URL_KEYS.ayah] = verseKey;
    }

    this.patchUrlQuery(queryParams);
  }

  moveSelectedWord(direction: 'previous' | 'next'): boolean {
    const page = this._page();
    const selectedWordLocation = this._selectedWordLocation();

    if (!page || !selectedWordLocation) {
      return false;
    }

    const words = page.lines.flatMap((line) => line.words);
    const currentIndex = words.findIndex((word) => word.wordLocation === selectedWordLocation);

    if (currentIndex < 0) {
      return false;
    }

    const step = direction === 'next' ? 1 : -1;
    let nextIndex = currentIndex + step;

    while (nextIndex >= 0 && nextIndex < words.length && words[nextIndex].isAyahMarker) {
      nextIndex += step;
    }

    const nextWord = words[nextIndex];
    if (!nextWord || nextWord.isAyahMarker) {
      return false;
    }

    this.selectWord(nextWord.wordLocation);
    return true;
  }

  loadWordAnalysis(wordLocation: string): void {
    this._selectedWordLocation.set(wordLocation);
    this.wordAnalysisLoadRunner.loadImmediate(wordLocation);
  }

  setPanel(panel: PanelMode): void {
    this.patchUrlQuery({
      [MUSHAF_URL_KEYS.panel]: panel === DEFAULT_MUSHAF_READER_STATE.panel ? null : panel,
    });
  }

  setAyahTab(tab: AyahStudyTab): void {
    this.patchUrlQuery({ [MUSHAF_URL_KEYS.ayahTab]: tab });
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

  loadStudySourceCatalog(): void {
    if (this.studySourceCatalogLoaded || this.studySourceCatalogLoading) {
      return;
    }

    this.studySourceCatalogLoading = true;
    subscribeToApiLoad(this.studySourceCatalogApi.getCatalog(), {
      onSuccess: (data) => {
        this.studySourceCatalogLoaded = true;
        this._tafsirSourceOptions.set(data.tafsirSources.map(tafsirCatalogItemToOption));
        this._translationSourceOptions.set(
          data.translationSources.map(translationCatalogItemToOption),
        );
        this._fullI3rabSourceOptions.set(data.fullI3rabSources.map(fullI3rabCatalogItemToOption));
      },
      onSettled: () => {
        this.studySourceCatalogLoading = false;
      },
      emptyMessage: 'تعذّر تحميل كتالوج مصادر الدراسة.',
      notFoundMessage: 'تعذّر تحميل كتالوج مصادر الدراسة.',
      connectionMessage: 'تعذّر الاتصال بالخادم.',
    });
  }

  resolveSurahStartPage(surahNumber: number): number | null {
    return resolveMushafSurahStartPage(surahNumber);
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
      this.readerCache.getOrLoad(MushafReaderCacheKeys.page(clamped), () =>
        this.pagesApi.getPage(clamped),
      ),
      {
        onSuccess: (data) => {
          this._page.set(toPageViewModel(data));
          prefetchAdjacentMushafPages(
            data.previousPageNumber,
            data.nextPageNumber,
            this.readerCache,
            this.pagesApi,
          );
          if (this._focusAyahKey() !== null) {
            this.scheduleFocusAyahClear();
          }
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
      },
    );
  }

  loadAyahStudy(verseKey: string): void {
    this._selectedAyahKey.set(verseKey);
    this.ayahStudyLoadRunner.loadImmediate(verseKey);
  }

  private hydrateFromUrl(snapshot: MushafUrlSnapshot): void {
    this.loadPage(snapshot.pageNumber);
    const previousFocusAyah = this._focusAyahKey();
    this._focusAyahKey.set(snapshot.focusAyah);
    if (snapshot.focusAyah) {
      if (snapshot.focusAyah !== previousFocusAyah) {
        this.scheduleFocusAyahClear();
      }
    } else {
      this.cancelPeekFlashClearTimer();
    }
    this.applyUrlState(snapshot);
  }

  private cancelPeekFlashClearTimer(): void {
    if (this.peekFlashClearTimer === null) {
      return;
    }

    clearTimeout(this.peekFlashClearTimer);
    this.peekFlashClearTimer = null;
  }

  private scheduleFocusAyahClear(): void {
    this.cancelPeekFlashClearTimer();
    this.peekFlashClearTimer = setTimeout(() => {
      this.peekFlashClearTimer = null;
      if (this._focusAyahKey() !== null && this.activeRoute !== null) {
        this.patchUrlQuery({ [MUSHAF_URL_KEYS.focusAyah]: null });
      }
    }, PEEK_FLASH_CLEAR_MS);
  }

  private patchUrlQuery(
    params: Partial<
      Record<(typeof MUSHAF_URL_KEYS)[keyof typeof MUSHAF_URL_KEYS], string | number | null>
    >,
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

  applyUrlState(
    snapshot: Pick<
      MushafUrlSnapshot,
      'panel' | 'ayah' | 'word' | 'segment' | 'ayahTab' | 'wordTab' | 'sources'
    >,
  ): void {
    const recovering = this.rebindRecoveryPending;
    this.rebindRecoveryPending = false;

    applyAuthoritativeUrlSnapshot(
      snapshot,
      {
        selectedAyahKey: this._selectedAyahKey(),
        selectedWordLocation: this._selectedWordLocation(),
        urlExplicitSources: this._urlExplicitSources(),
        ayahStudyIsLoading: recovering && this._ayahStudyLoadState().isLoading,
        wordAnalysisIsLoading: recovering && this._wordAnalysisLoadState().isLoading,
      },
      {
        setUiState: (panel, ayahTab, wordTab, segmentLocation) => {
          this._panel.set(panel);
          this._ayahTab.set(ayahTab);
          this._wordTab.set(wordTab);
          this._selectedSegmentLocation.set(segmentLocation);
        },
        clearWordSelection: () => {
          this.wordAnalysisLoadRunner.clearPending();
          this._selectedWordLocation.set(null);
          this._selectedSegmentLocation.set(null);
          this._wordAnalysis.set(null);
          this._wordAnalysisLoadState.set({ isLoading: false, isEmpty: false, errorMessage: null });
        },
        setWord: (wordLocation, reload) => {
          const hadWordSelection = this._selectedWordLocation() !== null;
          this._selectedWordLocation.set(wordLocation);

          if (reload) {
            if (hadWordSelection) {
              this.wordAnalysisLoadRunner.schedule(wordLocation);
            } else {
              this.loadWordAnalysis(wordLocation);
            }
          }
        },
        setUrlExplicitSources: (sources) => this._urlExplicitSources.set(sources),
        clearAyahSelection: () => {
          this.ayahStudyLoadRunner.clearPending();
          this.similarAyahsLoadRunner.clearData();
          this.mutashabihatLoadRunner.clearData();
          this._selectedAyahKey.set(null);
          this._ayahStudy.set(null);
          this._ayahStudyLoadState.set({ isLoading: false, isEmpty: false, errorMessage: null });
        },
        setAyah: (verseKey, reload) => {
          const hadAyahSelection = this._selectedAyahKey() !== null;
          const verseKeyChanged = this._selectedAyahKey() !== verseKey;
          this._selectedAyahKey.set(verseKey);

          if (verseKeyChanged) {
            this.similarAyahsLoadRunner.clearData();
            this.mutashabihatLoadRunner.clearData();
          }

          if (reload) {
            if (verseKeyChanged && hadAyahSelection) {
              this.ayahStudyLoadRunner.schedule(verseKey);
            } else {
              this.loadAyahStudy(verseKey);
            }
          }
        },
      },
    );

    this.syncSimilarAyahsDetail(snapshot.ayah, snapshot.ayahTab);
    this.syncMutashabihatDetail(snapshot.ayah, snapshot.ayahTab);
  }

  private syncSimilarAyahsDetail(verseKey: string | null, ayahTab: AyahStudyTab): void {
    if (!verseKey || ayahTab !== 'similar-ayahs') {
      return;
    }

    const current = this._similarAyahs();
    if (current?.verseKey === verseKey && !this._similarAyahsLoadState().isLoading) {
      return;
    }

    this.similarAyahsLoadRunner.loadImmediate(verseKey);
  }

  private syncMutashabihatDetail(verseKey: string | null, ayahTab: AyahStudyTab): void {
    if (!verseKey || ayahTab !== 'mutashabihat') {
      return;
    }

    const current = this._mutashabihat();
    if (current?.verseKey === verseKey && !this._mutashabihatLoadState().isLoading) {
      return;
    }

    this.mutashabihatLoadRunner.loadImmediate(verseKey);
  }
}
