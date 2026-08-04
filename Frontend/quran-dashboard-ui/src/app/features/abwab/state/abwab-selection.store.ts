import { Injectable, computed, signal } from '@angular/core';

import { AbwabTreeSnapshotVm } from '../models/abwab.models';

interface AbwabSelectionState {
  readonly selectedDoorId: number | null;
  readonly selectedVersion: number | null;
  readonly bulkMode: boolean;
  readonly bulkSet: ReadonlyMap<number, number>;
}

const INITIAL_STATE: AbwabSelectionState = {
  selectedDoorId: null,
  selectedVersion: null,
  bulkMode: false,
  bulkSet: new Map(),
};

@Injectable({ providedIn: 'root' })
export class AbwabSelectionStore {
  private readonly state = signal<AbwabSelectionState>(INITIAL_STATE);
  private readonly archiveViewActive = signal(false);

  readonly selectedDoorId = computed(() => this.state().selectedDoorId);
  readonly selectedVersion = computed(() => this.state().selectedVersion);
  readonly bulkMode = computed(() => this.state().bulkMode);
  readonly bulkSet = computed(() => this.state().bulkSet);
  readonly bulkCount = computed(() => this.state().bulkSet.size);

  select(doorId: number, version: number): void {
    if (this.state().bulkMode) {
      return;
    }
    this.state.update((current) => ({ ...current, selectedDoorId: doorId, selectedVersion: version }));
  }

  clearSelection(): void {
    this.state.update((current) => ({ ...current, selectedDoorId: null, selectedVersion: null }));
  }

  setArchiveViewActive(active: boolean): void {
    this.archiveViewActive.set(active);
    if (active) {
      this.setBulkMode(false);
    }
  }

  setBulkMode(on: boolean): void {
    if (on && this.archiveViewActive()) {
      return;
    }
    this.state.update((current) => ({
      ...current,
      bulkMode: on,
      selectedDoorId: on ? null : current.selectedDoorId,
      selectedVersion: on ? null : current.selectedVersion,
      bulkSet: on ? current.bulkSet : new Map(),
    }));
  }

  toggleBulk(doorId: number, version: number): void {
    this.state.update((current) => {
      const next = new Map(current.bulkSet);
      if (next.has(doorId)) {
        next.delete(doorId);
      } else {
        next.set(doorId, version);
      }
      return { ...current, bulkSet: next };
    });
  }

  clearBulk(): void {
    this.state.update((current) => ({ ...current, bulkSet: new Map() }));
  }

  rebindTo(snapshot: AbwabTreeSnapshotVm): void {
    this.state.update((current) => {
      const selectedNode =
        current.selectedDoorId !== null ? snapshot.byId.get(current.selectedDoorId) : undefined;

      const nextBulk = new Map<number, number>();
      for (const doorId of current.bulkSet.keys()) {
        const node = snapshot.byId.get(doorId);
        if (node && !node.isArchived) {
          nextBulk.set(doorId, node.version);
        }
      }

      return {
        ...current,
        selectedDoorId: selectedNode ? current.selectedDoorId : null,
        selectedVersion: selectedNode ? selectedNode.version : null,
        bulkSet: nextBulk,
      };
    });
  }
}
