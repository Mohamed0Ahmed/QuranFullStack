import { Subscription } from 'rxjs';

import { MushafAyahStudyApi } from '../data-access/mushaf-ayah-study.api';
import {
  AyahStudyDto,
  AyahStudyViewModel,
  MushafReaderSources,
  ResourceLoadState,
} from '../models/mushaf.models';
import { subscribeToApiLoad } from './mushaf-api-load.helpers';
import { MushafReaderCache, MushafReaderCacheKeys } from './mushaf-reader-cache';
import { toAyahStudyViewModel } from './mushaf-reader-view-mappers';

export const AYAH_STUDY_SWITCH_DELAY_MS = 700;

export interface AyahStudyLoadBindings {
  getUrlExplicitSources(): MushafReaderSources;
  setAyahStudy(value: AyahStudyViewModel | null): void;
  setSources(sources: MushafReaderSources): void;
  setLoadState(state: ResourceLoadState): void;
  bumpRequestToken(): number;
  getRequestToken(): number;
  ayahStudyApi: MushafAyahStudyApi;
  readerCache: MushafReaderCache;
}

export class AyahStudyLoadRunner {
  private timer: ReturnType<typeof setTimeout> | null = null;
  private activeSubscription: Subscription | null = null;

  constructor(private readonly bindings: AyahStudyLoadBindings) {}

  loadImmediate(verseKey: string): void {
    this.clearPending();
    const requestToken = this.bindings.bumpRequestToken();
    this.runLoad(verseKey, requestToken);
  }

  schedule(verseKey: string): void {
    this.clearPending();

    if (this.applyCached(verseKey)) {
      return;
    }

    const requestToken = this.bindings.bumpRequestToken();
    this.bindings.setLoadState({ isLoading: true, isEmpty: false, errorMessage: null });
    this.timer = setTimeout(() => {
      this.timer = null;
      this.runLoad(verseKey, requestToken);
    }, AYAH_STUDY_SWITCH_DELAY_MS);
  }

  clearPending(): void {
    if (this.timer !== null) {
      clearTimeout(this.timer);
      this.timer = null;
    }

    if (this.activeSubscription !== null) {
      this.activeSubscription.unsubscribe();
      this.activeSubscription = null;
    }

    this.bindings.bumpRequestToken();
  }

  private applyCached(verseKey: string): boolean {
    const sources = this.bindings.getUrlExplicitSources();
    const cached = this.bindings.readerCache.peek<AyahStudyDto>(
      MushafReaderCacheKeys.ayahStudy(verseKey, sources),
    );
    if (!cached) {
      return false;
    }

    this.bindings.setAyahStudy(toAyahStudyViewModel(cached));
    this.bindings.setSources({
      tafsirSource: cached.selectedSources.tafsirSource,
      translationSource: cached.selectedSources.translationSource,
      fullI3rabSource: cached.selectedSources.fullI3rabSource,
    });
    this.bindings.setLoadState({ isLoading: false, isEmpty: false, errorMessage: null });
    return true;
  }

  private runLoad(verseKey: string, requestToken: number): void {
    this.bindings.setLoadState({ isLoading: true, isEmpty: false, errorMessage: null });

    const sources = this.bindings.getUrlExplicitSources();
    const cacheKey = MushafReaderCacheKeys.ayahStudy(verseKey, sources);
    this.activeSubscription = subscribeToApiLoad(
      this.bindings.readerCache.getOrLoad(cacheKey, () =>
        this.bindings.ayahStudyApi.getAyahStudy(verseKey, sources),
      ),
      {
        onSuccess: (data) => {
          if (this.bindings.getRequestToken() !== requestToken) {
            return;
          }

          this.bindings.setAyahStudy(toAyahStudyViewModel(data));
          this.bindings.setSources({
            tafsirSource: data.selectedSources.tafsirSource,
            translationSource: data.selectedSources.translationSource,
            fullI3rabSource: data.selectedSources.fullI3rabSource,
          });
        },
        onSettled: (loadState) => {
          this.activeSubscription = null;

          if (this.bindings.getRequestToken() !== requestToken) {
            return;
          }

          if (loadState.isEmpty) {
            this.bindings.setAyahStudy(null);
          }
          this.bindings.setLoadState(loadState);
        },
        emptyMessage: 'تعذّر تحميل دراسة الآية.',
        notFoundMessage: 'الآية غير موجودة.',
        connectionMessage: 'تعذّر الاتصال بالخادم.',
      },
    );
  }
}
