import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { AbwabSnapshotFacade } from '../../abwab/state/abwab-snapshot.facade';
import { DetailOverlayHistoryService } from '../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { LinkingSourceLaunch, LinkingSourceLaunchInput, toLinkingSourceLaunch } from '../models/linking-source-launch.models';
import {
  LinkingCopyCallbacks,
  LinkingOperationSourceDraft,
} from '../models/linking-operation-draft.models';
import { LinkingManualLinkShape } from '../models/linking-manual-mushaf.models';
import { isPreparedPreflightReady } from '../models/linking-prepared-preflight.models';
import { LINKING_LABELS } from '../models/linking.labels';
import {
  INITIAL_LINKING_WORKFLOW,
  LINKING_WORKFLOW_NAVIGABLE_STEPS,
  LinkingWorkflowState,
  LinkingWorkflowStep,
  isCopyPreflightNoOp,
  isStaleLinkingFailure,
} from '../models/linking-workflow.models';
import { LinkingAccessService } from './linking-access.service';
import { LinkingExecutionStore } from './linking-execution.store';
import { LinkingFocusCoordinator } from './linking-focus.coordinator';
import { LinkingInlineSourceWorkflowController } from './linking-inline-source-workflow.controller';
import {
  LinkingOperationDraftStore,
  createPreparedLinkingRequest,
} from './linking-operation-draft.store';
import { LinkingPreflightDetailsFacade } from './linking-preflight-details.facade';
import { LinkingPreparedPreflightFacade } from './linking-prepared-preflight.facade';
import { LinkingSourcePagesFacade } from './linking-source-pages.facade';
import { LinkingWorkflowCompletionController } from './linking-workflow-completion.controller';
import { LinkingWorkspaceStore } from './linking-workspace.store';
export type { LinkingWorkflowStep } from '../models/linking-workflow.models';
@Injectable({ providedIn: 'root' })
export class LinkingWorkflowFacade {
  private readonly access = inject(LinkingAccessService);
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly overlay = inject(DetailOverlayHistoryService);
  private readonly focus = inject(LinkingFocusCoordinator);
  private readonly doors = inject(AbwabSnapshotFacade);
  private readonly drafts = inject(LinkingOperationDraftStore);
  private readonly inlineSource = inject(LinkingInlineSourceWorkflowController);
  private readonly preparedFacade = inject(LinkingPreparedPreflightFacade);
  private readonly details = inject(LinkingPreflightDetailsFacade);
  private readonly execution = inject(LinkingExecutionStore);
  private readonly sourcePages = inject(LinkingSourcePagesFacade);
  private readonly completion = inject(LinkingWorkflowCompletionController);
  private readonly stateSignal = signal<LinkingWorkflowState>(INITIAL_LINKING_WORKFLOW);
  private readonly pendingSourceSignal = signal<LinkingSourceLaunch | null>(null);
  private restoreOverlayFocus = false;
  readonly state = this.stateSignal.asReadonly();
  readonly step = computed(() => this.stateSignal().step);
  readonly selectedDoorId = computed(() => this.stateSignal().selectedDoorId);
  readonly directDraft = this.inlineSource.draft;
  readonly isCopy = computed(() => this.stateSignal().origin === 'copy');
  readonly prepared = computed(() => this.stateSignal().prepared);
  readonly executionState = this.execution.state;
  readonly directSourceRequest = this.inlineSource.sourceRequest;
  readonly directTotalAyahCount = this.inlineSource.totalAyahCount;
  readonly directSelectedCount = this.inlineSource.selectedCount;
  readonly directManualGrouped = this.inlineSource.manualGrouped;
  readonly canAdvanceSource = computed(() =>
    this.stateSignal().origin === 'source' &&
    (this.inlineSource.draft()?.linkingDataRevision ?? 0) > 0 &&
    this.directSelectedCount() > 0,
  );
  readonly canAdvanceDoor = computed(() =>
    this.access.canUseLinking() && this.isLiveDoor(this.selectedDoorId()),
  );
  readonly preflightStatus = computed(() => {
    const step = this.step();
    if (step === 'preflighting') {
      return 'loading' as const;
    }
    if (step === 'failed' && this.execution.state().job === null) {
      return 'error' as const;
    }
    return this.prepared() === null ? 'idle' as const : 'ready' as const;
  });
  readonly preflightMessage = computed(() => this.stateSignal().errorMessage);
  readonly isNoOp = computed(() => {
    const prepared = this.prepared();
    return prepared !== null && (prepared.isNoOp === true
      || this.stateSignal().origin === 'copy' && isCopyPreflightNoOp(prepared));
  });
  readonly canSubmit = computed(() => {
    const prepared = this.prepared();
    return this.step() === 'ready' &&
      prepared !== null &&
      prepared.preflightToken !== null &&
      prepared.isBlocked !== true &&
      !this.isNoOp();
  });
  readonly canCancelExecution = computed(() => {
    const job = this.execution.state().job;
    return job !== null &&
      !['succeeded', 'failed', 'cancelled'].includes(job.status.toLowerCase()) &&
      job.stage.toLowerCase() !== 'finalizing';
  });

