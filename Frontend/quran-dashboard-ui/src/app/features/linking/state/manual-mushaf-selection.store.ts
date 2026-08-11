import { Injectable, computed, effect, inject, signal } from '@angular/core';

import { LinkingManualMushafAyahReference } from '../models/linking-manual-mushaf.models';
import { LINKING_LABELS } from '../models/linking.labels';
import { ManualMushafAyahReader } from '../data-access/manual-mushaf-ayah.reader';
import { linkingSourceKey } from '../utils/linking-source-key';
import { orderedUniqueLinkingVerseKeys } from '../utils/linking-verse-order';
import { LinkingAccessService } from './linking-access.service';
import { LinkingWorkspaceStore } from './linking-workspace.store';

export type ManualMushafSelectionLoadStatus = 'loading' | 'ready' | 'error';

export interface ManualMushafSelectionEntry {
  verseKey: string;
  reference: LinkingManualMushafAyahReference | null;
  status: ManualMushafSelectionLoadStatus;
  errorMessage: string | null;
  operationGeneration: number;
}

@Injectable({ providedIn: 'root' })
export class ManualMushafSelectionStore {
  private readonly access = inject(LinkingAccessService);
  private readonly reader = inject(ManualMushafAyahReader);
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly activeSignal = signal(false);
  private readonly currentPageSignal = signal<number | null>(null);
  private readonly entriesSignal = signal<readonly ManualMushafSelectionEntry[]>([]);
  private readonly operationGenerationSignal = signal(0);
  private readonly statusMessageSignal = signal<string | null>(null);

  readonly active = this.activeSignal.asReadonly();
  readonly currentPage = this.currentPageSignal.asReadonly();
  readonly entries = computed(() => orderEntries(this.entriesSignal()));
  readonly selectedVerseKeys = computed(() => this.entries().map((entry) => entry.verseKey));
  readonly selectedCount = computed(() => this.entries().length);
  readonly operationGeneration = this.operationGenerationSignal.asReadonly();
  readonly statusMessage = this.statusMessageSignal.asReadonly();
  readonly failedEntry = computed(() => this.entries().find((entry) => entry.status === 'error') ?? null);
  readonly canAddToWorkspace = computed(
    () =>
      this.active() &&
      this.entries().length > 0 &&
      this.entries().every((entry) => entry.status === 'ready' && entry.reference !== null),
  );

  constructor() {
    effect(() => {
      if (this.active() && !this.access.canUseLinking()) {
        this.reset(null);
      }
    });
  }

  setCurrentPage(pageNumber: number | null): void {
    if (this.active()) {
      this.currentPageSignal.set(pageNumber);
    }
  }

  activate(): void {
    if (!this.requireOwner()) {
      return;
    }

    this.activeSignal.set(true);
    this.statusMessageSignal.set(LINKING_LABELS.mushafSelectionInstruction);
  }

  cancel(): void {
    if (!this.requireOwner()) {
      return;
    }

    this.reset(LINKING_LABELS.mushafSelectionCancelled);
  }

  clear(): void {
    if (!this.requireOwner()) {
      return;
    }

    this.entriesSignal.set([]);
    this.statusMessageSignal.set(LINKING_LABELS.mushafSelectionCleared);
  }

  toggle(verseKey: string): void {
    if (!this.requireOwner() || !this.active()) {
      return;
    }

    const existing = this.entriesSignal().find((entry) => entry.verseKey === verseKey);
    if (existing) {
      this.remove(verseKey);
      return;
    }

    const operationGeneration = this.nextOperationGeneration();
    this.entriesSignal.update((entries) => [
      ...entries,
      {
        verseKey,
        reference: null,
        status: 'loading',
        errorMessage: null,
        operationGeneration,
      },
    ]);
    this.statusMessageSignal.set(LINKING_LABELS.mushafSelectionLoading);
    this.reader.readMetadata(verseKey).subscribe({
      next: (reference) => this.completeMetadataLoad(verseKey, operationGeneration, reference),
      error: () => this.failMetadataLoad(verseKey, operationGeneration),
    });
  }

