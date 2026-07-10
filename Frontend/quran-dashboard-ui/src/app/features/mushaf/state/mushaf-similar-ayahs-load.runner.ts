import { MushafSimilarAyahsApi } from '../data-access/mushaf-similar-ayahs.api';
import { ResourceLoadState, SimilarAyahsDto } from '../models/mushaf.models';
import { subscribeToApiLoad } from './mushaf-api-load.helpers';
import { MushafReaderCache, MushafReaderCacheKeys } from './mushaf-reader-cache';

export interface SimilarAyahsLoadBindings {
  setSimilarAyahs(value: SimilarAyahsDto | null): void;
  setLoadState(state: ResourceLoadState): void;
  bumpRequestToken(): number;
  getRequestToken(): number;
  similarAyahsApi: MushafSimilarAyahsApi;
  readerCache: MushafReaderCache;
}

export class SimilarAyahsLoadRunner {
  private timer: ReturnType<typeof setTimeout> | null = null;

  constructor(private readonly bindings: SimilarAyahsLoadBindings) {}

  loadImmediate(verseKey: string): void {
    this.clearPending();
    const requestToken = this.bindings.bumpRequestToken();
    this.runLoad(verseKey, requestToken);
  }

  clearPending(): void {
    if (this.timer !== null) {
      clearTimeout(this.timer);
      this.timer = null;
    }

    this.bindings.bumpRequestToken();
  }

  clearData(): void {
    this.clearPending();
    this.bindings.setSimilarAyahs(null);
    this.bindings.setLoadState({ isLoading: false, isEmpty: false, errorMessage: null });
  }

  private applyCached(verseKey: string): boolean {
    const cached = this.bindings.readerCache.peek<SimilarAyahsDto>(
      MushafReaderCacheKeys.similarAyahs(verseKey),
    );
    if (!cached) {
      return false;
    }

    this.bindings.setSimilarAyahs(cached);
    this.bindings.setLoadState({ isLoading: false, isEmpty: false, errorMessage: null });
    return true;
  }

  private runLoad(verseKey: string, requestToken: number): void {
    if (this.applyCached(verseKey)) {
      return;
    }

    this.bindings.setLoadState({ isLoading: true, isEmpty: false, errorMessage: null });

    subscribeToApiLoad(
      this.bindings.readerCache.getOrLoad(
        MushafReaderCacheKeys.similarAyahs(verseKey),
        () => this.bindings.similarAyahsApi.getSimilarAyahs(verseKey),
      ),
      {
        onSuccess: (data) => {
          if (this.bindings.getRequestToken() !== requestToken) {
            return;
          }

          this.bindings.setSimilarAyahs(data);
        },
        onSettled: (loadState) => {
          if (this.bindings.getRequestToken() !== requestToken) {
            return;
          }

          if (loadState.isEmpty) {
            this.bindings.setSimilarAyahs(null);
          }
          this.bindings.setLoadState(loadState);
        },
        emptyMessage: 'تعذّر تحميل الآيات القريبة في المعنى.',
        notFoundMessage: 'الآية غير موجودة.',
        connectionMessage: 'تعذّر الاتصال بالخادم.',
      },
    );
  }
}
