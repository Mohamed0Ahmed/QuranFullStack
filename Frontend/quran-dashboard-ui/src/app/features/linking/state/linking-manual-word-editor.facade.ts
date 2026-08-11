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
  activeVerseKey: string | null;
  occurrencesByVerseKey: Readonly<Record<string, LinkingAyah>>;
  loadStatusByVerseKey: Readonly<Record<string, ManualWordLoadStatus>>;
  draftLocations: LinkingManualWordLocationsByVerseKey;
  errorMessage: string | null;
}

const INITIAL_STATE: ManualWordEditorState = {
  sourceKey: null,
  capturedConfigurationRevision: null,
  activeVerseKey: null,
  occurrencesByVerseKey: {},
  loadStatusByVerseKey: {},
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
  readonly activeAyah = computed(() => {
    const activeVerseKey = this.stateSignal().activeVerseKey;
    return activeVerseKey === null ? null : this.stateSignal().occurrencesByVerseKey[activeVerseKey] ?? null;
  });
  readonly activeStatus = computed(() => {
    const activeVerseKey = this.stateSignal().activeVerseKey;
    return activeVerseKey === null ? 'idle' : this.stateSignal().loadStatusByVerseKey[activeVerseKey] ?? 'idle';
  });
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
      activeVerseKey: includedVerseKeys[0] ?? null,
      draftLocations: item.configuration.wordLocationsByVerseKey,
    });
    if (includedVerseKeys[0]) {
      this.loadAyah(includedVerseKeys[0]);
    }
  }

  close(): void {
    this.generation += 1;
    this.cancelLoad();
    this.stateSignal.set(INITIAL_STATE);
  }

  activateAyah(verseKey: string): void {
    if (!this.includedVerseKeys().includes(verseKey)) {
      return;
    }
    this.stateSignal.update((state) => ({ ...state, activeVerseKey: verseKey, errorMessage: null }));
    if (!this.stateSignal().occurrencesByVerseKey[verseKey]) {
      this.loadAyah(verseKey);
    }
  }

  retry(): void {
    const verseKey = this.stateSignal().activeVerseKey;
    if (verseKey !== null) {
      this.loadAyah(verseKey);
    }
  }

  toggleWord(wordLocation: string): void {
    const verseKey = this.stateSignal().activeVerseKey;
    const activeAyah = this.activeAyah();
    if (
      verseKey === null ||
      activeAyah?.words.some((word) => !word.isAyahMarker && word.wordLocation === wordLocation) !== true
    ) {
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

  clearActiveAyah(): void {
    const verseKey = this.stateSignal().activeVerseKey;
    if (verseKey !== null) {
      this.stateSignal.update((state) => ({
        ...state,
        draftLocations: { ...state.draftLocations, [verseKey]: [] },
      }));
    }
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

  previous(): void {
    this.moveActiveAyah(-1);
  }

  next(): void {
    this.moveActiveAyah(1);
  }

  private moveActiveAyah(offset: number): void {
    const verseKeys = this.includedVerseKeys();
    const index = verseKeys.indexOf(this.stateSignal().activeVerseKey ?? '');
    const nextVerseKey = verseKeys[index + offset];
    if (nextVerseKey) {
      this.activateAyah(nextVerseKey);
    }
  }

  private loadAyah(verseKey: string): void {
    this.cancelLoad();
    const generation = ++this.generation;
    this.stateSignal.update((state) => ({
      ...state,
      loadStatusByVerseKey: { ...state.loadStatusByVerseKey, [verseKey]: 'loading' },
      errorMessage: null,
    }));
    this.subscription = this.reader.readCompleteAyah(verseKey).subscribe({
      next: (ayah) => {
        if (generation !== this.generation) {
          return;
        }
        this.stateSignal.update((state) => ({
          ...state,
          occurrencesByVerseKey: { ...state.occurrencesByVerseKey, [verseKey]: ayah },
          loadStatusByVerseKey: { ...state.loadStatusByVerseKey, [verseKey]: 'success' },
        }));
      },
      error: (error: unknown) => {
        if (generation !== this.generation) {
          return;
        }
        this.stateSignal.update((state) => ({
          ...state,
          loadStatusByVerseKey: { ...state.loadStatusByVerseKey, [verseKey]: 'error' },
          errorMessage: error instanceof Error ? error.message : 'تعذر تحميل كلمات الآية كاملة.',
        }));
      },
    });
  }

  private cancelLoad(): void {
    this.subscription?.unsubscribe();
    this.subscription = null;
  }
}
