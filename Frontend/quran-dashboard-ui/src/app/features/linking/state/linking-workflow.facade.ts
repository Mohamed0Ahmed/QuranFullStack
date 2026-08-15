import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { AbwabSnapshotFacade } from '../../abwab/state/abwab-snapshot.facade';
import { DetailOverlayHistoryService } from '../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { LinkingPreparedPreflightStatusDto } from '../../../core/api/generated/models/linking-prepared-preflight-status-dto';
import { LinkingSourceDescriptor } from '../models/linking-source.models';
import { LinkingManualLinkShape } from '../models/linking-manual-mushaf.models';
import { LinkingOperationSourceDraft } from '../models/linking-operation-draft.models';
import { LinkingSourcePageRequest } from '../models/linking-page.models';
import { isPreparedPreflightReady } from '../models/linking-prepared-preflight.models';
import { LINKING_LABELS } from '../models/linking.labels';
import { LinkingAccessService } from './linking-access.service';
import { LinkingExecutionStore } from './linking-execution.store';
import { LinkingFocusCoordinator } from './linking-focus.coordinator';
import {
  LinkingOperationDraftStore,
  createPreparedLinkingRequest,
  createInlineLinkingDraft,
} from './linking-operation-draft.store';
import { LinkingPreflightDetailsFacade } from './linking-preflight-details.facade';
import { LinkingPreparedPreflightFacade } from './linking-prepared-preflight.facade';
import { LinkingSourcePagesFacade } from './linking-source-pages.facade';
import { LinkingWorkspaceStore } from './linking-workspace.store';
export type LinkingWorkflowStep =
  | 'configure-source'
  | 'door'
  | 'preflighting'
  | 'ready'
  | 'submitting'
  | 'queued'
  | 'running'
  | 'finalizing'
  | 'succeeded'
  | 'failed'
  | 'cancelled';
