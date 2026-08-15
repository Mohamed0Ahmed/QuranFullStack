import { Injectable, computed, signal } from '@angular/core';

import { DoorLinkAyahDto } from '../../../core/api/generated/models/door-link-ayah-dto';
import { DoorLinkSnapshotDto } from '../../../core/api/generated/models/door-link-snapshot-dto';
import {
  AbwabDoorLinkCopyBatch,
  AbwabDoorLinkCopyScope,
  AbwabDoorLinkSelectionState,
  AbwabDoorLinkSelectionMode,
  AbwabDoorLinksState,
  EMPTY_ABWAB_DOOR_LINK_SELECTION,
} from '../models/abwab-door-links.models';
import { mapDoorLinkSnapshot } from './abwab-door-link-snapshot.mapper';

function initialState(openDoorId: number | null = null): AbwabDoorLinksState {
  return {
    openDoorId,
    doorVersion: null,
    records: {
      status: 'idle',
      items: [],
      linkingDataRevision: null,
      totalCount: 0,
      errorMessage: null,
    },
    selection: EMPTY_ABWAB_DOOR_LINK_SELECTION,
    edit: {
      unitId: null,
      expectedDoorVersion: null,
      ayahs: [],
      status: 'idle',
      errorMessage: null,
    },
    deletion: { confirmationOpen: false, status: 'idle', errorMessage: null },
    copy: {
      open: false,
      status: 'choosing',
      scope: null,
      sourceDoorId: null,
      expectedSourceDoorVersion: null,
      expectedLinkingDataRevision: null,
      sourceSelection: null,
      targetDoorId: null,
      batches: [],
      currentBatchNumber: 0,
      errorMessage: null,
    },
    staleMessage: null,
    noticeMessage: null,
  };
}

function sortedUnique(values: readonly number[]): number[] {
  return [...new Set(values)].sort((left, right) => left - right);
}

@Injectable({ providedIn: 'root' })
export class AbwabDoorLinksStore {
  private readonly stateSignal = signal<AbwabDoorLinksState>(initialState());

  readonly state = this.stateSignal.asReadonly();
  readonly openDoorId = computed(() => this.stateSignal().openDoorId);
  readonly doorVersion = computed(() => this.stateSignal().doorVersion);
  readonly recordViews = computed(() => this.stateSignal().records.items);
  readonly records = computed(() => this.recordViews().map((record) => record.summary));
  readonly selectedCount = computed(() => {
    const { selection, records } = this.stateSignal();
    return selection.mode === 'only'
      ? selection.unitIds.length
      : Math.max(records.totalCount - selection.unitIds.length, 0);
  });

  open(doorId: number): boolean {
    if (this.stateSignal().openDoorId === doorId) {
      return false;
    }
    this.stateSignal.set(initialState(doorId));
    return true;
  }

  close(): void {
    this.stateSignal.set(initialState());
  }

  beginSnapshotLoad(refreshing: boolean): void {
    this.stateSignal.update((state) => ({
      ...state,
      records: {
        ...state.records,
        status: refreshing && state.records.items.length > 0 ? 'refreshing' : 'loading',
        errorMessage: null,
      },
    }));
  }

  receiveSnapshot(snapshot: DoorLinkSnapshotDto): void {
    const items = mapDoorLinkSnapshot(snapshot);
    this.stateSignal.update((state) => {
      if (state.openDoorId !== snapshot.doorId) {
        return state;
      }
      return {
        ...state,
        doorVersion: snapshot.doorVersion,
        records: {
          status: items.length === 0 ? 'empty' : 'ready',
          items,
          linkingDataRevision: snapshot.linkingDataRevision,
          totalCount: items.length,
          errorMessage: null,
        },
      };
    });
  }

  failSnapshot(message: string): void {
    this.stateSignal.update((state) => ({
      ...state,
      records: { ...state.records, status: 'error', errorMessage: message },
    }));
  }

