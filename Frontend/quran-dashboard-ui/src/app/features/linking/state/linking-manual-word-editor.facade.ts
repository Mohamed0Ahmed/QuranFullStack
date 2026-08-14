import { Injectable, computed, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';

import { LinkingManualWordIdsByVerseKey } from '../models/linking-manual-mushaf.models';
import { LinkingPageRange, LinkingSourcePage } from '../models/linking-page.models';
import { selectedLinkingVerseKeys } from '../utils/linking-selection';
import { LinkingAccessService } from './linking-access.service';
import { LinkingSourcePagesFacade } from './linking-source-pages.facade';
import { LinkingWorkspaceStore } from './linking-workspace.store';

type ManualWordLoadStatus = 'idle' | 'loading' | 'success' | 'error';

interface ManualWordEditorState {
  sourceKey: string | null;
  capturedConfigurationRevision: number | null;
  status: ManualWordLoadStatus;
  page: LinkingSourcePage | null;
  ayahIds: readonly number[];
  draftWordIdsByAyahId: Readonly<Record<number, readonly number[]>>;
  errorMessage: string | null;
}

const INITIAL_STATE: ManualWordEditorState = {
  sourceKey: null,
  capturedConfigurationRevision: null,
  status: 'idle',
  page: null,
  ayahIds: [],
  draftWordIdsByAyahId: {},
  errorMessage: null,
};

@Injectable({ providedIn: 'root' })
export class LinkingManualWordEditorFacade {
  private readonly access = inject(LinkingAccessService);
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly pages = inject(LinkingSourcePagesFacade);
  private readonly stateSignal = signal<ManualWordEditorState>(INITIAL_STATE);
  private subscription: Subscription | null = null;
  private activeRange: LinkingPageRange<LinkingSourcePage> | null = null;
  private generation = 0;

  readonly item = computed(() => {
    const sourceKey = this.stateSignal().sourceKey;
    return sourceKey === null ? null : this.workspace.item(sourceKey);
  });
  readonly state = computed(() => {
    const state = this.stateSignal();
    return {
      ...state,
      draftWordIds: toWordIdsByVerseKey(
        state.draftWordIdsByAyahId,
        this.item()?.ayahIdByVerseKey ?? {},
      ),
    };
  });
  readonly includedVerseKeys = computed(() => {
    const item = this.item();
    return item?.source.kind === 'manual-mushaf-ayahs' && item.configuration.kind === 'manual'
      ? selectedLinkingVerseKeys(
          item.configuration.ayahInclusion,
          item.source.manualAyahs.map((ayah) => ayah.verseKey),
        )
      : [];
  });
  readonly ayahs = computed(() => {
    const state = this.stateSignal();
    return state.page === null
      ? []
      : state.ayahIds
          .map((ayahId) => this.pages.displayAyah(state.page!, ayahId))
          .filter((ayah) => ayah !== null);
  });
  readonly status = computed(() => this.stateSignal().status);
  readonly selectedWordCount = computed(() =>
    Object.values(this.stateSignal().draftWordIdsByAyahId).reduce(
      (count, quranWordIds) => count + quranWordIds.length,
      0,
    ),
  );

  open(sourceKey: string | null): void {
    const item = sourceKey === null ? null : this.workspace.item(sourceKey);
    if (
      !this.access.canUseLinking() ||
      item?.source.kind !== 'manual-mushaf-ayahs' ||
      item.configuration.kind !== 'manual'
    ) {
      this.close();
      return;
    }
    if (this.stateSignal().sourceKey === sourceKey) {
      return;
    }
    this.cancelLoad();
    this.stateSignal.set({
      ...INITIAL_STATE,
      sourceKey,
      capturedConfigurationRevision: item.configurationRevision,
      draftWordIdsByAyahId: toWordIdsByAyahId(
        item.configuration.quranWordIdsByVerseKey,
        item.ayahIdByVerseKey,
      ),
    });
    if (this.includedVerseKeys().length > 0) {
      this.loadFirstPage();
    }
  }

  close(): void {
    this.generation += 1;
    this.cancelLoad();
    this.stateSignal.set(INITIAL_STATE);
  }

  retry(): void {
    if (this.item() !== null && this.includedVerseKeys().length > 0) {
      this.loadFirstPage();
    }
  }

  toggleWord(verseKey: string, quranWordId: number): void {
    const ayah = this.ayahs().find((candidate) => candidate.verseKey === verseKey);
    if (
      ayah?.words.some(
        (word) => !word.isAyahMarker && word.canonicalQuranWordId === quranWordId,
      ) !== true
    ) {
      return;
    }
    this.stateSignal.update((state) => {
      const selected = new Set(state.draftWordIdsByAyahId[ayah.ayahId] ?? []);
      selected.has(quranWordId) ? selected.delete(quranWordId) : selected.add(quranWordId);
      return {
        ...state,
        draftWordIdsByAyahId: {
          ...state.draftWordIdsByAyahId,
          [ayah.ayahId]: [...selected].sort((left, right) => left - right),
        },
      };
    });
  }

  clearAyah(verseKey: string): void {
    const ayah = this.ayahs().find((candidate) => candidate.verseKey === verseKey);
    if (ayah === undefined) {
      return;
    }
    this.stateSignal.update((state) => ({
      ...state,
      draftWordIdsByAyahId: { ...state.draftWordIdsByAyahId, [ayah.ayahId]: [] },
    }));
  }

  save(): boolean {
    const state = this.stateSignal();
    const item = state.sourceKey === null ? null : this.workspace.item(state.sourceKey);
    if (
      !this.access.canUseLinking() ||
      item?.configuration.kind !== 'manual' ||
      item.configurationRevision !== state.capturedConfigurationRevision
    ) {
      return false;
    }
    this.workspace.setManualWordIds(
      item.sourceKey,
      toWordIdsByVerseKey(state.draftWordIdsByAyahId, item.ayahIdByVerseKey),
    );
    return true;
  }

  private loadFirstPage(): void {
    const item = this.item();
    if (item === null) {
      return;
    }
    this.cancelLoad();
    const generation = ++this.generation;
    this.stateSignal.update((state) => ({
      ...state,
      status: 'loading',
      page: null,
      ayahIds: [],
      errorMessage: null,
    }));
    this.subscription = this.pages
      .loadRange(
        `manual-word-editor:${item.sourceKey}`,
        {
          source: item.source,
          expectedLinkingDataRevision: item.linkingDataRevision,
          expectedSourceViewIdentity: null,
          view: {
            segment: 'included',
            inclusionMode:
              item.configuration.ayahInclusion.mode === 'all-except' ? 'all_except' : 'only',
            ayahOverrideIds: [...item.ayahOverrideIds],
          },
          pageSize: 100,
          draftGeneration: generation,
        },
        0,
        99,
      )
      .subscribe({
        next: (range) => {
          if (generation !== this.generation) {
            range.release();
            return;
          }
          this.activeRange?.release();
          this.activeRange = range;
          const page = range.pages[0] ?? null;
          this.stateSignal.update((state) => ({
            ...state,
            status: 'success',
            page,
            ayahIds: page?.ayahIds ?? [],
          }));
        },
        error: (error: unknown) => this.publishLoadError(generation, error),
      });
  }

  private publishLoadError(generation: number, error: unknown): void {
    if (generation === this.generation) {
      this.stateSignal.update((state) => ({
        ...state,
        status: 'error',
        page: null,
        ayahIds: [],
        errorMessage: error instanceof Error ? error.message : 'تعذر تحميل كلمات الآيات.',
      }));
    }
  }

  private cancelLoad(): void {
    this.subscription?.unsubscribe();
    this.subscription = null;
    this.activeRange?.release();
    this.activeRange = null;
    const sourceKey = this.stateSignal().sourceKey;
    if (sourceKey !== null) {
      this.pages.cancel(`manual-word-editor:${sourceKey}`);
    }
  }
}

function toWordIdsByAyahId(
  byVerseKey: LinkingManualWordIdsByVerseKey,
  ayahIdByVerseKey: Readonly<Record<string, number>>,
): Readonly<Record<number, readonly number[]>> {
  return Object.fromEntries(
    Object.entries(byVerseKey)
      .map(([verseKey, wordIds]) => [ayahIdByVerseKey[verseKey], wordIds] as const)
      .filter((entry): entry is readonly [number, readonly number[]] => entry[0] !== undefined),
  );
}

function toWordIdsByVerseKey(
  byAyahId: Readonly<Record<number, readonly number[]>>,
  ayahIdByVerseKey: Readonly<Record<string, number>>,
): LinkingManualWordIdsByVerseKey {
  const verseKeyByAyahId = new Map(
    Object.entries(ayahIdByVerseKey).map(([verseKey, ayahId]) => [ayahId, verseKey]),
  );
  return Object.fromEntries(
    Object.entries(byAyahId)
      .map(([ayahId, wordIds]) => [verseKeyByAyahId.get(Number(ayahId)), wordIds] as const)
      .filter((entry): entry is readonly [string, readonly number[]] => entry[0] !== undefined),
  );
}