type LinkingWorkflowOrigin = 'workspace' | 'source';
interface LinkingWorkflowState {
  origin: LinkingWorkflowOrigin | null;
  step: LinkingWorkflowStep;
  directDraft: LinkingOperationSourceDraft | null;
  directTotalAyahCount: number;
  selectedDoorId: number | null;
  preparationKey: string | null;
  prepared: LinkingPreparedPreflightStatusDto | null;
  errorMessage: string | null;
  operationGeneration: number;
}
const INITIAL_WORKFLOW: LinkingWorkflowState = {
  origin: null,
  step: 'configure-source',
  directDraft: null,
  directTotalAyahCount: 0,
  selectedDoorId: null,
  preparationKey: null,
  prepared: null,
  errorMessage: null,
  operationGeneration: 0,
};
const NAVIGABLE_STEPS: readonly LinkingWorkflowStep[] = [
  'configure-source',
  'door',
  'preflighting',
  'ready',
];
@Injectable({ providedIn: 'root' })
export class LinkingWorkflowFacade {
  private readonly access = inject(LinkingAccessService);
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly overlay = inject(DetailOverlayHistoryService);
  private readonly focus = inject(LinkingFocusCoordinator);
  private readonly doors = inject(AbwabSnapshotFacade);
  private readonly drafts = inject(LinkingOperationDraftStore);
  private readonly preparedFacade = inject(LinkingPreparedPreflightFacade);
  private readonly details = inject(LinkingPreflightDetailsFacade);
  private readonly execution = inject(LinkingExecutionStore);
  private readonly sourcePages = inject(LinkingSourcePagesFacade);
  private readonly stateSignal = signal<LinkingWorkflowState>(INITIAL_WORKFLOW);
  private readonly pendingSourceSignal = signal<LinkingSourceDescriptor | null>(null);
  private restoreOverlayFocus = false;
  readonly state = this.stateSignal.asReadonly();
  readonly step = computed(() => this.stateSignal().step);
  readonly selectedDoorId = computed(() => this.stateSignal().selectedDoorId);
  readonly directDraft = computed(() => this.stateSignal().directDraft);
  readonly prepared = computed(() => this.stateSignal().prepared);
  readonly executionState = this.execution.state;
  readonly directSourceRequest = computed<Omit<LinkingSourcePageRequest, 'page'> | null>(() => {
    const draft = this.stateSignal().directDraft;
    if (draft === null) {
      return null;
    }
    return {
      source: draft.descriptor,
      expectedLinkingDataRevision: null,
      expectedSourceViewIdentity: null,
      view: {
        segment: 'all',
        inclusionMode: null,
        ayahOverrideIds: [],
      },
      pageSize: 100,
      draftGeneration: this.stateSignal().operationGeneration,
    };
  });
  readonly directSelectedCount = computed(() => {
    const draft = this.stateSignal().directDraft;
    if (draft === null) {
      return 0;
    }
    return draft.selection.mode === 'all-except'
      ? Math.max(this.stateSignal().directTotalAyahCount - draft.selection.ayahIds.length, 0)
      : draft.selection.ayahIds.length;
  });
  readonly directManualGrouped = computed(() =>
    this.stateSignal().directDraft?.manualLinkShape === 'grouped' && this.directSelectedCount() > 1,
  );
  readonly canAdvanceSource = computed(() =>
    this.stateSignal().origin === 'source' &&
    (this.stateSignal().directDraft?.linkingDataRevision ?? 0) > 0 &&
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
  readonly canSubmit = computed(() => {
    const prepared = this.prepared();
    return this.step() === 'ready' &&
      prepared !== null &&
      prepared.preflightToken !== null &&
      prepared.isBlocked !== true &&
      prepared.isNoOp !== true;
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

  startFromSource(source: LinkingSourceDescriptor): boolean {
    if (!this.access.canUseLinking()) {
      return false;
    }
    if (this.overlay.isOpen()) {
      this.pendingSourceSignal.set(source);
      this.overlay.close();
      return true;
    }
    if (!this.workspace.openOperationFlow()) {
      return false;
    }
    const generation = this.stateSignal().operationGeneration + 1;
    this.stateSignal.set({
      ...INITIAL_WORKFLOW,
      origin: 'source',
      directDraft: createInlineLinkingDraft(source),
      operationGeneration: generation,
    });
    return true;
  }

  startWorkspaceOperation(): void {
    void this.startWorkspaceAfterFlush();
  }

  directPageReady(linkingDataRevision: number, totalAyahCount: number): void {
    const draft = this.stateSignal().directDraft;
    if (draft === null) {
      return;
    }
    if (
      draft.linkingDataRevision === linkingDataRevision &&
      this.stateSignal().directTotalAyahCount === totalAyahCount
    ) {
      return;
    }
    const updated = { ...draft, linkingDataRevision };
    this.stateSignal.update((state) => ({
      ...state,
      directDraft: updated,
      directTotalAyahCount: totalAyahCount,
    }));
    this.drafts.replace([updated], linkingDataRevision, this.selectedDoorId());
  }

  toggleDirectAyah(ayahId: number): void {
    this.updateDirectDraft((draft) => {
      const overrides = new Set(draft.selection.ayahIds);
      overrides.has(ayahId) ? overrides.delete(ayahId) : overrides.add(ayahId);
      return {
        ...draft,
        selection: { ...draft.selection, ayahIds: [...overrides].sort((left, right) => left - right) },
      };
    });
  }

  selectAllDirectAyahs(): void {
    this.updateDirectDraft((draft) => ({
      ...draft,
      selection: { mode: 'all-except', ayahIds: [] },
    }));
  }

  clearAllDirectAyahs(): void {
    this.updateDirectDraft((draft) => ({ ...draft, selection: { mode: 'only', ayahIds: [] } }));
  }

  toggleDirectManualWord(ayahId: number, quranWordId: number): void {
    this.updateDirectDraft((draft) => {
      const selected = new Set(draft.selectedWordIdsByAyahId[ayahId] ?? []);
      selected.has(quranWordId) ? selected.delete(quranWordId) : selected.add(quranWordId);
      return {
        ...draft,
        selectedWordIdsByAyahId: {
          ...draft.selectedWordIdsByAyahId,
          [ayahId]: [...selected].sort((left, right) => left - right),
        },
      };
    });
  }

  setDirectAutomaticWords(enabled: boolean): void {
    this.updateDirectDraft((draft) => ({ ...draft, automaticWordMatchesEnabled: enabled }));
  }

  setDirectManualLinkShape(linkShape: LinkingManualLinkShape): void {
    this.updateDirectDraft((draft) => ({ ...draft, manualLinkShape: linkShape }));
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

  next(): void {
    if (this.step() === 'configure-source' && this.canAdvanceSource()) {
      this.doors.load();
      this.stateSignal.update((state) => ({ ...state, step: 'door' }));
      return;
    }
    if (this.step() === 'door' && this.canAdvanceDoor()) {
      void this.runPreflight();
    }
  }

  retryPreflight(): void {
    if (this.stateSignal().origin !== null && this.selectedDoorId() !== null) {
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
    this.stateSignal.update((state) => ({ ...state, step: 'submitting', errorMessage: null }));
    void this.execution.execute(
      preparationKey,
      prepared.preflightId,
      prepared.preflightToken,
      crypto.randomUUID(),
    );
  }

  cancelExecution(): void {
    if (this.canCancelExecution()) {
      void this.execution.cancel();
    }
  }

  canNavigateTo(target: LinkingWorkflowStep): boolean {
    const currentIndex = NAVIGABLE_STEPS.indexOf(this.step());
    const targetIndex = NAVIGABLE_STEPS.indexOf(target);
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
      prepared: null,
      errorMessage: null,
    }));
  }

  async acknowledgeSuccess(): Promise<void> {
    await this.execution.acknowledge();
    if (this.stateSignal().origin === 'workspace') {
      this.workspace.clearCheckedSources();
    }
    this.dismiss();
  }

  dismiss(): void {
    const state = this.stateSignal();
    const acceptedJob = this.execution.state().job !== null || this.execution.state().outcome !== null;
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
    this.pendingSourceSignal.set(null);
    this.stateSignal.set({ ...INITIAL_WORKFLOW, operationGeneration: state.operationGeneration + 1 });
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
    this.doors.load();
    this.stateSignal.set({
      ...INITIAL_WORKFLOW,
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
    const request = createPreparedLinkingRequest(
      preparationKey,
      doorId,
      this.stateSignal().origin === 'source' ? requireDirectDraft(this.stateSignal()) : null,
      this.workspace.items(),
      this.workspace.checkedSourceKeys(),
    );
    this.stateSignal.update((state) => ({
      ...state,
      step: 'preflighting',
      preparationKey,
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
        if (isStaleFailure(preparedState.failureCode)) {
          untracked(() => this.invalidatePreparedGeneration(preparedState.failureCode!));
          return;
        }
        untracked(() => this.stateSignal.update((state) => ({
          ...state,
          step: 'failed',
          errorMessage: preparedState.errorMessage,
        })));
      }
      return;
    }
    const status = resource.status.toLowerCase();
    if (isStaleFailure(resource.failureCode)) {
      untracked(() => this.invalidatePreparedGeneration(resource.failureCode!));
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
  }

  private synchronizeExecution(): void {
    const execution = this.execution.state();
    if (execution.generation === 0 || this.stateSignal().origin === null) {
      return;
    }
    const job = execution.job;
    const failureCode = job?.failureCode ?? execution.failureCode;
    if (isStaleFailure(failureCode)) {
      untracked(() => this.invalidatePreparedGeneration(failureCode!));
      return;
    }
    let step = this.stateSignal().step;
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
  }

  private invalidatePreparedGeneration(message: string): void {
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
    this.stateSignal.set({
      ...state,
      step: state.origin === 'source' ? 'configure-source' : 'failed',
      directDraft:
        state.directDraft === null
          ? null
          : {
              ...state.directDraft,
              linkingDataRevision: 0,
              selection: { mode: 'all-except', ayahIds: [] },
              selectedWordIdsByAyahId: {},
            },
      directTotalAyahCount: 0,
      preparationKey: null,
      prepared: null,
      errorMessage: message,
      operationGeneration: state.operationGeneration + 1,
    });
  }

  private updateDirectDraft(
    update: (draft: LinkingOperationSourceDraft) => LinkingOperationSourceDraft,
    generation = this.stateSignal().operationGeneration,
  ): void {
    const draft = this.stateSignal().directDraft;
    if (draft === null) {
      return;
    }
    const updated = update(draft);
    this.stateSignal.update((state) => ({
      ...state,
      directDraft: updated,
      operationGeneration: generation,
    }));
    if (updated.linkingDataRevision > 0) {
      this.drafts.replace([updated], updated.linkingDataRevision, this.selectedDoorId());
    }
  }

  private isLiveDoor(doorId: number | null): boolean {
    if (doorId === null) {
      return false;
    }
    const door = this.doors.snapshot()?.byId.get(doorId);
    return door !== undefined && !door.isArchived && !door.sectionRetired;
  }
}
function requireDirectDraft(state: LinkingWorkflowState): LinkingOperationSourceDraft {
  if (state.directDraft === null) {
    throw new Error('إعداد المصدر المباشر غير متاح.');
  }
  return state.directDraft;
}
function isStaleFailure(code: string | null): boolean {
  return code !== null && [
    'LINKING_DATA_STALE',
    'SOURCE_VIEW_STALE',
    'WORKSPACE_SOURCE_STALE',
    'PREFLIGHT_STALE',
  ].includes(code);
}
