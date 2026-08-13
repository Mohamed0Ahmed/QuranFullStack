import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { Subscription } from 'rxjs';

import { AbwabNode } from '../../abwab/models/abwab.models';
import { AbwabSnapshotFacade } from '../../abwab/state/abwab-snapshot.facade';
import { DetailOverlayHistoryService } from '../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { LINKING_COMMAND_PORT, LinkingPreflightStaleError } from '../data-access/linking-command.port';
import { LinkingPreflightApi } from '../data-access/linking-preflight.api';
import { LinkingPreflightResult } from '../models/linking-preflight.models';
import { LINKING_LABELS } from '../models/linking.labels';
import { LinkingOperationMember } from '../models/linking-operation.models';
import { LinkingSourceDescriptor } from '../models/linking-source.models';
import { LinkingSourceSetOperationResult } from '../models/linking-workflow.models';
import { LinkingSourceConfiguration } from '../models/linking-workspace.models';
import { ephemeralLinkingOperationMember } from '../utils/linking-operation-members';
import { DEFAULT_LINKING_SELECTION } from '../utils/linking-selection';
import { LinkingAccessService } from './linking-access.service';
import { LinkingFocusCoordinator } from './linking-focus.coordinator';
import { LinkingSourceSetCoordinator } from './linking-source-set.coordinator';
import { LinkingWorkspaceStore } from './linking-workspace.store';

export type LinkingWorkflowStep =
  | 'configure-source'
  | 'resolve'
  | 'door'
  | 'preflight'
  | 'submitting'
  | 'success'
  | 'error';
type LinkingWorkflowOrigin = 'workspace' | 'source';
type LinkingPreflightStatus = 'idle' | 'loading' | 'ready' | 'error';

interface LinkingWorkflowState {
  origin: LinkingWorkflowOrigin | null;
  step: LinkingWorkflowStep;
  members: readonly LinkingOperationMember[];
  directConfiguration: LinkingSourceConfiguration | null;
  operation: LinkingSourceSetOperationResult | null;
  selectedDoorId: number | null;
  preflightStatus: LinkingPreflightStatus;
  preflight: LinkingPreflightResult | null;
  preflightMessage: string | null;
  errorMessage: string | null;
  resultMessage: string | null;
  operationGeneration: number;
}

const INITIAL_WORKFLOW: LinkingWorkflowState = {
  origin: null,
  step: 'configure-source',
  members: [],
  directConfiguration: null,
  operation: null,
  selectedDoorId: null,
  preflightStatus: 'idle',
  preflight: null,
  preflightMessage: null,
  errorMessage: null,
  resultMessage: null,
  operationGeneration: 0,
};

const PROGRESS_STEPS: readonly LinkingWorkflowStep[] = [
  'configure-source',
  'resolve',
  'door',
  'preflight',
];

@Injectable({ providedIn: 'root' })
export class LinkingWorkflowFacade {
  private readonly access = inject(LinkingAccessService);
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly overlay = inject(DetailOverlayHistoryService);
  private readonly focus = inject(LinkingFocusCoordinator);
  private readonly doors = inject(AbwabSnapshotFacade);
  private readonly sourceSet = inject(LinkingSourceSetCoordinator);
  private readonly commandPort = inject(LINKING_COMMAND_PORT);
  private readonly preflightApi = inject(LinkingPreflightApi);
  private readonly stateSignal = signal<LinkingWorkflowState>(INITIAL_WORKFLOW);
  private commandSubscription: Subscription | null = null;
  private preflightSubscription: Subscription | null = null;
  private pendingSource: LinkingSourceDescriptor | null = null;
  private restoreOverlayFocus = false;
  private attemptIdempotencyKey: string | null = null;

  readonly state = this.stateSignal.asReadonly();
  readonly step = computed(() => this.stateSignal().step);
  readonly selectedDoorId = computed(() => this.stateSignal().selectedDoorId);
  readonly sourceSetState = this.sourceSet.state;
  readonly operation = computed(() => this.stateSignal().operation);
  readonly memberStates = this.sourceSet.memberStates;
  readonly directConfiguration = computed(() => this.stateSignal().directConfiguration);
  readonly canAdvanceDoor = computed(() => this.access.canUseLinking() && this.isLiveDoor(this.selectedDoorId()));
  readonly preflight = computed(() => this.stateSignal().preflight);
  readonly preflightStatus = computed(() => this.stateSignal().preflightStatus);
  readonly preflightMessage = computed(() => this.stateSignal().preflightMessage);
  readonly canAdvancePreflight = computed(() => {
    const preflight = this.stateSignal().preflight;
    return this.stateSignal().preflightStatus === 'ready' &&
      preflight !== null &&
      !preflight.isBlocked &&
      !preflight.isNoOp;
  });
  readonly canSubmit = computed(
    () =>
      this.canAdvanceDoor() &&
      this.canAdvancePreflight() &&
      this.operation()?.mergedSelection.ayahs.length !== 0,
  );

