import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Injectable, computed, inject } from '@angular/core';
import { Subscription } from 'rxjs';

import { AbwabDoorLinksApi } from '../data-access/abwab-door-links.api';
import { ABWAB_LABELS } from '../models/abwab.labels';
import { AbwabSnapshotFacade } from './abwab-snapshot.facade';
import { AbwabDoorLinkEditController } from './abwab-door-link-edit.controller';
import { AbwabDoorLinksStore } from './abwab-door-links.store';

@Injectable({ providedIn: 'root' })
export class AbwabDoorLinksFacade {
  private readonly api = inject(AbwabDoorLinksApi);
  private readonly store = inject(AbwabDoorLinksStore);
  private readonly tree = inject(AbwabSnapshotFacade);
  private readonly editController = inject(AbwabDoorLinkEditController);
  private snapshotRequest: Subscription | null = null;
  private mutationRequest: Subscription | null = null;
  private generation = 0;

  readonly state = this.store.state;
  readonly openDoorId = this.store.openDoorId;
  readonly doorVersion = this.store.doorVersion;
  readonly recordViews = this.store.recordViews;
  readonly records = this.store.records;
  readonly selectedCount = this.store.selectedCount;
  readonly interactionLocked = computed(() =>
    this.state().edit.status === 'saving'
    || this.state().deletion.status === 'writing'
    || this.state().copy.open && this.state().copy.status !== 'choosing',
  );
  readonly selectedRecord = computed(() => {
    const state = this.state();
    const selection = state.selection;
    if (this.selectedCount() !== 1) {
      return null;
    }
    if (selection.mode === 'only') {
      const unitId = selection.unitIds[0];
      return this.records().find((record) => record.unitId === unitId) ?? null;
    }
    const records = this.records();
    if (records.length !== state.records.totalCount) {
      return null;
    }
    const excluded = new Set(selection.unitIds);
    return records.find((record) => !excluded.has(record.unitId)) ?? null;
  });

  toggleDoor(doorId: number): void {
    if (this.interactionLocked()) {
      return;
    }
    if (this.openDoorId() === doorId) {
      this.close();
      return;
    }
    this.openDoor(doorId);
  }

  openDoor(doorId: number): void {
    this.cancelRequests();
    this.generation++;
    this.store.open(doorId);
    this.loadSnapshot();
  }

  close(): void {
    this.cancelRequests();
    this.generation++;
    this.store.close();
  }

  refresh(): void {
    if (this.interactionLocked()) {
      return;
    }
    this.editController.cancel();
    this.loadSnapshot(true);
  }

  retryRecords(): void {
    if (this.interactionLocked()) {
      return;
    }
    this.editController.cancel();
    this.loadSnapshot(true);
  }

  toggleSelected(unitId: number): void {
    if (this.interactionLocked()) {
      return;
    }
    this.editController.cancel();
    this.store.toggleSelected(unitId);
  }

  selectAll(): void {
    if (this.interactionLocked()) {
      return;
    }
    this.editController.cancel();
    this.store.setSelectionMode('all-except');
  }

  clearSelection(): void {
    if (this.interactionLocked()) {
      return;
    }
    this.editController.cancel();
    this.store.clearSelection();
  }

  startEdit(): boolean {
    const record = this.selectedRecord();
    const recordView = record === null
      ? null
      : this.recordViews().find((candidate) => candidate.summary.unitId === record.unitId) ?? null;
    const state = this.state();
    if (
      record === null
      || recordView === null
      || state.openDoorId === null
      || state.doorVersion === null
      || state.deletion.status === 'writing'
      || !['idle', 'load-error'].includes(state.edit.status)
    ) {
      return false;
    }
    this.editController.start(
      state.doorVersion,
      record.unitId,
      recordView.ayahs,
    );
    return true;
  }

  retryEdit(): void {
    this.startEdit();
  }

  cancelEdit(): void {
    if (this.state().edit.status !== 'saving') {
      this.editController.cancel();
    }
  }

  setEditWord(ayahId: number, quranWordId: number, selected: boolean): void {
    this.store.setEditWord(ayahId, quranWordId, selected);
  }

  saveEdit(): void {
    const state = this.state();
    if (
      state.openDoorId === null ||
      state.edit.unitId === null ||
      state.edit.expectedDoorVersion === null ||
      !['ready', 'save-error'].includes(state.edit.status)
    ) {
      return;
    }
    this.mutationRequest?.unsubscribe();
    this.store.setEditWriteState('saving', null);
    const generation = this.generation;
    this.mutationRequest = this.api.replaceWords(state.openDoorId, state.edit.unitId, {
      expectedDoorVersion: state.edit.expectedDoorVersion,
      selectedWords: state.edit.ayahs.flatMap((ayah) =>
        ayah.selectedWordIds.map((quranWordId) => ({ ayahId: ayah.ayahId, quranWordId })),
      ),
    }).subscribe({
      next: (response) => {
        if (!this.isCurrent(generation)) {
          return;
        }
        if (!response.isSuccess || response.data == null) {
          this.store.setEditWriteState('save-error', response.message ?? ABWAB_LABELS.doorLinkWordsSaveError);
          return;
        }
        this.completeMutation(response.data.doorVersion, response.message ?? ABWAB_LABELS.doorLinkWordsUpdatedAnnouncement);
      },
      error: (error: unknown) => this.handleMutationError('edit', error, generation),
    });
  }

