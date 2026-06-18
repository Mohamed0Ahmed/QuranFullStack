import { Injectable, computed, inject, signal } from '@angular/core';

import { MushafAyahStudyApi } from '../data-access/mushaf-ayah-study.api';
import { MushafPagesApi } from '../data-access/mushaf-pages.api';
import { MushafSurahCatalogApi } from '../data-access/mushaf-surah-catalog.api';
import {
  AyahStudyDto,
  AyahStudyViewModel,
  DEFAULT_MUSHAF_READER_STATE,
  MushafPageDto,
  MushafPageViewModel,
  MushafReaderState,
  MushafSurahCatalogItemDto,
  ResourceLoadState,
  SourceOption,
  WordAnalysisViewModel,
} from '../models/mushaf.models';
import { subscribeToApiLoad } from './mushaf-api-load.helpers';

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
 * Owns all reader view state (selections, sources, tabs, and per-resource
 * loading/empty/error primitives). Load methods for ayah/word study and full
 * URL sync is added by later stories (T044 / T048).
 */
@Injectable({ providedIn: 'root' })
export class MushafReaderFacade {
  private readonly pagesApi = inject(MushafPagesApi);
  private readonly ayahStudyApi = inject(MushafAyahStudyApi);
  private readonly surahCatalogApi = inject(MushafSurahCatalogApi);

  private readonly _pageNumber = signal(DEFAULT_MUSHAF_READER_STATE.pageNumber);
  private readonly _selectedAyahKey = signal(DEFAULT_MUSHAF_READER_STATE.selectedAyahKey);
  private readonly _selectedWordLocation = signal(DEFAULT_MUSHAF_READER_STATE.selectedWordLocation);
  private readonly _selectedSegmentLocation = signal(DEFAULT_MUSHAF_READER_STATE.selectedSegmentLocation);
  private readonly _panel = signal(DEFAULT_MUSHAF_READER_STATE.panel);
  private readonly _ayahTab = signal(DEFAULT_MUSHAF_READER_STATE.ayahTab);
  private readonly _wordTab = signal(DEFAULT_MUSHAF_READER_STATE.wordTab);
  private readonly _sources = signal(DEFAULT_MUSHAF_READER_STATE.sources);

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

    subscribeToApiLoad(this.pagesApi.getPage(clamped), {
      onSuccess: (data) => this._page.set(toPageViewModel(data)),
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

    const sources = this._sources();
    subscribeToApiLoad(
      this.ayahStudyApi.getAyahStudy(verseKey, sources),
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

  applyUrlState(params: {
    ayah: string | null;
    tafsirSource: string | null;
    translationSource: string | null;
    fullI3rabSource: string | null;
    ayahTab: MushafReaderState['ayahTab'] | null;
  }): void {
    if (params.ayahTab) {
      this._ayahTab.set(params.ayahTab);
    }

    const previousAyah = this._selectedAyahKey();
    const previousSources = this._sources();
    const nextSources = {
      tafsirSource: params.tafsirSource ?? previousSources.tafsirSource,
      translationSource: params.translationSource ?? previousSources.translationSource,
      fullI3rabSource: params.fullI3rabSource ?? previousSources.fullI3rabSource,
    };

    const ayahChanged = params.ayah !== previousAyah;
    const sourcesChanged =
      nextSources.tafsirSource !== previousSources.tafsirSource ||
      nextSources.translationSource !== previousSources.translationSource ||
      nextSources.fullI3rabSource !== previousSources.fullI3rabSource;

    this._sources.set(nextSources);

    if (params.ayah) {
      this._selectedAyahKey.set(params.ayah);
      if (ayahChanged || sourcesChanged) {
        this.loadAyahStudy(params.ayah);
      }
    } else {
      this._selectedAyahKey.set(null);
      this._ayahStudy.set(null);
      this._ayahStudyLoadState.set({ isLoading: false, isEmpty: false, errorMessage: null });
    }
  }
}