  constructor() {
    effect(() => this.synchronizePrepared());
    effect(() => this.synchronizeExecution());
    effect(() => {
      const overlayOpen = this.overlay.isOpen();
      const pendingSource = this.pendingSourceSignal();
      if (!this.access.canUseLinking() && this.stateSignal().origin !== null) {
        untracked(() => this.dismiss());
      }
      if (pendingSource !== null && !overlayOpen) {
        untracked(() => {
          this.pendingSourceSignal.set(null);
          this.startFromSource(pendingSource);
        });
      }
      if (this.restoreOverlayFocus && overlayOpen) {
        this.restoreOverlayFocus = false;
        untracked(() => this.focus.restore());
      }
    });
  }

  startFromSource(source: LinkingSourceLaunchInput): boolean {
    if (!this.access.canUseLinking()) {
      return false;
    }
    if (this.overlay.isOpen()) {
      this.pendingSourceSignal.set(toLinkingSourceLaunch(source));
      this.overlay.close();
      return true;
    }
    if (!this.workspace.openOperationFlow()) {
      return false;
    }
    const generation = this.stateSignal().operationGeneration + 1;
    this.completion.clearCopyCallbacks();
    this.inlineSource.start(toLinkingSourceLaunch(source), generation);
    this.stateSignal.set({
      ...INITIAL_LINKING_WORKFLOW,
      origin: 'source',
      operationGeneration: generation,
    });
    return true;
  }

  startFromPreparedInlineSources(
    sources: readonly LinkingOperationSourceDraft[],
    targetDoorId: number,
    callbacks: LinkingCopyCallbacks,
  ): boolean {
    if (
      !this.access.canUseLinking()
      || !this.isLiveDoor(targetDoorId)
      || sources.length === 0
      || !this.workspace.openOperationFlow()
    ) {
      return false;
    }
    const generation = this.stateSignal().operationGeneration + 1;
    const revisions = new Set(sources.map((source) => source.linkingDataRevision));
    this.completion.setCopyCallbacks(callbacks);
    this.inlineSource.reset(generation);
    this.drafts.replace(sources, revisions.size === 1 ? [...revisions][0]! : null, targetDoorId);
    this.stateSignal.set({
      ...INITIAL_LINKING_WORKFLOW,
      origin: 'copy',
      step: 'preflighting',
      selectedDoorId: targetDoorId,
      operationGeneration: generation,
    });
    void this.runPreflight();
    return true;
  }

  startWorkspaceOperation(): void {
    void this.startWorkspaceAfterFlush();
  }

  toggleDirectAyah(ayahId: number): void {
    this.inlineSource.toggleAyah(ayahId, this.selectedDoorId());
  }

  selectAllDirectAyahs(): void {
    this.inlineSource.selectAllAyahs(this.selectedDoorId());
  }

  clearAllDirectAyahs(): void {
    this.inlineSource.clearAllAyahs(this.selectedDoorId());
  }

  toggleDirectManualWord(ayahId: number, quranWordId: number): void {
    this.inlineSource.toggleManualWord(ayahId, quranWordId, this.selectedDoorId());
  }

  setDirectAutomaticWords(enabled: boolean): void {
    this.inlineSource.setAutomaticWords(enabled, this.selectedDoorId());
  }

  setDirectManualLinkShape(linkShape: LinkingManualLinkShape): void {
    this.inlineSource.setManualLinkShape(linkShape, this.selectedDoorId());
  }

  loadDoors(): void {
    if (this.access.canUseLinking()) {
      this.doors.load();
    }
  }