  requestDelete(): void {
    if (
      this.selectedCount() > 0
      && this.state().edit.status === 'idle'
      && this.state().deletion.status !== 'writing'
    ) {
      this.store.openDeleteConfirmation();
    }
  }

  cancelDelete(): void {
    if (this.state().deletion.status !== 'writing') {
      this.store.closeDeleteConfirmation();
    }
  }

  confirmDelete(): void {
    const state = this.state();
    if (
      state.openDoorId === null ||
      state.doorVersion === null ||
      this.selectedCount() === 0 ||
      state.deletion.status === 'writing'
    ) {
      return;
    }
    this.mutationRequest?.unsubscribe();
    this.store.setDeleteWriteState('writing', null);
    const generation = this.generation;
    this.mutationRequest = this.api.deleteLinks(state.openDoorId, {
      expectedDoorVersion: state.doorVersion,
      selectionMode: state.selection.mode === 'all-except' ? 'all_except' : 'only',
      unitIds: [...state.selection.unitIds],
    }).subscribe({
      next: (response) => {
        if (!this.isCurrent(generation)) {
          return;
        }
        if (!response.isSuccess || response.data == null) {
          this.store.setDeleteWriteState('error', response.message ?? ABWAB_LABELS.doorLinksDeleteError);
          return;
        }
        this.completeMutation(response.data.doorVersion, response.message ?? ABWAB_LABELS.doorLinksDeletedAnnouncement);
      },
      error: (error: unknown) => this.handleMutationError('delete', error, generation),
    });
  }

  clearNotice(): void {
    this.store.clearNotice();
  }

  private loadSnapshot(refreshing = false): void {
    const doorId = this.openDoorId();
    if (doorId === null) {
      return;
    }
    this.snapshotRequest?.unsubscribe();
    this.store.beginSnapshotLoad(refreshing);
    const generation = this.generation;
    this.snapshotRequest = this.api.getSnapshot(doorId).subscribe({
      next: (response) => {
        if (!this.isCurrent(generation)) {
          return;
        }
        if (response.isSuccess && response.data != null && response.data.doorId === doorId) {
          try {
            this.store.receiveSnapshot(response.data);
          } catch {
            this.store.failSnapshot(ABWAB_LABELS.doorLinksLoadError);
          }
        } else {
          this.store.failSnapshot(response.message ?? ABWAB_LABELS.doorLinksLoadError);
        }
      },
      error: (error: unknown) => this.handleReadError(error, generation),
    });
  }

  private handleReadError(error: unknown, generation: number): void {
    if (!this.isCurrent(generation)) {
      return;
    }
    const message = doorLinkResponseMessage(error) ?? ABWAB_LABELS.doorLinksLoadError;
    if (isDoorLinkStaleResponse(error)) {
      this.store.markStale(message || ABWAB_LABELS.doorLinksStale);
      this.loadSnapshot(true);
      return;
    }
    this.store.failSnapshot(message);
  }

  private handleMutationError(kind: 'edit' | 'delete', error: unknown, generation: number): void {
    if (!this.isCurrent(generation)) {
      return;
    }
    const fallback = kind === 'edit' ? ABWAB_LABELS.doorLinkWordsSaveError : ABWAB_LABELS.doorLinksDeleteError;
    const message = doorLinkResponseMessage(error) ?? fallback;
    if (isDoorLinkStaleResponse(error)) {
      this.store.markStale(message || ABWAB_LABELS.doorLinksStale);
      this.loadSnapshot(true);
      return;
    }
    kind === 'edit'
      ? this.store.setEditWriteState('save-error', message)
      : this.store.setDeleteWriteState('error', message);
  }

  private completeMutation(doorVersion: number, message: string): void {
    this.store.completeMutation(doorVersion, message);
    this.tree.refresh();
    this.loadSnapshot();
  }

  private isCurrent(generation: number): boolean {
    return generation === this.generation && this.openDoorId() !== null;
  }

  private cancelRequests(): void {
    this.editController.cancel();
    this.snapshotRequest?.unsubscribe();
    this.mutationRequest?.unsubscribe();
    this.snapshotRequest = null;
    this.mutationRequest = null;
  }
}

function doorLinkResponseMessage(error: unknown): string | null {
  if (!(error instanceof HttpErrorResponse) || typeof error.error !== 'object' || error.error === null) {
    return null;
  }
  const message = (error.error as Record<string, unknown>)['message'];
  return typeof message === 'string' && message.trim().length > 0 ? message : null;
}

function isDoorLinkStaleResponse(error: unknown): boolean {
  if (!(error instanceof HttpErrorResponse) || error.status !== HttpStatusCode.Conflict) {
    return false;
  }
  const envelope = typeof error.error === 'object' && error.error !== null
    ? error.error as Record<string, unknown>
    : null;
  const data = envelope !== null && typeof envelope['data'] === 'object' && envelope['data'] !== null
    ? envelope['data'] as Record<string, unknown>
    : null;
  return data?.['code'] === 'DOOR_LINKS_STALE' || data?.['code'] === 'LINKING_DATA_STALE';
}