  setSelectionMode(mode: AbwabDoorLinkSelectionMode, unitIds: readonly number[] = []): void {
    this.stateSignal.update((state) => ({
      ...state,
      selection: { mode, unitIds: sortedUnique(unitIds) },
      edit: initialState().edit,
      deletion: initialState().deletion,
    }));
  }

  toggleSelected(unitId: number): void {
    this.stateSignal.update((state) => {
      const ids = new Set(state.selection.unitIds);
      ids.has(unitId) ? ids.delete(unitId) : ids.add(unitId);
      return {
        ...state,
        selection: { ...state.selection, unitIds: sortedUnique([...ids]) },
        edit: initialState().edit,
        deletion: initialState().deletion,
      };
    });
  }

  selectUnits(unitIds: readonly number[]): void {
    this.stateSignal.update((state) => {
      const ids = new Set(state.selection.unitIds);
      if (state.selection.mode === 'only') {
        unitIds.forEach((unitId) => ids.add(unitId));
      } else {
        unitIds.forEach((unitId) => ids.delete(unitId));
      }
      return {
        ...state,
        selection: { ...state.selection, unitIds: sortedUnique([...ids]) },
        edit: initialState().edit,
        deletion: initialState().deletion,
      };
    });
  }

  clearSelection(): void {
    this.setSelectionMode('only');
  }

  beginEditPreparation(unitId: number, expectedDoorVersion: number): void {
    this.stateSignal.update((state) => ({
      ...state,
      edit: {
        unitId,
        expectedDoorVersion,
        ayahs: [],
        status: 'preparing',
        errorMessage: null,
      },
    }));
  }

  completeEditPreparation(
    unitId: number,
    expectedDoorVersion: number,
    ayahs: readonly DoorLinkAyahDto[],
  ): void {
    this.stateSignal.update((state) => {
      if (state.edit.unitId !== unitId || state.edit.status !== 'preparing') {
        return state;
      }
      return {
        ...state,
        edit: {
          unitId,
          expectedDoorVersion,
          ayahs: ayahs.map((ayah) => ({ ...ayah, selectedWordIds: sortedUnique(ayah.selectedWordIds) })),
          status: 'ready',
          errorMessage: null,
        },
      };
    });
  }

  failEditPreparation(unitId: number, message: string): void {
    this.stateSignal.update((state) => state.edit.unitId !== unitId ? state : ({
      ...state,
      edit: { ...state.edit, status: 'load-error', errorMessage: message },
    }));
  }

  cancelEdit(): void {
    this.stateSignal.update((state) => ({ ...state, edit: initialState().edit }));
  }

  setEditWord(ayahId: number, quranWordId: number, selected: boolean): void {
    this.stateSignal.update((state) => {
      if (state.edit.unitId === null || !['ready', 'save-error'].includes(state.edit.status)) {
        return state;
      }
      return {
        ...state,
        edit: {
          ...state.edit,
          ayahs: state.edit.ayahs.map((ayah) => {
            if (ayah.ayahId !== ayahId) {
              return ayah;
            }
            const wordIds = new Set(ayah.selectedWordIds);
            selected ? wordIds.add(quranWordId) : wordIds.delete(quranWordId);
            return { ...ayah, selectedWordIds: sortedUnique([...wordIds]) };
          }),
        },
      };
    });
  }

  setEditWriteState(status: 'saving' | 'save-error', errorMessage: string | null): void {
    this.stateSignal.update((state) => ({ ...state, edit: { ...state.edit, status, errorMessage } }));
  }

  openDeleteConfirmation(): void {
    this.stateSignal.update((state) => ({
      ...state,
      deletion: { confirmationOpen: true, status: 'idle', errorMessage: null },
    }));
  }

  closeDeleteConfirmation(): void {
    this.stateSignal.update((state) => ({ ...state, deletion: initialState().deletion }));
  }

  setDeleteWriteState(status: 'writing' | 'error', errorMessage: string | null): void {
    this.stateSignal.update((state) => ({
      ...state,
      deletion: { ...state.deletion, status, errorMessage },
    }));
  }

