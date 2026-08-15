import { Injectable, computed, inject, signal } from '@angular/core';

import { LinkingOperationSourceDraft } from '../models/linking-operation-draft.models';
import { LinkingManualLinkShape } from '../models/linking-manual-mushaf.models';
import { LinkingSourcePageRequest } from '../models/linking-page.models';
import { LinkingSourceDescriptor } from '../models/linking-source.models';
import { LinkingOperationDraftStore, createInlineLinkingDraft } from './linking-operation-draft.store';

interface LinkingInlineSourceState {
  readonly draft: LinkingOperationSourceDraft | null;
  readonly totalAyahCount: number;
  readonly operationGeneration: number;
}

const INITIAL_STATE: LinkingInlineSourceState = {
  draft: null,
  totalAyahCount: 0,
  operationGeneration: 0,
};

@Injectable({ providedIn: 'root' })
export class LinkingInlineSourceWorkflowController {
  private readonly drafts = inject(LinkingOperationDraftStore);
  private readonly stateSignal = signal<LinkingInlineSourceState>(INITIAL_STATE);

  readonly draft = computed(() => this.stateSignal().draft);
  readonly totalAyahCount = computed(() => this.stateSignal().totalAyahCount);
  readonly sourceRequest = computed<Omit<LinkingSourcePageRequest, 'page'> | null>(() => {
    const draft = this.stateSignal().draft;
    if (draft === null) {
      return null;
    }
    return {
      source: draft.descriptor,
      expectedLinkingDataRevision: null,
      expectedSourceViewIdentity: null,
      view: {
        segment: 'all',
        inclusionMode: null,
        ayahOverrideIds: [],
      },
      pageSize: 100,
      draftGeneration: this.stateSignal().operationGeneration,
    };
  });
  readonly selectedCount = computed(() => {
    const state = this.stateSignal();
    const draft = state.draft;
    if (draft === null) {
      return 0;
    }
    return draft.selection.mode === 'all-except'
      ? Math.max(state.totalAyahCount - draft.selection.ayahIds.length, 0)
      : draft.selection.ayahIds.length;
  });
  readonly manualGrouped = computed(() =>
    this.stateSignal().draft?.manualLinkShape === 'grouped' && this.selectedCount() > 1,
  );

  start(source: LinkingSourceDescriptor, operationGeneration: number): void {
    this.stateSignal.set({
      draft: createInlineLinkingDraft(source),
      totalAyahCount: 0,
      operationGeneration,
    });
  }

  pageReady(linkingDataRevision: number, totalAyahCount: number, doorId: number | null): void {
    const draft = this.stateSignal().draft;
    if (draft === null) {
      return;
    }
    if (draft.linkingDataRevision === linkingDataRevision && this.totalAyahCount() === totalAyahCount) {
      return;
    }
    const updated = { ...draft, linkingDataRevision };
    this.stateSignal.update((state) => ({ ...state, draft: updated, totalAyahCount }));
    this.drafts.replace([updated], linkingDataRevision, doorId);
  }

  toggleAyah(ayahId: number, doorId: number | null): void {
    this.updateDraft((draft) => {
      const overrides = new Set(draft.selection.ayahIds);
      overrides.has(ayahId) ? overrides.delete(ayahId) : overrides.add(ayahId);
      return {
        ...draft,
        selection: { ...draft.selection, ayahIds: [...overrides].sort((left, right) => left - right) },
      };
    }, doorId);
  }

  selectAllAyahs(doorId: number | null): void {
    this.updateDraft((draft) => ({
      ...draft,
      selection: { mode: 'all-except', ayahIds: [] },
    }), doorId);
  }

  clearAllAyahs(doorId: number | null): void {
    this.updateDraft((draft) => ({
      ...draft,
      selection: { mode: 'only', ayahIds: [] },
    }), doorId);
  }

  toggleManualWord(ayahId: number, quranWordId: number, doorId: number | null): void {
    this.updateDraft((draft) => {
      const selected = new Set(draft.selectedWordIdsByAyahId[ayahId] ?? []);
      selected.has(quranWordId) ? selected.delete(quranWordId) : selected.add(quranWordId);
      return {
        ...draft,
        selectedWordIdsByAyahId: {
          ...draft.selectedWordIdsByAyahId,
          [ayahId]: [...selected].sort((left, right) => left - right),
        },
      };
    }, doorId);
  }

  setAutomaticWords(enabled: boolean, doorId: number | null): void {
    this.updateDraft((draft) => ({ ...draft, automaticWordMatchesEnabled: enabled }), doorId);
  }

  setManualLinkShape(linkShape: LinkingManualLinkShape, doorId: number | null): void {
    this.updateDraft((draft) => ({ ...draft, manualLinkShape: linkShape }), doorId);
  }

  requireDraft(): LinkingOperationSourceDraft {
    const draft = this.stateSignal().draft;
    if (draft === null) {
      throw new Error('إعداد المصدر المباشر غير متاح.');
    }
    return draft;
  }

  invalidate(operationGeneration: number): void {
    const draft = this.stateSignal().draft;
    this.stateSignal.set({
      draft: draft === null
        ? null
        : {
            ...draft,
            linkingDataRevision: 0,
            selection: { mode: 'all-except', ayahIds: [] },
            selectedWordIdsByAyahId: {},
          },
      totalAyahCount: 0,
      operationGeneration,
    });
  }

  reset(operationGeneration: number): void {
    this.stateSignal.set({ ...INITIAL_STATE, operationGeneration });
  }

  private updateDraft(
    update: (draft: LinkingOperationSourceDraft) => LinkingOperationSourceDraft,
    doorId: number | null,
  ): void {
    const draft = this.stateSignal().draft;
    if (draft === null) {
      return;
    }
    const updated = update(draft);
    this.stateSignal.update((state) => ({ ...state, draft: updated }));
    if (updated.linkingDataRevision > 0) {
      this.drafts.replace([updated], updated.linkingDataRevision, doorId);
    }
  }
}
