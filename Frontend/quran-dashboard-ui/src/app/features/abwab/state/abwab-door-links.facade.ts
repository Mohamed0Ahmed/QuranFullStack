import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Injectable, computed, effect, inject, untracked } from '@angular/core';
import { Subscription } from 'rxjs';

import { AbwabDoorLinksApi } from '../data-access/abwab-door-links.api';
import { ABWAB_LABELS } from '../models/abwab.labels';
import { AbwabSnapshotFacade } from './abwab-snapshot.facade';
import { AbwabDoorLinkEditController } from './abwab-door-link-edit.controller';
import { AbwabDoorLinksStore } from './abwab-door-links.store';
import { ACTIVE_OWNER, AbwabMutationOutcome, AbwabMutationPolicy } from './abwab-mutation.policy';

@Injectable({ providedIn: 'root' })
export class AbwabDoorLinksFacade {
  private readonly api = inject(AbwabDoorLinksApi);
  private readonly store = inject(AbwabDoorLinksStore);
  private readonly tree = inject(AbwabSnapshotFacade);
  private readonly editController = inject(AbwabDoorLinkEditController);
  private readonly mutationPolicy = inject(AbwabMutationPolicy);
  private snapshotRequest: Subscription | null = null;
  private mutationRequest: Subscription | null = null;
  private generation = 0;
  private observedTreeDoorId: number | null = null;
  private observedTreeDoorVersion: number | null = null;

  readonly state = this.store.state;
  readonly openDoorId = this.store.openDoorId;
  readonly doorVersion = this.store.doorVersion;
  readonly recordViews = this.store.recordViews;
  readonly selectedCount = this.store.selectedCount;
  readonly interactionLocked = computed(() =>
    this.state().edit.status === 'saving'
    || this.state().deletion.status === 'writing'
    || this.state().copy.open && this.state().copy.status !== 'choosing',
  );

  constructor() {
    effect(() => {
      const doorId = this.openDoorId();
      const panelDoorVersion = this.doorVersion();
      const treeDoorVersion = doorId === null
        ? null
        : this.tree.snapshot()?.byId.get(doorId)?.version ?? null;
      const interactionLocked = this.interactionLocked();

      untracked(() => this.reconcileTreeDoorVersion(
        doorId,
        panelDoorVersion,
        treeDoorVersion,
        interactionLocked,
      ));
    });
  }

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

  startEdit(unitId: number): boolean {
    const recordView = this.recordViews().find((candidate) => candidate.summary.unitId === unitId) ?? null;
    const state = this.state();
    if (
      recordView === null
      || state.openDoorId === null
      || state.doorVersion === null
      || state.deletion.status === 'writing'
      || state.deletion.confirmationOpen
      || state.copy.open
      || !['idle', 'load-error'].includes(state.edit.status)
    ) {
      return false;
    }
    this.editController.start(
      state.doorVersion,
      unitId,
      recordView.ayahs,
    );
    return true;
  }

  retryEdit(): void {
    const unitId = this.state().edit.unitId;
    if (unitId !== null) {
      this.startEdit(unitId);
    }
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
    const { openDoorId, edit } = state;
    this.mutationRequest = this.mutationPolicy.execute(
      ACTIVE_OWNER,
      () => this.api.replaceWords(openDoorId, edit.unitId!, {
        expectedDoorVersion: edit.expectedDoorVersion!,
        selectedWords: edit.ayahs.flatMap((ayah) =>
          ayah.selectedWordIds.map((quranWordId) => ({ ayahId: ayah.ayahId, quranWordId })),
        ),
      }),
    ).subscribe((outcome) => this.handleMutationOutcome('edit', outcome, generation));
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
    const { openDoorId, doorVersion } = state;
    this.mutationRequest = this.mutationPolicy.execute(
      ACTIVE_OWNER,
      () => this.api.deleteLinks(openDoorId, {
        expectedDoorVersion: doorVersion,
        selectionMode: state.selection.mode === 'all-except' ? 'all_except' : 'only',
        unitIds: [...state.selection.unitIds],
      }),
    ).subscribe((outcome) => this.handleMutationOutcome('delete', outcome, generation));
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

  private handleMutationOutcome(
    kind: 'edit' | 'delete',
    outcome: AbwabMutationOutcome<{ readonly doorVersion: number }>,
    generation: number,
  ): void {
    if (!this.isCurrent(generation)) {
      return;
    }
    const failureFallback = kind === 'edit'
      ? ABWAB_LABELS.doorLinkWordsSaveError
      : ABWAB_LABELS.doorLinksDeleteError;
    if (outcome.kind !== 'success') {
      const message = outcome.message;
      if (outcome.kind === 'conflict' && isDoorLinkStaleCode(outcome.conflictCode)) {
        this.store.markStale(message || ABWAB_LABELS.doorLinksStale);
        this.loadSnapshot(true);
      } else if (kind === 'edit') {
        this.store.setEditWriteState('save-error', message);
      } else {
        this.store.setDeleteWriteState('error', message);
      }
      return;
    }
    if (outcome.data === null) {
      kind === 'edit'
        ? this.store.setEditWriteState('save-error', outcome.envelope?.message ?? failureFallback)
        : this.store.setDeleteWriteState('error', outcome.envelope?.message ?? failureFallback);
      return;
    }
    const announcement = kind === 'edit'
      ? ABWAB_LABELS.doorLinkWordsUpdatedAnnouncement
      : ABWAB_LABELS.doorLinksDeletedAnnouncement;
    this.completeMutation(outcome.data.doorVersion, outcome.envelope?.message ?? announcement);
  }

  private completeMutation(doorVersion: number, message: string): void {
    this.store.completeMutation(doorVersion, message);
    this.tree.refresh();
    this.loadSnapshot();
  }

  private reconcileTreeDoorVersion(
    doorId: number | null,
    panelDoorVersion: number | null,
    treeDoorVersion: number | null,
    interactionLocked: boolean,
  ): void {
    if (doorId === null || treeDoorVersion === null) {
      this.observedTreeDoorId = doorId;
      this.observedTreeDoorVersion = treeDoorVersion;
      return;
    }
    if (this.observedTreeDoorId !== doorId) {
      this.observedTreeDoorId = doorId;
      this.observedTreeDoorVersion = treeDoorVersion;
      return;
    }
    if (panelDoorVersion === null || treeDoorVersion === this.observedTreeDoorVersion) {
      return;
    }
    if (panelDoorVersion === treeDoorVersion) {
      this.observedTreeDoorVersion = treeDoorVersion;
      return;
    }
    if (interactionLocked) {
      return;
    }

    this.observedTreeDoorVersion = treeDoorVersion;
    this.store.markStale(ABWAB_LABELS.doorLinksStale);
    this.loadSnapshot(true);
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

function isDoorLinkStaleCode(code: string | null): boolean {
  return code === 'DOOR_LINKS_STALE' || code === 'LINKING_DATA_STALE';
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
