import { MushafAyahMutashabihatApi } from '../data-access/mushaf-ayah-mutashabihat.api';
import { AyahMutashabihatDto, ResourceLoadState } from '../models/mushaf.models';
import { subscribeToApiLoad } from './mushaf-api-load.helpers';
import { MushafReaderCache, MushafReaderCacheKeys } from './mushaf-reader-cache';
import type { QuranVerseKey } from '../../../shared/quran/quran-location';

export interface MutashabihatLoadBindings {
  setMutashabihat(value: AyahMutashabihatDto | null): void;
  setLoadState(state: ResourceLoadState): void;
  bumpRequestToken(): number;
  getRequestToken(): number;
  mutashabihatApi: MushafAyahMutashabihatApi;
  readerCache: MushafReaderCache;
}

export class MutashabihatLoadRunner {
  private timer: ReturnType<typeof setTimeout> | null = null;

  constructor(private readonly bindings: MutashabihatLoadBindings) {}

  loadImmediate(verseKey: QuranVerseKey): void {
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
    this.bindings.setMutashabihat(null);
    this.bindings.setLoadState({ isLoading: false, isEmpty: false, errorMessage: null });
  }

  private applyCached(verseKey: QuranVerseKey): boolean {
    const cached = this.bindings.readerCache.peek<AyahMutashabihatDto>(
      MushafReaderCacheKeys.ayahMutashabihat(verseKey),
    );
    if (!cached) {
      return false;
    }

    this.bindings.setMutashabihat(cached);
    this.bindings.setLoadState({ isLoading: false, isEmpty: false, errorMessage: null });
    return true;
  }

  private runLoad(verseKey: QuranVerseKey, requestToken: number): void {
    if (this.applyCached(verseKey)) {
      return;
    }

    this.bindings.setLoadState({ isLoading: true, isEmpty: false, errorMessage: null });

    subscribeToApiLoad(
      this.bindings.readerCache.getOrLoad(
        MushafReaderCacheKeys.ayahMutashabihat(verseKey),
        () => this.bindings.mutashabihatApi.getAyahMutashabihat(verseKey),
      ),
      {
        onSuccess: (data) => {
          if (this.bindings.getRequestToken() !== requestToken) {
            return;
          }

          this.bindings.setMutashabihat(data);
        },
        onSettled: (loadState) => {
          if (this.bindings.getRequestToken() !== requestToken) {
            return;
          }

          if (loadState.isEmpty) {
            this.bindings.setMutashabihat(null);
          }
          this.bindings.setLoadState(loadState);
        },
        emptyMessage: 'تعذّر تحميل المتشابهات اللفظية.',
        notFoundMessage: 'الآية غير موجودة.',
        connectionMessage: 'تعذّر الاتصال بالخادم.',
      },
    );
  }
}
