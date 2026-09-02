import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Subscription } from 'rxjs';

import { AbwabInclusionsApi } from '../data-access/abwab-inclusions.api';
import { AbwabSnapshotFacade } from './abwab-snapshot.facade';
import { AbwabNode } from '../models/abwab.models';
import { ABWAB_LABELS } from '../models/abwab.labels';
import { AbwabDirectInclusionDoorDto } from '../../../core/api/generated/models/abwab-direct-inclusion-door-dto';
import { AbwabDoorInclusionTopologyDto } from '../../../core/api/generated/models/abwab-door-inclusion-topology-dto';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { ABWAB_WRITE_PERMISSIONS } from './abwab-permissions.controller';
import { AbwabMutationFailure, AbwabMutationPolicy } from './abwab-mutation.policy';

const NO_SOURCE_IDS: ReadonlySet<number> = new Set<number>();

@Injectable()
export class AbwabInclusionsController {
  private readonly api = inject(AbwabInclusionsApi);
  private readonly snapshot = inject(AbwabSnapshotFacade);
  private readonly mutationPolicy = inject(AbwabMutationPolicy);
  private readonly destroyRef = inject(DestroyRef);

  private readonly openState = signal(false);
  private readonly targetState = signal<AbwabNode | null>(null);
  private readonly topologyState = signal<AbwabDoorInclusionTopologyDto | null>(null);
  private readonly doorVersionState = signal<number | null>(null);
  private readonly initialLoadingState = signal(false);
  private readonly refreshingState = signal(false);
  private readonly readErrorState = signal<string | null>(null);
  private readonly writeErrorState = signal<string | null>(null);
  private readonly noticeState = signal<string | null>(null);
  private readonly submittingState = signal(false);
  private readonly detachingState = signal(false);
  private readonly detachCandidateState = signal<AbwabDirectInclusionDoorDto | null>(null);
  private readonly detachErrorState = signal<string | null>(null);
  private readonly selectedSourceIdsState = signal<ReadonlySet<number>>(NO_SOURCE_IDS);
  private readonly addCompletionState = signal(0);

  private topologyRequest: Subscription | null = null;
  private writeRequest: Subscription | null = null;
  private topologyGeneration = 0;
  private writeGeneration = 0;

  readonly isOpen = this.openState.asReadonly();
  readonly target = computed(() => {
    const target = this.targetState();
    return target === null ? null : (this.snapshot.snapshot()?.byId.get(target.id) ?? target);
  });
  readonly topology = this.topologyState.asReadonly();
  readonly doorVersion = this.doorVersionState.asReadonly();
  readonly isInitialLoading = this.initialLoadingState.asReadonly();
  readonly isRefreshing = this.refreshingState.asReadonly();
  readonly readError = this.readErrorState.asReadonly();
  readonly writeError = this.writeErrorState.asReadonly();
  readonly notice = this.noticeState.asReadonly();
  readonly isSubmitting = this.submittingState.asReadonly();
  readonly isDetaching = this.detachingState.asReadonly();
  readonly detachCandidate = this.detachCandidateState.asReadonly();
  readonly detachError = this.detachErrorState.asReadonly();
  readonly selectedSourceIds = this.selectedSourceIdsState.asReadonly();
  readonly addCompletion = this.addCompletionState.asReadonly();
  readonly selectedSourceCount = computed(() => this.selectedSourceIdsState().size);
  readonly directSourceIds = computed<ReadonlySet<number>>(() => {
    const topology = this.topologyState();
    return topology === null
      ? NO_SOURCE_IDS
      : new Set(topology.sources.map((source) => source.doorId));
  });
  readonly canSubmit = computed(() =>
    this.openState()
    && this.doorVersionState() !== null
    && this.selectedSourceIdsState().size > 0
    && !this.submittingState()
    && !this.detachingState(),
  );

  constructor() {
    this.destroyRef.onDestroy(() => this.cancelRequests());
  }

  open(target: AbwabNode): void {
    this.cancelRequests();
    this.openState.set(true);
    this.targetState.set(target);
    this.topologyState.set(null);
    this.doorVersionState.set(null);
    this.selectedSourceIdsState.set(NO_SOURCE_IDS);
    this.addCompletionState.set(0);
    this.readErrorState.set(null);
    this.writeErrorState.set(null);
    this.noticeState.set(null);
    this.submittingState.set(false);
    this.detachingState.set(false);
    this.detachCandidateState.set(null);
    this.detachErrorState.set(null);
    this.loadTopology();
  }

