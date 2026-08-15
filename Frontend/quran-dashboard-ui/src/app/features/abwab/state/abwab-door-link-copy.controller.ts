import { Injectable, inject } from '@angular/core';

import { LinkingWorkflowFacade } from '../../linking/state/linking-workflow.facade';
import {
  ABWAB_DOOR_LINK_COPY_BATCH_SIZE,
  AbwabDoorLinkCopyBatch,
  AbwabDoorLinkSelectionState,
} from '../models/abwab-door-links.models';
import { ABWAB_LABELS } from '../models/abwab.labels';
import { AbwabSnapshotFacade } from './abwab-snapshot.facade';
import { AbwabDoorLinkCopyLoader, copyFailureMessage } from './abwab-door-link-copy.loader';
import {
  mapAbwabDoorLinkCopyRecords,
  partitionAbwabDoorLinkCopyUnits,
} from './abwab-door-link-copy.mapper';
import { AbwabDoorLinksStore } from './abwab-door-links.store';

@Injectable({ providedIn: 'root' })
export class AbwabDoorLinkCopyController {
  private readonly store = inject(AbwabDoorLinksStore);
  private readonly tree = inject(AbwabSnapshotFacade);
  private readonly workflow = inject(LinkingWorkflowFacade);
  private readonly loader = inject(AbwabDoorLinkCopyLoader);
  private generation = 0;

  readonly state = this.store.state;

  open(): void {
    const state = this.state();
    if (
      state.openDoorId === null
      || selectedCount(state.selection, state.records.totalCount) === 0
    ) {
      return;
    }
    this.generation++;
    this.store.openCopy(state.selection);
    this.tree.ensureLoaded();
  }

  close(): void {
    if (this.state().copy.status === 'running') {
      return;
    }
    this.generation++;
    this.store.closeCopy();
  }

  selectTarget(targetDoorId: number): void {
    if (this.state().copy.status !== 'choosing') {
      return;
    }
    this.store.setCopyTarget(this.isValidTarget(targetDoorId) ? targetDoorId : null);
  }

  start(): void {
    const state = this.state();
    const copy = state.copy;
    const sourceDoorId = state.openDoorId;
    const expectedSourceDoorVersion = state.doorVersion;
    if (
      !copy.open
      || copy.status !== 'choosing'
      || copy.sourceSelection === null
      || copy.targetDoorId === null
      || sourceDoorId === null
      || expectedSourceDoorVersion === null
      || !this.isValidTarget(copy.targetDoorId)
      || !this.isLiveDoor(sourceDoorId)
    ) {
      return;
    }
    const sourceSelection = {
      ...copy.sourceSelection,
      unitIds: [...copy.sourceSelection.unitIds],
    };
    if (selectedCount(sourceSelection, state.records.totalCount) === 0) {
      return;
    }
    const generation = ++this.generation;
    this.store.beginCopyPreparation(sourceDoorId, expectedSourceDoorVersion, sourceSelection);
    void this.enumerateAndPrepare(generation);
  }

  retry(): void {
    const copy = this.state().copy;
    if (
      !copy.open
      || copy.status !== 'stopped'
      || copy.sourceDoorId === null
      || copy.sourceSelection === null
      || copy.targetDoorId === null
    ) {
      return;
    }
    const generation = ++this.generation;
    this.store.setCopyStatus('enumerating');
    this.store.setCopyLinkingDataRevision(null);
    this.tree.load();
    void this.refreshAndRetry(generation);
  }

  private async enumerateAndPrepare(generation: number): Promise<void> {
    try {
      const copy = this.requireCapturedCopy();
      const unitIds = await this.loader.enumerateUnitIds(
        copy.sourceDoorId,
        copy.expectedSourceDoorVersion,
        copy.sourceSelection,
        () => this.isCurrent(generation),
      );
      if (!this.isCurrent(generation)) {
        return;
      }
      if (unitIds.length === 0) {
        throw new Error(ABWAB_LABELS.doorLinksCopyNoRecords);
      }
      this.store.setCopyBatches(partitionAbwabDoorLinkCopyUnits(unitIds));
      await this.prepareCurrentBatch(generation);
    } catch (error: unknown) {
      this.stop(generation, copyFailureMessage(error));
    }
  }

  private async refreshAndRetry(generation: number): Promise<void> {
    try {
      const copy = this.requireCapturedCopy();
      const doorVersion = await this.loader.captureDoorVersion(copy.sourceDoorId);
      if (!this.isCurrent(generation)) {
        return;
      }
      this.store.setCopySourceDoorVersion(doorVersion);
      if (copy.batches.length === 0) {
        await this.enumerateAndPrepare(generation);
        return;
      }
      this.store.updateCopyBatch(copy.currentBatchNumber, {
        sources: [],
        status: 'pending',
        errorMessage: null,
      });
      await this.prepareCurrentBatch(generation);
    } catch (error: unknown) {
      this.stop(generation, copyFailureMessage(error));
    }
  }

