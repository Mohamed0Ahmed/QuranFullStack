import { Injectable, computed, signal } from '@angular/core';

import { LinkingPreparedSourceBody } from '../../../core/api/generated/models/linking-prepared-source-body';
import { CreateLinkingPreparedPreflightBody } from '../../../core/api/generated/models/create-linking-prepared-preflight-body';
import {
  LinkingAyahIdSelection,
  LinkingOperationDraft,
  LinkingOperationSourceDraft,
} from '../models/linking-operation-draft.models';
import { LinkingSourceDescriptor } from '../models/linking-source.models';
import { LinkingWorkspaceItem } from '../models/linking-workspace.models';
import { toLinkingSourceDescriptorBody } from '../utils/linking-source-descriptor-body';

const EMPTY_DRAFT: LinkingOperationDraft = {
  generation: 0,
  linkingDataRevision: null,
  doorId: null,
  sourceOrder: [],
  sources: {},
};

@Injectable({ providedIn: 'root' })
export class LinkingOperationDraftStore {
  private readonly draftSignal = signal<LinkingOperationDraft>(EMPTY_DRAFT);

  readonly draft = this.draftSignal.asReadonly();
  readonly generation = computed(() => this.draftSignal().generation);
  readonly linkingDataRevision = computed(() => this.draftSignal().linkingDataRevision);
  readonly sourceCount = computed(() => this.draftSignal().sourceOrder.length);

  replace(
    sources: readonly LinkingOperationSourceDraft[],
    linkingDataRevision: number | null,
    doorId: number | null,
  ): void {
    const generation = this.draftSignal().generation + 1;
    this.draftSignal.set({
      generation,
      linkingDataRevision,
      doorId,
      sourceOrder: sources.map((source) => source.sourceKey),
      sources: Object.freeze(Object.fromEntries(sources.map((source) => [source.sourceKey, freezeSource(source)]))),
    });
  }

  updateSelection(sourceKey: string, selection: LinkingAyahIdSelection): void {
    this.updateSource(sourceKey, (source) => ({
      ...source,
      selection: freezeSelection(selection),
    }));
  }

  setWordSelected(sourceKey: string, ayahId: number, wordId: number, selected: boolean): void {
    this.updateSource(sourceKey, (source) => {
      const wordIds = new Set(source.selectedWordIdsByAyahId[ayahId] ?? []);
      selected ? wordIds.add(wordId) : wordIds.delete(wordId);
      return {
        ...source,
        selectedWordIdsByAyahId: Object.freeze({
          ...source.selectedWordIdsByAyahId,
          [ayahId]: Object.freeze([...wordIds].sort((left, right) => left - right)),
        }),
      };
    });
  }

  setDoor(doorId: number | null): void {
    this.draftSignal.update((draft) => ({ ...draft, doorId }));
  }

  requireFreshGeneration(): void {
    const nextGeneration = this.draftSignal().generation + 1;
    this.draftSignal.set({ ...EMPTY_DRAFT, generation: nextGeneration });
  }

  reset(): void {
    this.requireFreshGeneration();
  }

  private updateSource(
    sourceKey: string,
    update: (source: LinkingOperationSourceDraft) => LinkingOperationSourceDraft,
  ): void {
    this.draftSignal.update((draft) => {
      const source = draft.sources[sourceKey];
      if (source === undefined) {
        return draft;
      }
      return {
        ...draft,
        sources: Object.freeze({ ...draft.sources, [sourceKey]: freezeSource(update(source)) }),
      };
    });
  }
}

export function createInlineLinkingDraft(source: LinkingSourceDescriptor): LinkingOperationSourceDraft {
  const manual = source.kind === 'manual-mushaf-ayahs';
  return {
    sourceKey: `inline:${crypto.randomUUID()}`,
    sourceId: null,
    sourceVersion: null,
    linkingDataRevision: 0,
    descriptor: source,
    label: source.label,
    selection: { mode: 'all-except', ayahIds: [] },
    selectedWordIdsByAyahId: {},
    descriptions: [],
    automaticWordMatchesEnabled: manual ? null : true,
    manualLinkShape: manual ? 'independent' : null,
  };
}

export function toInlinePreparedSource(
  draft: LinkingOperationSourceDraft,
  orderValue: number,
): LinkingPreparedSourceBody {
  return {
    orderValue,
    workspaceSource: null,
    inlineSource: {
      descriptor: toLinkingSourceDescriptorBody(draft.descriptor),
      configuration: {
        inclusionMode: draft.selection.mode === 'all-except' ? 'all_except' : 'only',
        ayahOverrideIds: [...draft.selection.ayahIds],
        selectedWords: Object.entries(draft.selectedWordIdsByAyahId).flatMap(
          ([ayahId, wordIds]) => wordIds.map((quranWordId) => ({ ayahId: Number(ayahId), quranWordId })),
        ),
        automaticWordMatchesEnabled: draft.automaticWordMatchesEnabled,
        manualLinkShape: draft.manualLinkShape,
        descriptions: draft.descriptions.map((description) => ({ ...description })),
      },
    },
  };
}

export function createPreparedLinkingRequest(
  preparationKey: string,
  doorId: number,
  directDraft: LinkingOperationSourceDraft | null,
  workspaceItems: readonly LinkingWorkspaceItem[],
  checkedSourceKeys: readonly string[],
): CreateLinkingPreparedPreflightBody {
  const checked = new Set(checkedSourceKeys);
  const selectedItems = workspaceItems.filter((item) => checked.has(item.sourceKey));
  const sources = directDraft === null
    ? selectedItems.flatMap((item, index): LinkingPreparedSourceBody[] =>
        item.sourceId === null || item.sourceVersion === null
          ? []
          : [{
              orderValue: index + 1,
              workspaceSource: { sourceId: item.sourceId, sourceVersion: item.sourceVersion },
              inlineSource: null,
            }],
      )
    : [toInlinePreparedSource(directDraft, 1)];
  const revisions = new Set(
    directDraft === null
      ? selectedItems.flatMap((item) =>
          item.linkingDataRevision === null ? [] : [item.linkingDataRevision],
        )
      : [directDraft.linkingDataRevision],
  );
  return {
    preparationKey,
    doorId,
    expectedLinkingDataRevision: revisions.size === 1 ? [...revisions][0]! : null,
    sources,
  };
}

function freezeSource(source: LinkingOperationSourceDraft): LinkingOperationSourceDraft {
  return Object.freeze({
    ...source,
    selection: freezeSelection(source.selection),
    descriptions: Object.freeze(source.descriptions.map((description) => Object.freeze({ ...description }))),
    selectedWordIdsByAyahId: Object.freeze(
      Object.fromEntries(
        Object.entries(source.selectedWordIdsByAyahId).map(([ayahId, wordIds]) => [
          ayahId,
          Object.freeze([...wordIds]),
        ]),
      ),
    ),
  });
}

function freezeSelection(selection: LinkingAyahIdSelection): LinkingAyahIdSelection {
  return Object.freeze({ ...selection, ayahIds: Object.freeze([...selection.ayahIds]) });
}
