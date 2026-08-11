import { Subscription } from 'rxjs';

import { MushafWordAnalysisApi } from '../data-access/mushaf-word-analysis.api';
import { ResourceLoadState, WordAnalysisDto, WordAnalysisViewModel } from '../models/mushaf.models';
import { subscribeToApiLoad } from './mushaf-api-load.helpers';
import { MushafReaderCache, MushafReaderCacheKeys } from './mushaf-reader-cache';
import { isAyahMarkerOnMushafPage } from './mushaf-reader-page.helpers';
import { toWordAnalysisViewModel } from './mushaf-reader-view-mappers';
import type { MushafPageViewModel } from '../models/mushaf.models';

export interface WordAnalysisLoadBindings {
  getPage(): MushafPageViewModel | null;
  setAnalysis(value: WordAnalysisViewModel | null): void;
  setLoadState(state: ResourceLoadState): void;
  bumpRequestToken(): number;
  getRequestToken(): number;
  wordAnalysisApi: MushafWordAnalysisApi;
  readerCache: MushafReaderCache;
}

export class WordAnalysisLoadRunner {
  private activeSubscription: Subscription | null = null;

  constructor(private readonly bindings: WordAnalysisLoadBindings) {}

  loadImmediate(wordLocation: string): void {
    this.clearPending();
    const requestToken = this.bindings.bumpRequestToken();
    this.runLoad(wordLocation, requestToken);
  }

  schedule(wordLocation: string): void {
    this.clearPending();

    if (this.applyCached(wordLocation)) {
      return;
    }

    const requestToken = this.bindings.bumpRequestToken();
    this.runLoad(wordLocation, requestToken);
  }

  clearPending(): void {
    if (this.activeSubscription !== null) {
      this.activeSubscription.unsubscribe();
      this.activeSubscription = null;
    }

    this.bindings.bumpRequestToken();
  }

  private applyCached(wordLocation: string): boolean {
    if (isAyahMarkerOnMushafPage(this.bindings.getPage(), wordLocation)) {
      return false;
    }

    const cached = this.bindings.readerCache.peek<WordAnalysisDto>(
      MushafReaderCacheKeys.wordAnalysis(wordLocation),
    );
    if (!cached) {
      return false;
    }

    this.bindings.setAnalysis(toWordAnalysisViewModel(cached));
    this.bindings.setLoadState({ isLoading: false, isEmpty: false, errorMessage: null });
    return true;
  }

  private runLoad(wordLocation: string, requestToken: number): void {
    if (isAyahMarkerOnMushafPage(this.bindings.getPage(), wordLocation)) {
      this.bindings.setAnalysis(null);
      this.bindings.setLoadState({
        isLoading: false,
        isEmpty: false,
        errorMessage: 'هذه الكلمة غير قابلة للتحليل (علامة نهاية آية)',
      });
      return;
    }

    this.bindings.setLoadState({ isLoading: true, isEmpty: false, errorMessage: null });

    this.activeSubscription = subscribeToApiLoad(
      this.bindings.readerCache.getOrLoad(
        MushafReaderCacheKeys.wordAnalysis(wordLocation),
        () => this.bindings.wordAnalysisApi.getWordAnalysis(wordLocation),
      ),
      {
        onSuccess: (data) => {
          if (this.bindings.getRequestToken() !== requestToken) {
            return;
          }

          this.bindings.setAnalysis(toWordAnalysisViewModel(data));
        },
        onSettled: (loadState) => {
          this.activeSubscription = null;

          if (this.bindings.getRequestToken() !== requestToken) {
            return;
          }

          if (loadState.isEmpty) {
            this.bindings.setAnalysis(null);
          }
          this.bindings.setLoadState(loadState);
        },
        emptyMessage: 'تعذّر تحميل تحليل الكلمة.',
        notFoundMessage: 'الكلمة غير موجودة.',
        connectionMessage: 'تعذّر الاتصال بالخادم.',
      },
    );
  }
}