  close(): void {
    this.cancelRequests();
    this.openState.set(false);
    this.targetState.set(null);
    this.topologyState.set(null);
    this.doorVersionState.set(null);
    this.selectedSourceIdsState.set(NO_SOURCE_IDS);
    this.addCompletionState.set(0);
    this.initialLoadingState.set(false);
    this.refreshingState.set(false);
    this.readErrorState.set(null);
    this.writeErrorState.set(null);
    this.noticeState.set(null);
    this.submittingState.set(false);
    this.detachingState.set(false);
    this.detachCandidateState.set(null);
    this.detachErrorState.set(null);
  }

  retryLoad(): void {
    this.loadTopology();
  }

  refresh(): void {
    this.loadTopology();
  }

  toggleSource(doorId: number): void {
    const target = this.target();
    if (target === null || target.isArchived || doorId === target.id || this.directSourceIds().has(doorId)) {
      return;
    }

    const next = new Set(this.selectedSourceIdsState());
    if (!next.delete(doorId)) {
      next.add(doorId);
    }
    this.selectedSourceIdsState.set(next.size === 0 ? NO_SOURCE_IDS : next);
    this.writeErrorState.set(null);
    this.noticeState.set(null);
  }

  clearSourceDraft(): void {
    this.selectedSourceIdsState.set(NO_SOURCE_IDS);
    this.writeErrorState.set(null);
  }

  submit(): void {
    const target = this.target();
    const doorVersion = this.doorVersionState();
    const sourceDoorIds = [...this.selectedSourceIdsState()].sort((left, right) => left - right);
    if (target === null
        || target.isArchived
        || doorVersion === null
        || sourceDoorIds.length === 0
        || this.submittingState()
        || this.detachingState()) {
      return;
    }

    this.writeRequest?.unsubscribe();
    const generation = ++this.writeGeneration;
    this.submittingState.set(true);
    this.writeErrorState.set(null);
    this.noticeState.set(null);
    this.writeRequest = this.mutationPolicy.execute(
      ABWAB_WRITE_PERMISSIONS.createInclusion,
      () => this.api.addSources(target.id, {
        expectedTargetDoorVersion: doorVersion,
        sourceDoorIds,
      }),
    ).subscribe({
      next: (outcome) => {
        if (generation !== this.writeGeneration) {
          return;
        }
        this.submittingState.set(false);
        if (outcome.kind !== 'success') {
          this.handleWriteFailure('add', outcome, doorVersion);
          return;
        }
        if (outcome.data == null) {
          this.writeErrorState.set(outcome.envelope?.message ?? ABWAB_LABELS.inclusionsAddError);
          return;
        }

        this.doorVersionState.set(outcome.data.targetDoorVersion);
        this.selectedSourceIdsState.set(NO_SOURCE_IDS);
        this.addCompletionState.update((value) => value + 1);
        this.noticeState.set(outcome.envelope?.message ?? ABWAB_LABELS.inclusionsAddedNotice);
        this.loadTopology();
        this.snapshot.refresh();
      },
    });
  }

  requestDetach(source: AbwabDirectInclusionDoorDto): void {
    const target = this.target();
    if (target === null || target.isArchived || this.submittingState() || this.detachingState()) {
      return;
    }

    this.detachCandidateState.set(source);
    this.detachErrorState.set(null);
    this.writeErrorState.set(null);
    this.noticeState.set(null);
  }

  cancelDetach(): void {
    if (this.detachingState()) {
      return;
    }

    this.detachCandidateState.set(null);
    this.detachErrorState.set(null);
  }

