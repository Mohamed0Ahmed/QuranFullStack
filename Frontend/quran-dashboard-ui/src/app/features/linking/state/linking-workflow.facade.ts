import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { Subscription } from 'rxjs';

import { AbwabNode } from '../../abwab/models/abwab.models';
import { AbwabSnapshotFacade } from '../../abwab/state/abwab-snapshot.facade';
import { DetailOverlayHistoryService } from '../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { LINKING_COMMAND_PORT } from '../data-access/linking-command.port';
import { LINKING_LABELS } from '../models/linking.labels';
import { LinkingOperationMember } from '../models/linking-operation.models';
import { LinkingSourceDescriptor } from '../models/linking-source.models';
import { LinkingSourceSetOperationResult } from '../models/linking-workflow.models';
import { LinkingSourceConfiguration } from '../models/linking-workspace.models';
import { ephemeralLinkingOperationMember } from '../utils/linking-operation-members';
import { DEFAULT_LINKING_SELECTION } from '../utils/linking-selection';
import { LinkingAccessService } from './linking-access.service';
import { LinkingSourceSetCoordinator } from './linking-source-set.coordinator';
import { LinkingWorkspaceStore } from './linking-workspace.store';

export type LinkingWorkflowStep = 'configure-source' | 'resolve' | 'door' | 'review' | 'submitting' | 'success' | 'error';
type LinkingWorkflowOrigin = 'workspace' | 'source';

interface LinkingWorkflowState {
  origin: LinkingWorkflowOrigin | null;
  step: LinkingWorkflowStep;
  members: readonly LinkingOperationMember[];
  directConfiguration: LinkingSourceConfiguration | null;
  operation: LinkingSourceSetOperationResult | null;
  selectedDoorId: number | null;
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
  errorMessage: null,
  resultMessage: null,
  operationGeneration: 0,
};

@Injectable({ providedIn: 'root' })
export class LinkingWorkflowFacade {
  private readonly access = inject(LinkingAccessService);
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly overlay = inject(DetailOverlayHistoryService);
  private readonly doors = inject(AbwabSnapshotFacade);
  private readonly sourceSet = inject(LinkingSourceSetCoordinator);
  private readonly commandPort = inject(LINKING_COMMAND_PORT);
  private readonly stateSignal = signal<LinkingWorkflowState>(INITIAL_WORKFLOW);
  private commandSubscription: Subscription | null = null;
  private pendingSource: LinkingSourceDescriptor | null = null;

  readonly state = this.stateSignal.asReadonly();
  readonly step = computed(() => this.stateSignal().step);
  readonly selectedDoorId = computed(() => this.stateSignal().selectedDoorId);
  readonly sourceSetState = this.sourceSet.state;
  readonly operation = computed(() => this.stateSignal().operation);
  readonly memberStates = this.sourceSet.memberStates;
  readonly directSource = computed(() => this.stateSignal().members[0]?.source ?? null);
  readonly directConfiguration = computed(() => this.stateSignal().directConfiguration);
  readonly canAdvanceDoor = computed(() => this.access.canUseLinking() && this.isLiveDoor(this.selectedDoorId()));
  readonly canSubmit = computed(() => this.canAdvanceDoor() && this.operation()?.mergedSelection.ayahs.length !== 0);

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
    this.workspace.openEphemeralDirectLink();
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
    this.workspace.openEphemeralDirectLink();
    this.stateSignal.set({ ...INITIAL_WORKFLOW, origin: 'workspace', members });
    this.resolve();
  }

  setDirectAutomaticWords(enabled: boolean): void {
    const state = this.stateSignal();
    if (state.origin !== 'source' || state.directConfiguration?.kind !== 'automatic') {
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
    const generation = this.sourceSet.state().generation + 1;
    this.stateSignal.update((current) => ({
      ...current,
      operation: null,
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
    if (this.step() === 'configure-source') {
      this.resolve();
    } else if (this.step() === 'door' && this.canAdvanceDoor()) {
      this.stateSignal.update((state) => ({ ...state, step: 'review' }));
    }
  }

  back(): void {
    const step = this.step();
    if (step === 'review') {
      this.stateSignal.update((state) => ({ ...state, step: 'door' }));
      return;
    }
    if (step === 'door' && this.state().origin === 'source') {
      this.stateSignal.update((state) => ({ ...state, step: 'configure-source' }));
      return;
    }
    this.dismiss();
  }

  submit(): void {
    const operation = this.operation();
    const doorId = this.selectedDoorId();
    if (!this.canSubmit() || operation === null || doorId === null || this.commandSubscription !== null) {
      return;
    }
    this.stateSignal.update((state) => ({ ...state, step: 'submitting', errorMessage: null }));
    this.commandSubscription = this.commandPort.execute({ doorId, operation }).subscribe({
      next: (result) => this.stateSignal.update((state) => ({ ...state, step: 'success', resultMessage: result.message })),
      error: (error: unknown) => this.stateSignal.update((state) => ({
        ...state,
        step: 'error',
        errorMessage: error instanceof Error ? error.message : LINKING_LABELS.sourceLoadError,
      })),
      complete: () => { this.commandSubscription = null; },
    });
  }

  acknowledgeSuccess(): void {
    if (this.step() !== 'success') {
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
    this.pendingSource = null;
    this.sourceSet.cancel();
    this.stateSignal.set(INITIAL_WORKFLOW);
    origin === 'workspace' ? this.workspace.openWorkspace() : this.workspace.close();
  }

  private isLiveDoor(doorId: number | null): boolean {
    return doorId !== null && (this.doors.snapshot()?.liveRoots.some((node) => containsDoor(node, doorId)) ?? false);
  }
}

function defaultConfiguration(source: LinkingSourceDescriptor): LinkingSourceConfiguration {
  return source.kind === 'manual-mushaf-ayahs'
    ? { kind: 'manual', ayahInclusion: DEFAULT_LINKING_SELECTION, wordLocationsByVerseKey: {}, linkShape: 'independent' }
    : { kind: 'automatic', ayahInclusion: DEFAULT_LINKING_SELECTION, automaticWordMatchesEnabled: true };
}

function containsDoor(node: AbwabNode, doorId: number): boolean {
  return node.id === doorId || node.children.some((child) => containsDoor(child, doorId));
}

function firstError(members: readonly { errorMessage: string | null }[]): string {
  return members.find((member) => member.errorMessage !== null)?.errorMessage ?? LINKING_LABELS.sourceLoadError;
}