  private async prepareCurrentBatch(generation: number): Promise<void> {
    const copy = this.requireCapturedCopy();
    const batch = copy.batches.find((candidate) => candidate.batchNumber === copy.currentBatchNumber);
    if (batch === undefined) {
      throw new Error(ABWAB_LABELS.doorLinksCopyBatchInvalid);
    }
    if (!this.isValidTarget(copy.targetDoorId)) {
      throw new Error(ABWAB_LABELS.doorLinksCopyTargetUnavailable);
    }
    if (!this.isLiveDoor(copy.sourceDoorId)) {
      throw new Error(ABWAB_LABELS.doorLinksCopySourceChanged);
    }
    this.store.setCopyStatus('preparing');
    this.store.updateCopyBatch(batch.batchNumber, { sources: [], status: 'preparing', errorMessage: null });
    const records = await this.loader.hydrateRecords(
      copy.sourceDoorId,
      copy.expectedSourceDoorVersion,
      copy.expectedLinkingDataRevision,
      batch.unitIds,
      () => this.isCurrent(generation),
    );
    const linkingDataRevisions = new Set(records.map((record) => record.linkingDataRevision));
    if (linkingDataRevisions.size !== 1) {
      throw new Error(ABWAB_LABELS.doorLinksCopySourceChanged);
    }
    this.store.setCopyLinkingDataRevision([...linkingDataRevisions][0]!);
    const sourceLabel = ABWAB_LABELS.doorLinksCopySourceLabel(this.sourceDoorName(copy.sourceDoorId));
    const sources = mapAbwabDoorLinkCopyRecords(records, sourceLabel);
    if (sources.length === 0 || sources.length > ABWAB_DOOR_LINK_COPY_BATCH_SIZE) {
      throw new Error(ABWAB_LABELS.doorLinksCopyBatchInvalid);
    }
    if (!this.isCurrent(generation)) {
      throw new Error(ABWAB_LABELS.doorLinksCopyStopped);
    }
    if (!this.isLiveDoor(copy.sourceDoorId)) {
      throw new Error(ABWAB_LABELS.doorLinksCopySourceChanged);
    }
    if (!this.isValidTarget(copy.targetDoorId)) {
      throw new Error(ABWAB_LABELS.doorLinksCopyTargetUnavailable);
    }
    this.store.updateCopyBatch(batch.batchNumber, { sources, status: 'running', errorMessage: null });
    this.store.setCopyStatus('running');
    const started = this.workflow.startFromPreparedInlineSources(
      sources,
      copy.targetDoorId,
      { batchNumber: batch.batchNumber, totalBatches: copy.batches.length },
      {
        acknowledged: () => this.batchAcknowledged(generation, batch.batchNumber),
        stopped: (message) => this.stop(generation, safeWorkflowMessage(message)),
      },
    );
    if (!started) {
      throw new Error(ABWAB_LABELS.doorLinksCopyStartError);
    }
  }

  private batchAcknowledged(generation: number, batchNumber: number): void {
    if (!this.isCurrent(generation)) {
      return;
    }
    const copy = this.state().copy;
    this.store.updateCopyBatch(batchNumber, {
      sources: [],
      status: 'completed',
      errorMessage: null,
    });
    if (batchNumber >= copy.batches.length) {
      this.generation++;
      this.store.completeCopy(ABWAB_LABELS.doorLinksCopyCompleted);
      return;
    }
    this.store.setCurrentCopyBatch(batchNumber + 1);
    void this.prepareCurrentBatch(generation).catch((error: unknown) => {
      this.stop(generation, copyFailureMessage(error));
    });
  }

  private stop(generation: number, message: string): void {
    if (!this.isCurrent(generation)) {
      return;
    }
    const batchNumber = this.state().copy.currentBatchNumber;
    this.store.stopCopy(batchNumber, message);
  }

  private requireCapturedCopy(): {
    sourceDoorId: number;
    expectedSourceDoorVersion: number;
    expectedLinkingDataRevision: number | null;
    sourceSelection: AbwabDoorLinkSelectionState;
    targetDoorId: number;
    batches: readonly AbwabDoorLinkCopyBatch[];
    currentBatchNumber: number;
  } {
    const copy = this.state().copy;
    if (
      copy.sourceDoorId === null
      || copy.expectedSourceDoorVersion === null
      || copy.sourceSelection === null
      || copy.targetDoorId === null
    ) {
      throw new Error(ABWAB_LABELS.doorLinksCopyStartError);
    }
    return {
      sourceDoorId: copy.sourceDoorId,
      expectedSourceDoorVersion: copy.expectedSourceDoorVersion,
      expectedLinkingDataRevision: copy.expectedLinkingDataRevision,
      sourceSelection: copy.sourceSelection,
      targetDoorId: copy.targetDoorId,
      batches: copy.batches,
      currentBatchNumber: copy.currentBatchNumber,
    };
  }

  private isValidTarget(doorId: number): boolean {
    return doorId !== this.state().openDoorId && this.isLiveDoor(doorId);
  }

  private isLiveDoor(doorId: number): boolean {
    const door = this.tree.snapshot()?.byId.get(doorId);
    return door !== undefined && !door.isArchived && !door.sectionRetired;
  }

  private sourceDoorName(doorId: number): string {
    return this.tree.snapshot()?.byId.get(doorId)?.name ?? String(doorId);
  }

  private isCurrent(generation: number): boolean {
    return generation === this.generation && this.state().copy.open;
  }
}

function selectedCount(selection: AbwabDoorLinkSelectionState, totalCount: number): number {
  return selection.mode === 'only'
    ? selection.unitIds.length
    : Math.max(totalCount - selection.unitIds.length, 0);
}

function safeWorkflowMessage(message: string): string {
  return /^[A-Z0-9_]+$/.test(message) ? ABWAB_LABELS.doorLinksCopySourceChanged : message;
}
