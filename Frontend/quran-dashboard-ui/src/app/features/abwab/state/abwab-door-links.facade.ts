import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Injectable, computed, inject } from '@angular/core';
import { Subscription } from 'rxjs';

import { DoorLinkRecordSummaryDto } from '../../../core/api/generated/models/door-link-record-summary-dto';
import { AbwabDoorLinksApi } from '../data-access/abwab-door-links.api';
import {
  ABWAB_DOOR_LINK_AYAH_PAGE_SIZE,
  ABWAB_DOOR_LINK_COPY_BATCH_SIZE,
  ABWAB_DOOR_LINK_RECORD_PAGE_SIZE,
  AbwabDoorLinkCopyRecord,
  AbwabDoorLinkCopyScope,
} from '../models/abwab-door-links.models';
import { ABWAB_LABELS } from '../models/abwab.labels';
import { AbwabSnapshotFacade } from './abwab-snapshot.facade';
import { AbwabDoorLinkEditController } from './abwab-door-link-edit.controller';
import { AbwabDoorLinksStore } from './abwab-door-links.store';
import {
  mapAbwabDoorLinkCopyRecords,
  partitionAbwabDoorLinkCopyUnits,
} from './abwab-door-link-copy.mapper';

@Injectable({ providedIn: 'root' })
export class AbwabDoorLinksFacade {
  private readonly api = inject(AbwabDoorLinksApi);
  private readonly store = inject(AbwabDoorLinksStore);
  private readonly tree = inject(AbwabSnapshotFacade);
  private readonly editController = inject(AbwabDoorLinkEditController);
  private recordsRequest: Subscription | null = null;
  private ayahsRequest: Subscription | null = null;
  private mutationRequest: Subscription | null = null;
  private generation = 0;

  readonly state = this.store.state;
  readonly openDoorId = this.store.openDoorId;
  readonly doorVersion = this.store.doorVersion;
  readonly records = this.store.records;
  readonly hasMoreRecords = this.store.hasMoreRecords;
  readonly expandedAyahs = this.store.expandedAyahs;
  readonly hasMoreExpandedAyahs = this.store.hasMoreExpandedAyahs;
  readonly selectedCount = this.store.selectedCount;
  readonly interactionLocked = computed(() =>
    this.state().edit.status === 'saving' || this.state().deletion.status === 'writing',
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
    this.loadRecords(1);
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
    this.loadRecords(1, true);
  }

  retryRecords(): void {
    if (this.interactionLocked()) {
      return;
    }
    this.editController.cancel();
    const page = Math.max(this.state().records.requestedPage, 1);
    this.loadRecords(page, page === 1);
  }

  loadNextRecords(): void {
    const records = this.state().records;
    if (
      this.interactionLocked()
      || !this.hasMoreRecords()
      || records.status === 'loading'
      || records.status === 'refreshing'
    ) {
      return;
    }
    this.loadRecords(Math.max(...Object.keys(records.pages).map(Number), 0) + 1);
  }

  toggleExpanded(record: DoorLinkRecordSummaryDto): void {
    if (this.interactionLocked()) {
      return;
    }
    this.editController.cancel();
    this.ayahsRequest?.unsubscribe();
    const wasExpanded = this.state().expanded?.unitId === record.unitId;
    this.store.expand(record.unitId, record.isGrouped);
    if (!wasExpanded) {
      this.loadAyahs(1);
    }
  }

  retryExpandedAyahs(): void {
    const page = Math.max(this.state().expanded?.requestedPage ?? 1, 1);
    this.loadAyahs(page);
  }

  loadNextExpandedAyahs(): void {
    const expanded = this.state().expanded;
    if (
      expanded === null ||
      !this.hasMoreExpandedAyahs() ||
      expanded.status === 'loading' ||
      expanded.status === 'refreshing'
    ) {
      return;
    }
    this.loadAyahs(Math.max(...Object.keys(expanded.pages).map(Number), 0) + 1);
  }

  toggleSelected(unitId: number): void {
    if (this.interactionLocked()) {
      return;
    }
    this.editController.cancel();
    this.store.toggleSelected(unitId);
  }

