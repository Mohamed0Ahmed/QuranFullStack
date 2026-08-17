import { Injectable, inject } from '@angular/core';

import { LinkingWorkflowFacade } from '../../linking/state/linking-workflow.facade';
import {
  AbwabDoorLinkCopyRecord,
  AbwabDoorLinkRecordView,
  AbwabDoorLinkSelectionState,
} from '../models/abwab-door-links.models';
import { ABWAB_LABELS } from '../models/abwab.labels';
import { AbwabSnapshotFacade } from './abwab-snapshot.facade';
import { AbwabDoorLinkCopyLoader, copyFailureMessage } from './abwab-door-link-copy.loader';
import { mapAbwabDoorLinkCopyRecords } from './abwab-door-link-copy.mapper';
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
    if (state.openDoorId === null || selectedCount(state.selection, state.records.totalCount) === 0) {
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

  selectTarget(targetDoorId: number | null): void {
    if (this.state().copy.status !== 'choosing') {
      return;
    }
    this.store.setCopyTarget(targetDoorId !== null && this.isValidTarget(targetDoorId) ? targetDoorId : null);
  }

  start(): void {
    const state = this.state();
    const copy = state.copy;
    if (
      !copy.open
      || copy.status !== 'choosing'
      || copy.sourceSelection === null
      || copy.targetDoorId === null
      || state.openDoorId === null
      || state.doorVersion === null
      || state.records.linkingDataRevision === null
      || !this.isValidTarget(copy.targetDoorId)
      || !this.isLiveDoor(state.openDoorId)
    ) {
      return;
    }
    const sourceSelection = cloneSelection(copy.sourceSelection);
    const selectedViews = selectRecordViews(state.records.items, sourceSelection);
    if (selectedViews.length !== selectedCount(sourceSelection, state.records.totalCount)) {
      return;
    }
    const generation = ++this.generation;
    this.store.beginCopyPreparation(state.openDoorId, state.doorVersion, sourceSelection);
    this.store.setCopyUnitIds(selectedViews.map((record) => record.summary.unitId));
    this.store.setCopyLinkingDataRevision(state.records.linkingDataRevision);
    this.prepareCopy(generation);
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
    this.tree.load();
    void this.refreshAndRetry(generation, copy.sourceDoorId);
  }

  private async refreshAndRetry(generation: number, sourceDoorId: number): Promise<void> {
    try {
      const snapshot = await this.loader.loadSnapshot(sourceDoorId);
      if (!this.isCurrent(generation)) {
        return;
      }
      this.store.receiveSnapshot(snapshot);
      const copy = this.requireCapturedCopy();
      const selectedViews = selectRecordViews(this.state().records.items, copy.sourceSelection);
      if (selectedViews.length !== selectedCount(copy.sourceSelection, this.state().records.totalCount)) {
        throw new Error(ABWAB_LABELS.doorLinksCopySourceChanged);
      }
      this.store.setCopySourceDoorVersion(snapshot.doorVersion);
      this.store.setCopyLinkingDataRevision(snapshot.linkingDataRevision);
      this.store.setCopyUnitIds(selectedViews.map((record) => record.summary.unitId));
      this.prepareCopy(generation);
    } catch (error: unknown) {
      this.stop(generation, copyFailureMessage(error));
    }
  }

  private prepareCopy(generation: number): void {
    try {
      const state = this.state();
      const copy = this.requireCapturedCopy();
      if (
        copy.unitIds.length === 0
        || state.doorVersion !== copy.expectedSourceDoorVersion
        || state.records.linkingDataRevision !== copy.expectedLinkingDataRevision
        || !this.isLiveDoor(copy.sourceDoorId)
      ) {
        throw new Error(ABWAB_LABELS.doorLinksCopySourceChanged);
      }
      if (!this.isValidTarget(copy.targetDoorId)) {
        throw new Error(ABWAB_LABELS.doorLinksCopyTargetUnavailable);
      }
      this.store.setCopyStatus('preparing');
      const selectedIds = new Set(copy.unitIds);
      const selectedViews = state.records.items.filter((record) => selectedIds.has(record.summary.unitId));
      if (selectedViews.length !== selectedIds.size || copy.expectedLinkingDataRevision === null) {
        throw new Error(ABWAB_LABELS.doorLinksCopySourceChanged);
      }
      const sourceLabel = ABWAB_LABELS.doorLinksCopySourceLabel(this.sourceDoorName(copy.sourceDoorId));
      const sources = mapAbwabDoorLinkCopyRecords(
        toCopyRecords(selectedViews, copy.expectedLinkingDataRevision),
        sourceLabel,
      );
      if (sources.length === 0) {
        throw new Error(ABWAB_LABELS.doorLinksCopyInvalid);
      }
      if (!this.isCurrent(generation)) {
        return;
      }
      this.store.setCopySources(sources);
      this.store.setCopyStatus('running');
      const started = this.workflow.startFromPreparedInlineSources(
        sources,
        copy.targetDoorId,
        {
          acknowledged: () => this.copyAcknowledged(generation),
          cancelled: () => this.copyCancelled(generation),
          stopped: (message) => this.stop(generation, safeWorkflowMessage(message)),
        },
      );
      if (!started) {
        throw new Error(ABWAB_LABELS.doorLinksCopyStartError);
      }
    } catch (error: unknown) {
      this.stop(generation, copyFailureMessage(error));
    }
  }

  private copyAcknowledged(generation: number): void {
    if (!this.isCurrent(generation)) {
      return;
    }
    this.generation++;
    this.store.completeCopy(ABWAB_LABELS.doorLinksCopyCompleted);
  }

  private copyCancelled(generation: number): void {
    if (!this.isCurrent(generation)) {
      return;
    }
    this.generation++;
    this.store.closeCopy();
  }

  private stop(generation: number, message: string): void {
    if (this.isCurrent(generation)) {
      this.store.stopCopy(message);
    }
  }

  private requireCapturedCopy(): {
    sourceDoorId: number;
    expectedSourceDoorVersion: number;
    expectedLinkingDataRevision: number | null;
    sourceSelection: AbwabDoorLinkSelectionState;
    targetDoorId: number;
    unitIds: readonly number[];
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
      unitIds: copy.unitIds,
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

function cloneSelection(selection: AbwabDoorLinkSelectionState): AbwabDoorLinkSelectionState {
  return { ...selection, unitIds: [...selection.unitIds] };
}

function selectRecordViews(
  records: readonly AbwabDoorLinkRecordView[],
  selection: AbwabDoorLinkSelectionState,
): readonly AbwabDoorLinkRecordView[] {
  const ids = new Set(selection.unitIds);
  return records.filter((record) =>
    selection.mode === 'only' ? ids.has(record.summary.unitId) : !ids.has(record.summary.unitId),
  );
}

function toCopyRecords(
  records: readonly AbwabDoorLinkRecordView[],
  linkingDataRevision: number,
): readonly AbwabDoorLinkCopyRecord[] {
  return records.map((record) => ({
    unitId: record.summary.unitId,
    isGrouped: record.summary.isGrouped,
    linkingDataRevision,
    ayahs: record.ayahs,
  }));
}

function selectedCount(selection: AbwabDoorLinkSelectionState, totalCount: number): number {
  return selection.mode === 'only'
    ? selection.unitIds.length
    : Math.max(totalCount - selection.unitIds.length, 0);
}

function safeWorkflowMessage(message: string): string {
  return /^[A-Z0-9_]+$/.test(message) ? ABWAB_LABELS.doorLinksCopySourceChanged : message;
}