  selectDoor(doorId: number): void {
    if (this.access.canUseLinking() && this.isLiveDoor(doorId)) {
      this.stateSignal.update((state) => ({ ...state, selectedDoorId: doorId }));
      this.drafts.setDoor(doorId);
    }
  }

  clearDoor(): void {
    this.stateSignal.update((state) => ({ ...state, selectedDoorId: null }));
    this.drafts.setDoor(null);
  }

  next(): void {
    if (this.step() === 'configure-source' && this.canAdvanceSource()) {
      this.doors.ensureLoaded();
      this.stateSignal.update((state) => ({ ...state, step: 'door' }));
      return;
    }
    if (this.step() === 'door' && this.canAdvanceDoor()) {
      void this.runPreflight();
    }
  }

  retryPreflight(): void {
    if (
      this.stateSignal().origin !== null
      && this.stateSignal().origin !== 'copy'
      && this.selectedDoorId() !== null
    ) {
      const execution = this.execution.state();
      if (execution.job !== null && ['failed', 'cancelled'].includes(execution.job.status.toLowerCase())) {
        void this.execution.acknowledge().finally(() => {
          this.execution.dismiss();
          void this.runPreflight();
        });
        return;
      }
      void this.runPreflight();
    }
  }

  submit(): void {
    const prepared = this.prepared();
    const preparationKey = this.stateSignal().preparationKey;
    if (
      !this.canSubmit() ||
      prepared === null ||
      prepared.preflightToken === null ||
      preparationKey === null
    ) {
      return;
    }
    const confirmationIdempotencyKey =
      this.stateSignal().confirmationIdempotencyKey ?? crypto.randomUUID();
    this.stateSignal.update((state) => ({
      ...state,
      step: 'submitting',
      confirmationIdempotencyKey,
      errorMessage: null,
    }));
    void this.execution.execute(
      preparationKey,
      prepared.preflightId,
      prepared.preflightToken,
      confirmationIdempotencyKey,
    );
  }

  cancelExecution(): void {
    if (this.canCancelExecution()) {
      void this.execution.cancel();
    }
  }

  canNavigateTo(target: LinkingWorkflowStep): boolean {
    if (this.stateSignal().origin === 'copy') {
      return false;
    }
    const currentIndex = LINKING_WORKFLOW_NAVIGABLE_STEPS.indexOf(this.step());
    const targetIndex = LINKING_WORKFLOW_NAVIGABLE_STEPS.indexOf(target);
    return targetIndex >= 0 && currentIndex >= 0 && targetIndex < currentIndex;
  }

  navigateTo(target: LinkingWorkflowStep): void {
    if (!this.canNavigateTo(target)) {
      return;
    }
    if (target === 'configure-source' && this.stateSignal().origin !== 'source') {
      return;
    }
    const preparationKey = this.stateSignal().preparationKey;
    if (preparationKey !== null) {
      this.preparedFacade.dismiss(preparationKey);
      this.details.evict(this.stateSignal().prepared?.preflightId ?? '');
    }
    this.stateSignal.update((state) => ({
      ...state,
      step: target,
      preparationKey: null,
      confirmationIdempotencyKey: null,
      prepared: null,
      errorMessage: null,
    }));
  }

  acknowledgeSuccess(): Promise<void> {
    return this.completion.acknowledge(this.stateSignal().origin, () => this.dismiss(false));
  }

  dismiss(notifyCopyStop = true): void {
    const state = this.stateSignal();
    const execution = this.execution.state();
    if (
      notifyCopyStop
      && (state.step === 'succeeded' || this.completion.executionSucceeded(state.confirmationIdempotencyKey))
    ) {
      void this.acknowledgeSuccess();
      return;
    }
    if (
      notifyCopyStop
      && state.origin === 'copy'
      && this.completion.executionInFlight(state.confirmationIdempotencyKey)
      && !this.canCancelExecution()
    ) {
      return;
    }
    if (state.origin === 'copy' && this.canCancelExecution()) {
      void this.execution.cancel();
    }
    if (notifyCopyStop && state.origin === 'copy') {
      this.completion.cancelCopy();
    } else {
      this.completion.clearCopyCallbacks();
    }
    const acceptedJob = execution.job !== null || execution.outcome !== null;
    if (!acceptedJob && state.preparationKey !== null && state.prepared !== null) {
      void this.preparedFacade.cancel(state.preparationKey).finally(() =>
        this.preparedFacade.dismiss(state.preparationKey!),
      );
    } else if (state.preparationKey !== null) {
      this.preparedFacade.dismiss(state.preparationKey);
    }
    if (state.prepared !== null) {
      this.details.evict(state.prepared.preflightId);
    }
    this.sourcePages.cancel('direct-linking-source');
    this.execution.dismiss();
    this.drafts.reset();
    this.inlineSource.reset(state.operationGeneration + 1);
    this.pendingSourceSignal.set(null);
    this.stateSignal.set({
      ...INITIAL_LINKING_WORKFLOW,
      operationGeneration: state.operationGeneration + 1,
    });
    if (state.origin === 'workspace') {
      this.workspace.openWorkspace();
      return;
    }
    this.workspace.close();
    if (this.overlay.isRetainedClosed()) {
      this.restoreOverlayFocus = true;
      this.overlay.restore();
    } else {
      this.focus.restore();
    }
  }