  selectPage(page: number): void {
    if (this.interactionLocked()) {
      return;
    }
    this.editController.cancel();
    const items = this.state().records.pages[page]?.items ?? [];
    this.store.selectUnits(items.map((record) => record.unitId));
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
    const state = this.state();
    if (
      record === null
      || state.openDoorId === null
      || state.doorVersion === null
      || state.deletion.status === 'writing'
      || !['idle', 'load-error'].includes(state.edit.status)
    ) {
      return false;
    }
    this.ayahsRequest?.unsubscribe();
    this.ayahsRequest = null;
    this.store.collapseExpanded();
    this.editController.start(
      state.openDoorId,
      state.doorVersion,
      record.unitId,
      () => this.loadRecords(1, true),
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

  beginCopy(scope: AbwabDoorLinkCopyScope): void {
    this.store.openCopy(scope);
    if (scope === 'selected' && this.state().selection.mode === 'only') {
      this.store.setCopyBatches(partitionAbwabDoorLinkCopyUnits(this.state().selection.unitIds));
    }
  }

  setCopyTarget(targetDoorId: number | null): void {
    const sourceDoorId = this.openDoorId();
    this.store.setCopyTarget(targetDoorId === sourceDoorId ? null : targetDoorId);
  }

  queueCopyUnits(unitIds: readonly number[]): void {
    this.store.setCopyBatches(partitionAbwabDoorLinkCopyUnits(unitIds));
  }

  prepareCopyBatch(
    batchNumber: number,
    records: readonly AbwabDoorLinkCopyRecord[],
    sourceLabel: string,
  ): boolean {
    const sources = mapAbwabDoorLinkCopyRecords(records, sourceLabel);
    if (sources.length > ABWAB_DOOR_LINK_COPY_BATCH_SIZE) {
      this.store.updateCopyBatch(batchNumber, { status: 'error', errorMessage: ABWAB_LABELS.writeInvalidFallback });
      return false;
    }
    this.store.updateCopyBatch(batchNumber, { sources, status: 'ready', errorMessage: null });
    return true;
  }

  markCopyBatchPreparing(batchNumber: number): void {
    this.store.updateCopyBatch(batchNumber, { status: 'preparing', errorMessage: null });
  }

  markCopyBatchCompleted(batchNumber: number): void {
    this.store.updateCopyBatch(batchNumber, { status: 'completed', errorMessage: null });
  }

  failCopyBatch(batchNumber: number, message: string): void {
    this.store.updateCopyBatch(batchNumber, { status: 'error', errorMessage: message });
    this.store.addCopyError(message);
  }

  setCurrentCopyBatch(batchNumber: number): void {
    this.store.setCurrentCopyBatch(batchNumber);
  }

  closeCopy(): void {
    this.store.closeCopy();
  }

  clearNotice(): void {
    this.store.clearNotice();
  }

  private loadRecords(page: number, refreshing = false): void {
    const doorId = this.openDoorId();
    if (doorId === null || page < 1) {
      return;
    }
    const expectedDoorVersion = page === 1 ? null : this.doorVersion();
    if (page > 1 && expectedDoorVersion === null) {
      return;
    }
    this.recordsRequest?.unsubscribe();
    this.store.beginRecordsLoad(page, refreshing);
    const generation = this.generation;
    this.recordsRequest = this.api.getRecords(doorId, {
      page,
      pageSize: ABWAB_DOOR_LINK_RECORD_PAGE_SIZE,
      expectedDoorVersion,
    }).subscribe({
      next: (response) => {
        if (!this.isCurrent(generation)) {
          return;
        }
        if (response.isSuccess && response.data != null) {
          this.store.receiveRecords(response.data);
        } else {
          this.store.failRecords(response.message ?? ABWAB_LABELS.doorLinksLoadError);
        }
      },
      error: (error: unknown) => this.handleReadError('records', error, generation),
    });
  }

  private loadAyahs(page: number): void {
    const state = this.state();
    if (state.openDoorId === null || state.doorVersion === null || state.expanded === null || page < 1) {
      return;
    }
    const expectedLinkingDataRevision = page === 1 ? null : state.expanded.linkingDataRevision;
    if (page > 1 && expectedLinkingDataRevision === null) {
      return;
    }
    this.ayahsRequest?.unsubscribe();
    this.store.beginAyahsLoad(page);
    const generation = this.generation;
    const unitId = state.expanded.unitId;
    this.ayahsRequest = this.api.getAyahs(state.openDoorId, unitId, {
      page,
      pageSize: ABWAB_DOOR_LINK_AYAH_PAGE_SIZE,
      expectedDoorVersion: state.doorVersion,
      expectedLinkingDataRevision,
    }).subscribe({
      next: (response) => {
        if (!this.isCurrent(generation)) {
          return;
        }
        if (response.isSuccess && response.data != null) {
          this.store.receiveAyahs(response.data);
        } else {
          this.store.failAyahs(response.message ?? ABWAB_LABELS.doorLinkAyahsLoadError);
        }
      },
      error: (error: unknown) => this.handleReadError('ayahs', error, generation),
    });
  }

  private handleReadError(kind: 'records' | 'ayahs', error: unknown, generation: number): void {
    if (!this.isCurrent(generation)) {
      return;
    }
    const message = doorLinkResponseMessage(error) ?? (
      kind === 'records' ? ABWAB_LABELS.doorLinksLoadError : ABWAB_LABELS.doorLinkAyahsLoadError
    );
    if (isDoorLinkStaleResponse(error)) {
      this.ayahsRequest?.unsubscribe();
      this.store.markStale(message || ABWAB_LABELS.doorLinksStale);
      this.loadRecords(1, true);
      return;
    }
    kind === 'records' ? this.store.failRecords(message) : this.store.failAyahs(message);
  }

  private handleMutationError(kind: 'edit' | 'delete', error: unknown, generation: number): void {
    if (!this.isCurrent(generation)) {
      return;
    }
    const fallback = kind === 'edit' ? ABWAB_LABELS.doorLinkWordsSaveError : ABWAB_LABELS.doorLinksDeleteError;
    const message = doorLinkResponseMessage(error) ?? fallback;
    if (isDoorLinkStaleResponse(error)) {
      this.store.markStale(message || ABWAB_LABELS.doorLinksStale);
      this.loadRecords(1, true);
      return;
    }
    kind === 'edit'
      ? this.store.setEditWriteState('save-error', message)
      : this.store.setDeleteWriteState('error', message);
  }

  private completeMutation(doorVersion: number, message: string): void {
    this.store.completeMutation(doorVersion, message);
    this.tree.refresh();
    this.loadRecords(1);
  }

  private isCurrent(generation: number): boolean {
    return generation === this.generation && this.openDoorId() !== null;
  }

  private cancelRequests(): void {
    this.editController.cancel();
    this.recordsRequest?.unsubscribe();
    this.ayahsRequest?.unsubscribe();
    this.mutationRequest?.unsubscribe();
    this.recordsRequest = null;
    this.ayahsRequest = null;
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