  constructor() {
    effect(() => {
      const sourceSet = this.sourceSet.state();
      const state = this.stateSignal();
      if (state.step !== 'resolve' || sourceSet.generation !== state.operationGeneration) {
        return;
      }
      if (sourceSet.result !== null) {
        untracked(() => {
          this.doors.load();
          this.stateSignal.update((current) => ({ ...current, operation: sourceSet.result, step: 'door' }));
        });
      } else if (sourceSet.members.some((member) => member.status === 'error')) {
        untracked(() => this.stateSignal.update((current) => ({ ...current, step: 'error', errorMessage: firstError(sourceSet.members) })));
      }
    });
    effect(() => {
      if (!this.access.canUseLinking() && this.stateSignal().origin !== null) {
        untracked(() => this.dismiss());
      }
      if (this.pendingSource !== null && !this.overlay.isOpen()) {
        const source = this.pendingSource;
        this.pendingSource = null;
        untracked(() => this.startFromSource(source));
      }
      if (this.restoreOverlayFocus && this.overlay.isOpen()) {
        this.restoreOverlayFocus = false;
        untracked(() => this.focus.restore());
      }
    });
  }

  startFromSource(source: LinkingSourceDescriptor): void {
    if (!this.access.canUseLinking()) {
      return;
    }
    if (this.overlay.isOpen()) {
      this.pendingSource = source;
      this.overlay.close();
      return;
    }
    this.workspace.openOperationFlow();
    const configuration = defaultConfiguration(source);
    this.stateSignal.set({
      ...INITIAL_WORKFLOW,
      origin: 'source',
      members: [ephemeralLinkingOperationMember(source, configuration)],
      directConfiguration: configuration,
    });
  }

  startWorkspaceOperation(): void {
    const members = this.workspace.captureOperationMembers();
    if (!this.access.canUseLinking() || members.length === 0) {
      return;
    }
    this.workspace.openOperationFlow();
    this.stateSignal.set({ ...INITIAL_WORKFLOW, origin: 'workspace', members });
    this.resolve();
  }

  setDirectAutomaticWords(enabled: boolean): void {
    const state = this.stateSignal();
    if (!this.access.canUseLinking() || state.origin !== 'source' || state.directConfiguration?.kind !== 'automatic') {
      return;
    }
    const configuration = { ...state.directConfiguration, automaticWordMatchesEnabled: enabled };
    this.stateSignal.set({
      ...state,
      directConfiguration: configuration,
      members: [ephemeralLinkingOperationMember(state.members[0].source, configuration)],
    });
  }

  resolve(): void {
    const state = this.stateSignal();
    if (!this.access.canUseLinking() || state.members.length === 0) {
      return;
    }
    this.cancelPreflight();
    const generation = this.sourceSet.state().generation + 1;
    this.stateSignal.update((current) => ({
      ...current,
      operation: null,
      preflightStatus: 'idle',
      preflight: null,
      preflightMessage: null,
      errorMessage: null,
      step: 'resolve',
      operationGeneration: generation,
    }));
    this.sourceSet.resolve(state.members);
  }

  retry(): void {
    this.resolve();
  }

  loadDoors(): void {
    if (this.access.canUseLinking()) {
      this.doors.load();
    }
  }

  selectDoor(doorId: number): void {
    if (this.access.canUseLinking() && this.isLiveDoor(doorId)) {
      this.stateSignal.update((state) => ({ ...state, selectedDoorId: doorId }));
    }
  }

  next(): void {
    const step = this.step();
    if (step === 'configure-source') {
      this.resolve();
      return;
    }
    if (step === 'door' && this.canAdvanceDoor()) {
      this.runPreflight();
      return;
    }
  }

  retryPreflight(): void {
    if (this.step() === 'preflight') {
      this.runPreflight();
    }
  }

  canNavigateTo(target: LinkingWorkflowStep): boolean {
    const state = this.stateSignal();
    const currentIndex = PROGRESS_STEPS.indexOf(state.step);
    const targetIndex = PROGRESS_STEPS.indexOf(target);
    if (currentIndex < 0 || targetIndex < 0 || targetIndex >= currentIndex) {
      return false;
    }

    switch (target) {
      case 'configure-source':
        return state.origin === 'source';
      case 'resolve':
        return state.members.length > 0;
      case 'door':
        return state.operation !== null;
      case 'preflight':
        return state.preflightStatus === 'ready' && state.preflight !== null;
      default:
        return false;
    }
  }

  navigateTo(target: LinkingWorkflowStep): void {
    if (!this.canNavigateTo(target)) {
      return;
    }

    if (target === 'resolve') {
      this.resolve();
      return;
    }

    if (target === 'configure-source') {
      this.sourceSet.cancel();
      this.cancelPreflight();
      this.stateSignal.update((state) => ({
        ...state,
        step: target,
        operation: null,
        preflightStatus: 'idle',
        preflight: null,
        preflightMessage: null,
        errorMessage: null,
      }));
      return;
    }

    if (target === 'door') {
      this.cancelPreflight();
      this.stateSignal.update((state) => ({
        ...state,
        step: target,
        preflightStatus: 'idle',
        preflight: null,
        preflightMessage: null,
        errorMessage: null,
      }));
      return;
    }

    this.stateSignal.update((state) => ({ ...state, step: target }));
  }