  private async startWorkspaceAfterFlush(): Promise<void> {
    if (!this.access.canUseLinking() || this.workspace.checkedSourceKeys().length === 0) {
      return;
    }
    try {
      await this.workspace.flushSelectedSources();
    } catch {
      return;
    }
    if (!this.workspace.openOperationFlow()) {
      return;
    }
    this.doors.ensureLoaded();
    this.completion.clearCopyCallbacks();
    this.inlineSource.reset(this.stateSignal().operationGeneration + 1);
    this.stateSignal.set({
      ...INITIAL_LINKING_WORKFLOW,
      origin: 'workspace',
      step: 'door',
      operationGeneration: this.stateSignal().operationGeneration + 1,
    });
  }

  private async runPreflight(): Promise<void> {
    const doorId = this.selectedDoorId();
    if (doorId === null) {
      return;
    }
    if (this.stateSignal().origin === 'workspace') {
      try {
        await this.workspace.flushSelectedSources();
      } catch {
        return;
      }
    }
    const preparationKey = crypto.randomUUID();
    const origin = this.stateSignal().origin;
    if (origin === 'copy' && !this.isLiveDoor(doorId)) {
      this.failCopyWorkflow(LINKING_LABELS.copyTargetUnavailable);
      return;
    }
    const inlineDrafts = origin === 'source'
      ? [this.inlineSource.requireDraft()]
      : origin === 'copy'
        ? this.currentPreparedInlineSources()
        : null;
    if (origin === 'copy' && (inlineDrafts === null || inlineDrafts.length === 0)) {
      this.failCopyWorkflow(LINKING_LABELS.copyInvalid);
      return;
    }
    const request = createPreparedLinkingRequest(
      preparationKey,
      doorId,
      inlineDrafts,
      this.workspace.items(),
      this.workspace.checkedSourceKeys(),
    );
    this.stateSignal.update((state) => ({
      ...state,
      step: 'preflighting',
      preparationKey,
      confirmationIdempotencyKey: null,
      prepared: null,
      errorMessage: null,
    }));
    await this.preparedFacade.create(request);
  }

  private synchronizePrepared(): void {
    const step = this.stateSignal().step;
    if (step !== 'preflighting' && step !== 'ready') {
      return;
    }
    const key = this.stateSignal().preparationKey;
    if (key === null) {
      return;
    }
    const preparedState = this.preparedFacade.stateFor(key)();
    const resource = preparedState.resource;
    if (resource === null) {
      if (preparedState.status === 'error') {
        if (isStaleLinkingFailure(preparedState.failureCode)) {
          untracked(() => this.invalidatePreparedGeneration());
          return;
        }
        untracked(() => this.stateSignal.update((state) => ({
          ...state,
          step: 'failed',
          errorMessage: preparedState.errorMessage,
        })));
        untracked(() => this.stopCopy(preparedState.errorMessage ?? LINKING_LABELS.sourceLoadError));
      }
      return;
    }
    const status = resource.status.toLowerCase();
    if (isStaleLinkingFailure(resource.failureCode)) {
      untracked(() => this.invalidatePreparedGeneration());
      return;
    }
    const nextStep: LinkingWorkflowStep = isPreparedPreflightReady(resource)
      ? 'ready'
      : ['failed', 'cancelled', 'expired'].includes(status)
        ? status === 'cancelled' ? 'cancelled' : 'failed'
        : 'preflighting';
    if (
      this.stateSignal().prepared === resource &&
      this.stateSignal().step === nextStep &&
      this.stateSignal().errorMessage === resource.failureCode
    ) {
      return;
    }
    untracked(() => this.stateSignal.update((state) => ({
      ...state,
      prepared: resource,
      step: nextStep,
      errorMessage: resource.failureCode,
    })));
    if (nextStep === 'failed' || nextStep === 'cancelled') {
      untracked(() => this.stopCopy(resource.failureCode ?? LINKING_LABELS.sourceLoadError));
    }
  }

