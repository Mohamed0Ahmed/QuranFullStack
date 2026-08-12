import { Injectable, computed, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';

import { LinkingSourceResolver } from '../data-access/linking-source-resolver';
import { LinkingAyah } from '../models/linking-ayah.models';
import { LinkingManualWordIdsByVerseKey } from '../models/linking-manual-mushaf.models';
import { LinkingSourceDescriptor } from '../models/linking-source.models';
import { selectedLinkingVerseKeys } from '../utils/linking-selection';
import { LinkingAccessService } from './linking-access.service';
import { LinkingWorkspaceStore } from './linking-workspace.store';

type ManualWordLoadStatus = 'idle' | 'loading' | 'success' | 'error';

interface ManualWordEditorState {
  sourceKey: string | null;
  capturedConfigurationRevision: number | null;
  status: ManualWordLoadStatus;
  ayahs: readonly LinkingAyah[];
  draftWordIds: LinkingManualWordIdsByVerseKey;
  errorMessage: string | null;
}

const INITIAL_STATE: ManualWordEditorState = {
  sourceKey: null,
  capturedConfigurationRevision: null,
  status: 'idle',
  ayahs: [],
  draftWordIds: {},
  errorMessage: null,
};

@Injectable({ providedIn: 'root' })
export class LinkingManualWordEditorFacade {
  private readonly access = inject(LinkingAccessService);
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly resolver = inject(LinkingSourceResolver);
  private readonly stateSignal = signal<ManualWordEditorState>(INITIAL_STATE);
  private subscription: Subscription | null = null;
  private generation = 0;

  readonly state = this.stateSignal.asReadonly();
  readonly item = computed(() => {
    const sourceKey = this.stateSignal().sourceKey;
    return sourceKey === null ? null : this.workspace.item(sourceKey);
  });
  readonly includedVerseKeys = computed(() => {
    const item = this.item();
    return item?.source.kind === 'manual-mushaf-ayahs' && item.configuration.kind === 'manual'
      ? selectedLinkingVerseKeys(item.configuration.ayahInclusion, item.source.manualAyahs.map((ayah) => ayah.verseKey))
      : [];
  });
  readonly ayahs = computed(() => this.stateSignal().ayahs);
  readonly status = computed(() => this.stateSignal().status);
  readonly selectedWordCount = computed(() =>
    Object.values(this.stateSignal().draftWordIds).reduce((count, quranWordIds) => count + quranWordIds.length, 0),
  );

  open(sourceKey: string | null): void {
    const item = sourceKey === null ? null : this.workspace.item(sourceKey);
    if (!this.access.canUseLinking() || item?.source.kind !== 'manual-mushaf-ayahs' || item.configuration.kind !== 'manual') {
      this.close();
      return;
    }
    if (this.stateSignal().sourceKey === sourceKey) {
      return;
    }
    this.cancelLoad();
    const includedVerseKeys = selectedLinkingVerseKeys(
      item.configuration.ayahInclusion,
      item.source.manualAyahs.map((ayah) => ayah.verseKey),
    );
    this.stateSignal.set({
      ...INITIAL_STATE,
      sourceKey,
      capturedConfigurationRevision: item.configurationRevision,
      draftWordIds: item.configuration.quranWordIdsByVerseKey,
    });
    if (includedVerseKeys.length > 0) {
      this.loadAyahs(item.source, includedVerseKeys);
    }
  }

  close(): void {
    this.generation += 1;
    this.cancelLoad();
    this.stateSignal.set(INITIAL_STATE);
  }

  retry(): void {
    const item = this.item();
    const verseKeys = this.includedVerseKeys();
    if (item !== null && verseKeys.length > 0) {
      this.loadAyahs(item.source, verseKeys);
    }
  }

  toggleWord(verseKey: string, quranWordId: number): void {
    const ayah = this.stateSignal().ayahs.find((candidate) => candidate.verseKey === verseKey);
    if (ayah?.words.some((word) => !word.isAyahMarker && word.canonicalQuranWordId === quranWordId) !== true) {
      return;
    }
    this.stateSignal.update((state) => {
      const selected = new Set(state.draftWordIds[verseKey] ?? []);
      selected.has(quranWordId) ? selected.delete(quranWordId) : selected.add(quranWordId);
      return {
        ...state,
        draftWordIds: {
          ...state.draftWordIds,
          [verseKey]: [...selected].sort((left, right) => left - right),
        },
      };
    });
  }

  clearAyah(verseKey: string): void {
    if (!this.includedVerseKeys().includes(verseKey)) {
      return;
    }
    this.stateSignal.update((state) => ({
      ...state,
      draftWordIds: { ...state.draftWordIds, [verseKey]: [] },
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
    this.workspace.setManualWordIds(item.sourceKey, state.draftWordIds);
    return true;
  }

  private loadAyahs(source: LinkingSourceDescriptor, verseKeys: readonly string[]): void {
    this.cancelLoad();
    const generation = ++this.generation;
    this.stateSignal.update((state) => ({
      ...state,
      status: 'loading',
      ayahs: [],
      errorMessage: null,
    }));
    this.subscription = this.resolver.resolve(source, () => undefined).subscribe({
      next: (ayahs) => {
        if (generation !== this.generation) {
          return;
        }
        const orderedAyahs = orderAyahs(verseKeys, ayahs);
        if (orderedAyahs.length !== verseKeys.length) {
          this.publishLoadError(generation, 'تعذر تحميل جميع الآيات المحددة كاملة.');
          return;
        }
        this.stateSignal.update((state) => ({
          ...state,
          status: 'success',
          ayahs: orderedAyahs,
        }));
      },
      error: (error: unknown) => {
        this.publishLoadError(
          generation,
          error instanceof Error ? error.message : 'تعذر تحميل كلمات الآيات كاملة.',
        );
      },
    });
  }

  private publishLoadError(generation: number, errorMessage: string): void {
    if (generation !== this.generation) {
      return;
    }
    this.stateSignal.update((state) => ({ ...state, status: 'error', ayahs: [], errorMessage }));
  }

  private cancelLoad(): void {
    this.subscription?.unsubscribe();
    this.subscription = null;
  }
}

function orderAyahs(verseKeys: readonly string[], ayahs: readonly LinkingAyah[]): readonly LinkingAyah[] {
  const byVerseKey = new Map(ayahs.map((ayah) => [ayah.verseKey, ayah]));
  return verseKeys
    .map((verseKey) => byVerseKey.get(verseKey))
    .filter((ayah): ayah is LinkingAyah => ayah !== undefined);
}