  submit(): void {
    const state = this.stateSignal();
    const operation = state.operation;
    const doorId = state.selectedDoorId;
    const preflight = state.preflight;
    if (
      !this.canSubmit() ||
      state.step !== 'preflight' ||
      operation === null ||
      doorId === null ||
      preflight === null ||
      this.commandSubscription !== null
    ) {
      return;
    }
    this.attemptIdempotencyKey ??= crypto.randomUUID();
    this.stateSignal.update((current) => ({ ...current, step: 'submitting', errorMessage: null }));
    this.commandSubscription = this.commandPort
      .execute({
        doorId,
        operation,
        preflightToken: preflight.preflightToken,
        idempotencyKey: this.attemptIdempotencyKey,
        preflightSources: preflight.sources,
      })
      .subscribe({
        next: (result) => {
          this.attemptIdempotencyKey = null;
          this.stateSignal.update((current) => ({
            ...current,
            step: 'success',
            resultMessage: result.message,
          }));
        },
        error: (error: unknown) => {
          this.commandSubscription = null;
          if (error instanceof LinkingPreflightStaleError) {
            this.runPreflight(error.message);
            return;
          }
          this.stateSignal.update((current) => ({
            ...current,
            step: 'error',
            errorMessage: error instanceof Error ? error.message : LINKING_LABELS.sourceLoadError,
          }));
        },
        complete: () => {
          this.commandSubscription = null;
        },
      });
  }

  private runPreflight(staleMessage: string | null = null): void {
    const state = this.stateSignal();
    const operation = state.operation;
    const doorId = state.selectedDoorId;
    if (!this.access.canUseLinking() || operation === null || doorId === null) {
      return;
    }
    this.cancelPreflight();
    this.stateSignal.update((current) => ({
      ...current,
      step: 'preflight',
      preflightStatus: 'loading',
      preflight: null,
      preflightMessage: staleMessage,
      errorMessage: null,
    }));
    this.preflightSubscription = this.preflightApi
      .preflight(doorId, operation.sourceIntents)
      .subscribe({
        next: (preflight) =>
          this.stateSignal.update((current) => ({
            ...current,
            preflight,
            preflightStatus: 'ready',
          })),
        error: (error: unknown) =>
          this.stateSignal.update((current) => ({
            ...current,
            preflightStatus: 'error',
            preflightMessage: error instanceof Error ? error.message : LINKING_LABELS.sourceLoadError,
          })),
        complete: () => {
          this.preflightSubscription = null;
        },
      });
  }

  private cancelPreflight(): void {
    this.preflightSubscription?.unsubscribe();
    this.preflightSubscription = null;
  }

  acknowledgeSuccess(): void {
    if (!this.access.canUseLinking() || this.step() !== 'success') {
      return;
    }
    if (this.state().origin === 'workspace') {
      this.workspace.clearCheckedSources();
    }
    this.dismiss();
  }

  dismiss(): void {
    const origin = this.stateSignal().origin;
    this.commandSubscription?.unsubscribe();
    this.commandSubscription = null;
    this.cancelPreflight();
    this.attemptIdempotencyKey = null;
    this.pendingSource = null;
    this.sourceSet.cancel();
    this.stateSignal.set(INITIAL_WORKFLOW);
    if (origin === 'workspace') {
      this.workspace.openWorkspace();
      return;
    }

    this.workspace.close();
    if (this.overlay.isRetainedClosed()) {
      this.restoreOverlayFocus = true;
      this.overlay.restore();
      return;
    }
    this.focus.restore();
  }

  private isLiveDoor(doorId: number | null): boolean {
    return doorId !== null && (this.doors.snapshot()?.liveRoots.some((node) => containsDoor(node, doorId)) ?? false);
  }
}

function defaultConfiguration(source: LinkingSourceDescriptor): LinkingSourceConfiguration {
  return source.kind === 'manual-mushaf-ayahs'
    ? { kind: 'manual', ayahInclusion: DEFAULT_LINKING_SELECTION, quranWordIdsByVerseKey: {}, linkShape: 'independent' }
    : { kind: 'automatic', ayahInclusion: DEFAULT_LINKING_SELECTION, automaticWordMatchesEnabled: true };
}

function containsDoor(node: AbwabNode, doorId: number): boolean {
  return node.id === doorId || node.children.some((child) => containsDoor(child, doorId));
}

function firstError(members: readonly { errorMessage: string | null }[]): string {
  return members.find((member) => member.errorMessage !== null)?.errorMessage ?? LINKING_LABELS.sourceLoadError;
}