  private synchronizeExecution(): void {
    const execution = this.execution.state();
    const confirmationIdempotencyKey = this.stateSignal().confirmationIdempotencyKey;
    if (
      execution.generation === 0 ||
      this.stateSignal().origin === null ||
      confirmationIdempotencyKey === null ||
      execution.idempotencyKey !== confirmationIdempotencyKey ||
      !['submitting', 'queued', 'running', 'finalizing'].includes(this.stateSignal().step)
    ) {
      return;
    }
    const job = execution.job;
    const failureCode = job?.failureCode ?? execution.failureCode;
    if (isStaleLinkingFailure(failureCode)) {
      untracked(() => this.invalidatePreparedGeneration());
      return;
    }
    const previousStep = this.stateSignal().step;
    let step = previousStep;
    if (execution.status === 'submitting') {
      step = 'submitting';
    } else if (execution.status === 'succeeded') {
      step = 'succeeded';
    } else if (execution.status === 'failed') {
      step = job?.status.toLowerCase() === 'cancelled' ? 'cancelled' : 'failed';
    } else if (job !== null) {
      const stage = job.stage.toLowerCase();
      step = stage === 'finalizing' ? 'finalizing' : stage === 'running' ? 'running' : 'queued';
    }
    if (step !== this.stateSignal().step || execution.errorMessage !== this.stateSignal().errorMessage) {
      untracked(() => this.stateSignal.update((state) => ({
        ...state,
        step,
        errorMessage: execution.errorMessage,
      })));
    }
    if (step === 'succeeded' && previousStep !== 'succeeded') {
      untracked(() => this.doors.refresh());
    }
    if (step === 'failed') {
      untracked(() => this.stopCopy(execution.errorMessage ?? LINKING_LABELS.copyStopped));
    } else if (step === 'cancelled' && this.stateSignal().origin === 'copy') {
      untracked(() => this.dismiss());
    }
  }

  private invalidatePreparedGeneration(): void {
    const state = this.stateSignal();
    if (state.prepared !== null) {
      this.details.evict(state.prepared.preflightId);
    }
    if (state.preparationKey !== null) {
      void this.preparedFacade.acknowledge(state.preparationKey);
      this.preparedFacade.dismiss(state.preparationKey);
    }
    this.workspace.invalidateLinkingDataRevision();
    this.sourcePages.cancel('direct-linking-source');
    this.drafts.requireFreshGeneration();
    this.inlineSource.invalidate(state.operationGeneration + 1);
    this.stateSignal.set({
      ...state,
      step: state.origin === 'source' ? 'configure-source' : 'failed',
      preparationKey: null,
      confirmationIdempotencyKey: null,
      prepared: null,
      errorMessage: LINKING_LABELS.preflightStale,
      operationGeneration: state.operationGeneration + 1,
    });
    this.stopCopy(LINKING_LABELS.preflightStale);
  }

  private currentPreparedInlineSources(): readonly LinkingOperationSourceDraft[] {
    const draft = this.drafts.draft();
    return draft.sourceOrder.flatMap((sourceKey) => {
      const source = draft.sources[sourceKey];
      return source === undefined ? [] : [source];
    });
  }

  private failCopyWorkflow(message: string): void {
    this.stateSignal.update((state) => ({ ...state, step: 'failed', errorMessage: message }));
    this.stopCopy(message);
  }

  private stopCopy(message: string): void {
    if (this.stateSignal().origin !== 'copy') {
      return;
    }
    this.completion.stopCopy(message);
    this.drafts.reset();
  }

  private isLiveDoor(doorId: number | null): boolean {
    if (doorId === null) {
      return false;
    }
    const door = this.doors.snapshot()?.byId.get(doorId);
    return door !== undefined && !door.isArchived && !door.sectionRetired;
  }
}
