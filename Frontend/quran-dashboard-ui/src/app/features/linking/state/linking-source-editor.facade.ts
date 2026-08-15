import { Injectable, computed, inject, signal } from '@angular/core';

import { LinkingSourcePageRequest } from '../models/linking-page.models';
import { LinkingAccessService } from './linking-access.service';
import { LinkingWorkspaceStore } from './linking-workspace.store';

interface LinkingSourceEditorState {
  sourceKey: string | null;
  sourceLabel: string | null;
  status: 'idle' | 'preparing' | 'ready' | 'error';
  totalAyahCount: number;
  errorMessage: string | null;
  generation: number;
}

const INITIAL_STATE: LinkingSourceEditorState = {
  sourceKey: null,
  sourceLabel: null,
  status: 'idle',
  totalAyahCount: 0,
  errorMessage: null,
  generation: 0,
};

@Injectable({ providedIn: 'root' })
export class LinkingSourceEditorFacade {
  private readonly access = inject(LinkingAccessService);
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly stateSignal = signal<LinkingSourceEditorState>(INITIAL_STATE);

  readonly state = this.stateSignal.asReadonly();
  readonly currentItem = computed(() => {
    const sourceKey = this.stateSignal().sourceKey;
    return sourceKey === null ? null : this.workspace.item(sourceKey);
  });
  readonly request = computed<Omit<LinkingSourcePageRequest, 'page'> | null>(() => {
    const item = this.currentItem();
    if (item === null) {
      return null;
    }
    return {
      source: item.source,
      expectedLinkingDataRevision: null,
      expectedSourceViewIdentity: null,
      view: {
        segment: 'all',
        inclusionMode: null,
        ayahOverrideIds: [],
      },
      pageSize: 100,
      draftGeneration: this.stateSignal().generation,
    };
  });
  readonly selectedCount = computed(() => {
    const item = this.currentItem();
    const total = this.stateSignal().totalAyahCount;
    if (item === null) {
      return 0;
    }
    return item.configuration.ayahInclusion.mode === 'all-except'
      ? Math.max(total - item.ayahOverrideIds.length, 0)
      : item.ayahOverrideIds.length;
  });

  open(sourceKey: string | null): void {
    const item = sourceKey === null ? null : this.workspace.item(sourceKey);
    if (!this.access.canUseLinking() || item === null) {
      this.close();
      return;
    }
    if (this.stateSignal().sourceKey === sourceKey) {
      return;
    }
    this.stateSignal.set({
      ...INITIAL_STATE,
      sourceKey,
      sourceLabel: item.source.label,
      status: 'preparing',
      generation: this.stateSignal().generation + 1,
    });
  }

  close(): void {
    this.stateSignal.set({ ...INITIAL_STATE, generation: this.stateSignal().generation + 1 });
  }

  retry(): void {
    if (this.currentItem() !== null) {
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

  toggleAyah(ayahId: number): void {
    const sourceKey = this.stateSignal().sourceKey;
    if (sourceKey !== null) {
      this.workspace.toggleAyahId(sourceKey, ayahId);
    }
  }

  selectAll(): void {
    const sourceKey = this.stateSignal().sourceKey;
    if (sourceKey !== null) {
      this.workspace.selectAllAyahIds(sourceKey);
    }
  }

  clearAll(): void {
    const sourceKey = this.stateSignal().sourceKey;
    if (sourceKey !== null) {
      this.workspace.clearAllAyahIds(sourceKey);
    }
  }

  toggleManualWord(ayahId: number, quranWordId: number): void {
    const sourceKey = this.stateSignal().sourceKey;
    if (sourceKey !== null) {
      this.workspace.toggleManualWordId(sourceKey, ayahId, quranWordId);
    }
  }

  setAutomaticWordMatches(enabled: boolean): void {
    const item = this.currentItem();
    if (item?.configuration.kind === 'automatic') {
      this.workspace.setAutomaticWordMatchesEnabled(item.sourceKey, enabled);
    }
  }

  setManualLinkShape(linkShape: 'grouped' | 'independent'): void {
    const item = this.currentItem();
    if (item?.configuration.kind === 'manual') {
      this.workspace.setManualLinkShape(item.sourceKey, linkShape);
    }
  }

}
