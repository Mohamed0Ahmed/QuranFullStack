import { Injectable, computed, inject, signal } from '@angular/core';

import { LinkingSourcePageRequest } from '../models/linking-page.models';
import { LinkingAccessService } from './linking-access.service';
import { LinkingWorkspaceStore } from './linking-workspace.store';

interface ManualWordEditorState {
  sourceKey: string | null;
  capturedConfigurationRevision: number | null;
  status: 'idle' | 'preparing' | 'ready' | 'error';
  draftWordIdsByAyahId: Readonly<Record<number, readonly number[]>>;
  totalAyahCount: number;
  errorMessage: string | null;
  generation: number;
}

const INITIAL_STATE: ManualWordEditorState = {
  sourceKey: null,
  capturedConfigurationRevision: null,
  status: 'idle',
  draftWordIdsByAyahId: {},
  totalAyahCount: 0,
  errorMessage: null,
  generation: 0,
};

@Injectable({ providedIn: 'root' })
export class LinkingManualWordEditorFacade {
  private readonly access = inject(LinkingAccessService);
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly stateSignal = signal<ManualWordEditorState>(INITIAL_STATE);

  readonly state = this.stateSignal.asReadonly();
  readonly item = computed(() => {
    const sourceKey = this.stateSignal().sourceKey;
    return sourceKey === null ? null : this.workspace.item(sourceKey);
  });
  readonly request = computed<Omit<LinkingSourcePageRequest, 'page'> | null>(() => {
    const item = this.item();
    if (item?.configuration.kind !== 'manual') {
      return null;
    }
    return {
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
      draftGeneration: this.stateSignal().generation,
    };
  });
  readonly selectedWordCount = computed(() =>
    Object.values(this.stateSignal().draftWordIdsByAyahId).reduce(
      (count, wordIds) => count + wordIds.length,
      0,
    ),
  );

  open(sourceKey: string | null): void {
    const item = sourceKey === null ? null : this.workspace.item(sourceKey);
    if (!this.access.canUseLinking() || item?.configuration.kind !== 'manual') {
      this.close();
      return;
    }
    if (this.stateSignal().sourceKey === sourceKey) {
      return;
    }
    this.stateSignal.set({
      ...INITIAL_STATE,
      sourceKey,
      capturedConfigurationRevision: item.configurationRevision,
      status: 'preparing',
      draftWordIdsByAyahId: item.selectedWordIdsByAyahId,
      generation: this.stateSignal().generation + 1,
    });
  }

  close(): void {
    this.stateSignal.set({ ...INITIAL_STATE, generation: this.stateSignal().generation + 1 });
  }

  retry(): void {
    if (this.item() !== null) {
      this.stateSignal.update((state) => ({
        ...state,
        status: 'preparing',
        errorMessage: null,
        generation: state.generation + 1,
      }));
    }
  }

  pageReady(linkingDataRevision: number, totalAyahCount: number): void {
    const sourceKey = this.stateSignal().sourceKey;
    if (sourceKey === null) {
      return;
    }
    this.workspace.reconcilePage(sourceKey, linkingDataRevision, totalAyahCount);
    this.stateSignal.update((state) => ({ ...state, status: 'ready', totalAyahCount }));
  }

  toggleWord(ayahId: number, quranWordId: number): void {
    this.stateSignal.update((state) => {
      const selected = new Set(state.draftWordIdsByAyahId[ayahId] ?? []);
      selected.has(quranWordId) ? selected.delete(quranWordId) : selected.add(quranWordId);
      return {
        ...state,
        draftWordIdsByAyahId: {
          ...state.draftWordIdsByAyahId,
          [ayahId]: [...selected].sort((left, right) => left - right),
        },
      };
    });
  }

  clearAyah(ayahId: number): void {
    this.stateSignal.update((state) => ({
      ...state,
      draftWordIdsByAyahId: { ...state.draftWordIdsByAyahId, [ayahId]: [] },
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
    this.workspace.setManualWordIdsByAyahId(item.sourceKey, state.draftWordIdsByAyahId);
    return true;
  }
}