  retry(verseKey: string): void {
    if (!this.requireOwner() || !this.active()) {
      return;
    }

    const entry = this.entriesSignal().find((candidate) => candidate.verseKey === verseKey);
    if (!entry || entry.status !== 'error') {
      return;
    }

    const operationGeneration = this.nextOperationGeneration();
    this.entriesSignal.update((entries) =>
      entries.map((candidate) =>
        candidate.verseKey === verseKey
          ? { ...candidate, status: 'loading', errorMessage: null, operationGeneration }
          : candidate,
      ),
    );
    this.statusMessageSignal.set(LINKING_LABELS.mushafSelectionLoading);
    this.reader.readMetadata(verseKey).subscribe({
      next: (reference) => this.completeMetadataLoad(verseKey, operationGeneration, reference),
      error: () => this.failMetadataLoad(verseKey, operationGeneration),
    });
  }

  remove(verseKey: string): void {
    if (!this.requireOwner()) {
      return;
    }

    this.entriesSignal.update((entries) => entries.filter((entry) => entry.verseKey !== verseKey));
    this.statusMessageSignal.set(LINKING_LABELS.mushafSelectionRemoved);
  }

  addToWorkspace(): void {
    if (!this.requireOwner() || !this.canAddToWorkspace()) {
      return;
    }

    const source = {
      kind: 'manual-mushaf-ayahs' as const,
      label: LINKING_LABELS.mushafSelectionSource,
      manualAyahs: this.entries()
        .map((entry) => entry.reference)
        .filter((reference): reference is LinkingManualMushafAyahReference => reference !== null),
    };
    const existing = this.workspace.items().some((item) => item.sourceKey === linkingSourceKey(source));
    const sourceKey = this.workspace.addSource(source);
    if (sourceKey === null) {
      this.statusMessageSignal.set(LINKING_LABELS.mushafSelectionHandoffError);
      return;
    }

    this.reset(existing ? LINKING_LABELS.alreadyInWorkspace : LINKING_LABELS.addedToWorkspace);
  }

  reset(statusMessage: string | null = null): void {
    this.nextOperationGeneration();
    this.activeSignal.set(false);
    this.entriesSignal.set([]);
    this.currentPageSignal.set(null);
    this.statusMessageSignal.set(statusMessage);
  }

  private completeMetadataLoad(
    verseKey: string,
    operationGeneration: number,
    reference: LinkingManualMushafAyahReference,
  ): void {
    if (!this.isCurrentOperation(verseKey, operationGeneration)) {
      return;
    }

    this.entriesSignal.update((entries) =>
      entries.map((entry) =>
        entry.verseKey === verseKey
          ? { ...entry, reference, status: 'ready', errorMessage: null }
          : entry,
      ),
    );
    this.statusMessageSignal.set(LINKING_LABELS.mushafSelectionReady);
  }

  private failMetadataLoad(verseKey: string, operationGeneration: number): void {
    if (!this.isCurrentOperation(verseKey, operationGeneration)) {
      return;
    }

    this.entriesSignal.update((entries) =>
      entries.map((entry) =>
        entry.verseKey === verseKey
          ? {
              ...entry,
              status: 'error',
              errorMessage: `${LINKING_LABELS.mushafSelectionMetadataError} ${verseKey}`,
            }
          : entry,
      ),
    );
    this.statusMessageSignal.set(`${LINKING_LABELS.mushafSelectionMetadataError} ${verseKey}`);
  }

  private isCurrentOperation(verseKey: string, operationGeneration: number): boolean {
    return (
      this.active() &&
      this.access.canUseLinking() &&
      this.entriesSignal().some(
        (entry) => entry.verseKey === verseKey && entry.operationGeneration === operationGeneration,
      )
    );
  }

  private requireOwner(): boolean {
    if (this.access.canUseLinking()) {
      return true;
    }

    this.reset(null);
    return false;
  }

  private nextOperationGeneration(): number {
    const operationGeneration = this.operationGenerationSignal() + 1;
    this.operationGenerationSignal.set(operationGeneration);
    return operationGeneration;
  }
}

function orderEntries(entries: readonly ManualMushafSelectionEntry[]): readonly ManualMushafSelectionEntry[] {
  const entriesByVerseKey = new Map(entries.map((entry) => [entry.verseKey, entry]));
  return orderedUniqueLinkingVerseKeys(entries.map((entry) => entry.verseKey))
    .map((verseKey) => entriesByVerseKey.get(verseKey))
    .filter((entry): entry is ManualMushafSelectionEntry => entry !== undefined);
}