  confirmDetach(): void {
    const target = this.target();
    const doorVersion = this.doorVersionState();
    const candidate = this.detachCandidateState();
    if (target === null
        || target.isArchived
        || doorVersion === null
        || candidate === null
        || this.submittingState()
        || this.detachingState()) {
      return;
    }

    this.writeRequest?.unsubscribe();
    const generation = ++this.writeGeneration;
    this.detachingState.set(true);
    this.detachErrorState.set(null);
    this.noticeState.set(null);
    this.writeRequest = this.mutationPolicy.execute(
      ABWAB_WRITE_PERMISSIONS.deleteInclusion,
      () => this.api.detachSource(
        target.id,
        candidate.inclusionId,
        { expectedTargetDoorVersion: doorVersion },
      ),
    ).subscribe({
      next: (outcome) => {
        if (generation !== this.writeGeneration) {
          return;
        }
        this.detachingState.set(false);
        if (outcome.kind !== 'success') {
          this.handleWriteFailure('detach', outcome, doorVersion);
          return;
        }
        if (outcome.data == null) {
          this.detachErrorState.set(outcome.envelope?.message ?? ABWAB_LABELS.inclusionsDetachError);
          return;
        }

        this.doorVersionState.set(outcome.data.targetDoorVersion);
        this.detachCandidateState.set(null);
        this.detachErrorState.set(null);
        this.noticeState.set(ABWAB_LABELS.inclusionsDetachedNotice(
          outcome.data.removedSynchronizedRecordCount,
        ));
        this.loadTopology();
        this.snapshot.refresh();
      },
    });
  }

  clearNotice(): void {
    this.noticeState.set(null);
  }

  private loadTopology(conflict?: {
    readonly attemptedVersion: number;
    readonly kind: 'add' | 'detach';
  }): void {
    const target = this.target();
    if (!this.openState() || target === null) {
      return;
    }

    this.topologyRequest?.unsubscribe();
    const generation = ++this.topologyGeneration;
    const isInitial = this.topologyState() === null;
    this.initialLoadingState.set(isInitial);
    this.refreshingState.set(!isInitial);
    this.readErrorState.set(null);
    this.topologyRequest = this.api.getTopology(target.id).subscribe({
      next: (response) => {
        if (generation !== this.topologyGeneration) {
          return;
        }
        this.finishTopologyLoad();
        if (!response.isSuccess || response.data == null) {
          this.readErrorState.set(response.message ?? ABWAB_LABELS.inclusionsLoadError);
          return;
        }
        this.topologyState.set(response.data);
        this.doorVersionState.set(response.data.doorVersion);
        if (conflict !== undefined && response.data.doorVersion !== conflict.attemptedVersion) {
          this.noticeState.set(ABWAB_LABELS.inclusionsConflictRefreshed);
          conflict.kind === 'add'
            ? this.writeErrorState.set(null)
            : this.detachErrorState.set(null);
        }
        const detachCandidate = this.detachCandidateState();
        if (detachCandidate !== null
            && !response.data.sources.some((source) => source.inclusionId === detachCandidate.inclusionId)) {
          this.detachCandidateState.set(null);
          this.detachErrorState.set(null);
        }
        const blockedSourceIds = new Set(response.data.sources.map((source) => source.doorId));
        const retainedSelection = new Set(
          [...this.selectedSourceIdsState()].filter((sourceDoorId) => !blockedSourceIds.has(sourceDoorId)),
        );
        this.selectedSourceIdsState.set(retainedSelection.size === 0 ? NO_SOURCE_IDS : retainedSelection);
        this.readErrorState.set(null);
      },
      error: (error: unknown) => {
        if (generation !== this.topologyGeneration) {
          return;
        }
        this.finishTopologyLoad();
        this.readErrorState.set(readErrorMessage(error, ABWAB_LABELS.inclusionsLoadError));
      },
    });
  }

  private finishTopologyLoad(): void {
    this.initialLoadingState.set(false);
    this.refreshingState.set(false);
  }

  private handleWriteFailure(
    kind: 'add' | 'detach',
    outcome: AbwabMutationFailure,
    attemptedVersion: number,
  ): void {
    const message = outcome.message;
    kind === 'add'
      ? this.writeErrorState.set(message)
      : this.detachErrorState.set(message);
    if (outcome.kind === 'conflict') {
      this.loadTopology({ attemptedVersion, kind });
      this.snapshot.refresh();
    }
  }

  private cancelRequests(): void {
    this.topologyGeneration++;
    this.writeGeneration++;
    this.topologyRequest?.unsubscribe();
    this.writeRequest?.unsubscribe();
    this.topologyRequest = null;
    this.writeRequest = null;
  }
}

function readErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)
      || typeof error.error !== 'object'
      || error.error === null) {
    return fallback;
  }
  const response = error.error as ApiResponse<unknown>;
  return typeof response.message === 'string' && response.message.length > 0
    ? response.message
    : fallback;
}