  openCopy(): void {
    this.stateSignal.update((state) => ({
      ...state,
      copy: { ...initialState().copy, open: true },
    }));
  }

  setCopyTarget(targetDoorId: number | null): void {
    this.stateSignal.update((state) => ({ ...state, copy: { ...state.copy, targetDoorId } }));
  }

  setCopyScope(scope: AbwabDoorLinkCopyScope): void {
    this.stateSignal.update((state) => ({ ...state, copy: { ...state.copy, scope } }));
  }

  beginCopyPreparation(
    sourceDoorId: number,
    expectedSourceDoorVersion: number,
    sourceSelection: AbwabDoorLinkSelectionState,
  ): void {
    this.stateSignal.update((state) => ({
      ...state,
      copy: {
        ...state.copy,
        status: 'enumerating',
        sourceDoorId,
        expectedSourceDoorVersion,
        expectedLinkingDataRevision: null,
        sourceSelection: { ...sourceSelection, unitIds: [...sourceSelection.unitIds] },
        batches: [],
        currentBatchNumber: 0,
        errorMessage: null,
      },
    }));
  }

  setCopySourceDoorVersion(expectedSourceDoorVersion: number): void {
    this.stateSignal.update((state) => ({
      ...state,
      copy: { ...state.copy, expectedSourceDoorVersion },
    }));
  }

  setCopyLinkingDataRevision(expectedLinkingDataRevision: number | null): void {
    this.stateSignal.update((state) => ({
      ...state,
      copy: { ...state.copy, expectedLinkingDataRevision },
    }));
  }

  setCopyStatus(status: 'enumerating' | 'preparing' | 'running'): void {
    this.stateSignal.update((state) => ({
      ...state,
      copy: { ...state.copy, status, errorMessage: null },
    }));
  }

  setCopyBatches(batches: readonly AbwabDoorLinkCopyBatch[]): void {
    this.stateSignal.update((state) => ({
      ...state,
      copy: { ...state.copy, batches, currentBatchNumber: batches.length === 0 ? 0 : 1, errorMessage: null },
    }));
  }

  updateCopyBatch(batchNumber: number, update: Partial<AbwabDoorLinkCopyBatch>): void {
    this.stateSignal.update((state) => ({
      ...state,
      copy: {
        ...state.copy,
        batches: state.copy.batches.map((batch) =>
          batch.batchNumber === batchNumber ? { ...batch, ...update, batchNumber } : batch,
        ),
      },
    }));
  }

  setCurrentCopyBatch(batchNumber: number): void {
    this.stateSignal.update((state) => ({
      ...state,
      copy: { ...state.copy, currentBatchNumber: batchNumber },
    }));
  }

  stopCopy(batchNumber: number, message: string): void {
    this.stateSignal.update((state) => ({
      ...state,
      copy: {
        ...state.copy,
        status: 'stopped',
        errorMessage: message,
        batches: state.copy.batches.map((batch) =>
          batch.batchNumber === batchNumber
            ? { ...batch, sources: [], status: 'error', errorMessage: message }
            : batch,
        ),
      },
    }));
  }

  closeCopy(): void {
    this.stateSignal.update((state) => ({ ...state, copy: initialState().copy }));
  }

  completeCopy(noticeMessage: string): void {
    this.stateSignal.update((state) => ({
      ...state,
      copy: initialState().copy,
      noticeMessage,
    }));
  }

  completeMutation(doorVersion: number, noticeMessage: string | null): void {
    const doorId = this.stateSignal().openDoorId;
    this.stateSignal.set({ ...initialState(doorId), doorVersion, noticeMessage });
  }

  markStale(message: string): void {
    const doorId = this.stateSignal().openDoorId;
    this.stateSignal.set({ ...initialState(doorId), staleMessage: message });
  }

  clearNotice(): void {
    this.stateSignal.update((state) => ({ ...state, noticeMessage: null, staleMessage: null }));
  }
}
