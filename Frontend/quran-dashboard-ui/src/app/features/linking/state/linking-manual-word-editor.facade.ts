import { Injectable, computed, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';

import { ManualMushafAyahReader } from '../data-access/manual-mushaf-ayah.reader';
import { LinkingAyah } from '../models/linking-ayah.models';
import { LinkingManualWordLocationsByVerseKey } from '../models/linking-manual-mushaf.models';
import { selectedLinkingVerseKeys } from '../utils/linking-selection';
import { LinkingAccessService } from './linking-access.service';
import { LinkingWorkspaceStore } from './linking-workspace.store';

type ManualWordLoadStatus = 'idle' | 'loading' | 'success' | 'error';

interface ManualWordEditorState {
  sourceKey: string | null;
  capturedConfigurationRevision: number | null;
  status: ManualWordLoadStatus;
  ayahs: readonly LinkingAyah[];
  draftLocations: LinkingManualWordLocationsByVerseKey;
  errorMessage: string | null;
}

const INITIAL_STATE: ManualWordEditorState = {
  sourceKey: null,
  capturedConfigurationRevision: null,
  status: 'idle',
  ayahs: [],
  draftLocations: {},
  errorMessage: null,
};

@Injectable({ providedIn: 'root' })
export class LinkingManualWordEditorFacade {
  private readonly access = inject(LinkingAccessService);
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly reader = inject(ManualMushafAyahReader);
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
    Object.values(this.stateSignal().draftLocations).reduce((count, locations) => count + locations.length, 0),
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
      draftLocations: item.configuration.wordLocationsByVerseKey,
    });
    if (includedVerseKeys.length > 0) {
      this.loadAyahs(includedVerseKeys);
    }
  }

  close(): void {
    this.generation += 1;
    this.cancelLoad();
    this.stateSignal.set(INITIAL_STATE);
  }

  retry(): void {
    const verseKeys = this.includedVerseKeys();
    if (verseKeys.length > 0) {
      this.loadAyahs(verseKeys);
    }
  }

  toggleWord(verseKey: string, wordLocation: string): void {
    const ayah = this.stateSignal().ayahs.find((candidate) => candidate.verseKey === verseKey);
    if (ayah?.words.some((word) => !word.isAyahMarker && word.wordLocation === wordLocation) !== true) {
      return;
    }
    this.stateSignal.update((state) => {
      const selected = new Set(state.draftLocations[verseKey] ?? []);
      selected.has(wordLocation) ? selected.delete(wordLocation) : selected.add(wordLocation);
      return {
        ...state,
        draftLocations: { ...state.draftLocations, [verseKey]: [...selected].sort() },
      };
    });
  }

  clearAyah(verseKey: string): void {
    if (!this.includedVerseKeys().includes(verseKey)) {
      return;
    }
    this.stateSignal.update((state) => ({
      ...state,
      draftLocations: { ...state.draftLocations, [verseKey]: [] },
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
    this.workspace.setManualWordLocations(item.sourceKey, state.draftLocations);
    return true;
  }

  private loadAyahs(verseKeys: readonly string[]): void {
    this.cancelLoad();
    const generation = ++this.generation;
    this.stateSignal.update((state) => ({
      ...state,
      status: 'loading',
      ayahs: [],
      errorMessage: null,
    }));
    this.subscription = this.reader.readCompleteAyahs(verseKeys).subscribe({
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
