import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { Subscription } from 'rxjs';

import { DetailOverlayHistoryService } from '../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { AbwabSnapshotFacade } from '../../abwab/state/abwab-snapshot.facade';
import { LINKING_COMMAND_PORT } from '../data-access/linking-command.port';
import { LinkingSourceResolver } from '../data-access/linking-source-resolver';
import { LINKING_LABELS } from '../models/linking.labels';
import { LinkingSourceDescriptor } from '../models/linking-source.models';
import {
  DirectLinkOrigin,
  DirectLinkWorkflowState,
  LinkingSourceLoadState,
  previousDirectLinkStep,
} from '../models/linking-workflow.models';
import { LinkingSelection } from '../models/linking-workspace.models';
import {
  DEFAULT_LINKING_SELECTION,
  clearLinkingAyahs,
  reconcileLinkingSelection,
  selectAllLinkingAyahs,
  selectedLinkingAyahCount,
  selectedLinkingVerseKeys,
  toggleLinkingSelection,
} from '../utils/linking-selection';
import { LinkingAccessService } from './linking-access.service';
import { LinkingWorkspaceStore } from './linking-workspace.store';

const INITIAL_SOURCE_LOAD: LinkingSourceLoadState = {
  status: 'idle',
  ayahs: [],
  progress: { loaded: 0, total: null },
  errorMessage: null,
};

const INITIAL_WORKFLOW: DirectLinkWorkflowState = {
  source: null,
  sourceKey: null,
  capturedConfigurationRevision: null,
  origin: null,
  step: 'door',
  selectedDoorId: null,
  doorNotice: null,
  selection: DEFAULT_LINKING_SELECTION,
  highlightSourceWords: true,
  sourceLoad: INITIAL_SOURCE_LOAD,
  result: null,
};

@Injectable({ providedIn: 'root' })
export class LinkingWorkflowFacade {
  private readonly access = inject(LinkingAccessService);
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly overlay = inject(DetailOverlayHistoryService);
  private readonly abwabSnapshot = inject(AbwabSnapshotFacade);
  private readonly resolver = inject(LinkingSourceResolver);
  private readonly commandPort = inject(LINKING_COMMAND_PORT);
  private readonly workflowState = signal<DirectLinkWorkflowState>(INITIAL_WORKFLOW);
  private pendingSourceStart: LinkingSourceDescriptor | null = null;
  private pendingOrigin: DirectLinkOrigin | null = null;
  private sourceLoadSubscription: Subscription | null = null;

  readonly state = this.workflowState.asReadonly();
  readonly source = computed(() => this.workflowState().source);
  readonly step = computed(() => this.workflowState().step);
  readonly selectedDoorId = computed(() => this.workflowState().selectedDoorId);
  readonly sourceLoad = computed(() => this.workflowState().sourceLoad);
  readonly allAyahs = computed(() => this.sourceLoad().ayahs);
  readonly selectedVerseKeys = computed(() => selectedLinkingVerseKeys(this.workflowState().selection, this.ayahVerseKeys()));
  readonly selectedCount = computed(() => selectedLinkingAyahCount(this.workflowState().selection, this.ayahVerseKeys()));
  readonly highlightSourceWords = computed(() => this.workflowState().highlightSourceWords);
  readonly selectedDoor = computed(() => {
    const doorId = this.selectedDoorId();
    const snapshot = this.abwabSnapshot.snapshot();
    if (doorId === null || snapshot === null) {
      return null;
    }
    const door = snapshot.byId.get(doorId);
    if (!door) {
      return null;
    }
    return {
      id: door.id,
      name: door.name,
      sectionName: snapshot.sections.find((section) => section.id === door.sectionId)?.name ?? null,
    };
  });
  readonly canAdvanceDoor = computed(() => this.selectedDoor() !== null && this.access.canUseLinking());
  readonly canAdvanceAyahs = computed(
    () => this.access.canUseLinking() && this.sourceLoad().status === 'success' && this.selectedCount() > 0,
  );

  constructor() {
    effect(() => {
      if (!this.access.canUseLinking() && this.workflowState().source !== null) {
        untracked(() => this.resetAndClose());
      }
    });
    effect(() => {
      const selectedDoorId = this.selectedDoorId();
      if (selectedDoorId !== null && this.abwabSnapshot.snapshot() !== null && this.selectedDoor() === null) {
        untracked(() =>
          this.workflowState.update((state) => ({
            ...state,
            step: 'door',
            selectedDoorId: null,
            doorNotice: LINKING_LABELS.selectedDoorUnavailable,
          })),
        );
      }
    });
    effect(() => {
      if (this.pendingSourceStart !== null && !this.overlay.isOpen()) {
        const source = this.pendingSourceStart;
        const origin = this.pendingOrigin;
        this.pendingSourceStart = null;
        this.pendingOrigin = null;
        if (origin !== null) {
          untracked(() => this.activate(source, origin));
        }
      }
    });
  }

  startFromWorkspace(sourceKey: string): void {
    const item = this.workspace.item(sourceKey);
    if (!item || !this.access.canUseLinking()) {
      this.workspace.openWorkspace();
      return;
    }
    const state = this.workflowState();
    if (state.origin === 'workspace' && state.sourceKey === sourceKey && state.source !== null) {
      return;
    }
    this.activate(
      item.source,
      'workspace',
      sourceKey,
      item.selection,
      item.highlightSourceWords,
      item.configurationRevision,
    );
  }

  startFromSource(source: LinkingSourceDescriptor): void {
    if (!this.access.canUseLinking()) {
      return;
    }
    if (this.overlay.isOpen()) {
      this.pendingSourceStart = source;
      this.pendingOrigin = 'source';
      this.overlay.close();
      return;
    }
    this.activate(source, 'source');
  }

  loadDoors(): void {
    if (this.access.canUseLinking() && this.step() === 'door') {
      this.abwabSnapshot.load();
    }
  }

  selectDoor(doorId: number): void {
    if (!this.access.canUseLinking() || !this.abwabSnapshot.snapshot()?.byId.has(doorId)) {
      return;
    }
    this.workflowState.update((state) => ({
      ...state,
      selectedDoorId: state.selectedDoorId === doorId ? null : doorId,
      doorNotice: null,
    }));
  }

  next(): void {
    if (!this.access.canUseLinking()) {
      return;
    }
    switch (this.step()) {
      case 'door':
        if (this.canAdvanceDoor()) {
          this.workflowState.update((state) => ({ ...state, step: 'ayahs' }));
        }
        return;
      case 'ayahs':
        if (this.canAdvanceAyahs()) {
          this.workflowState.update((state) => ({ ...state, step: 'highlight' }));
        }
        return;
      case 'highlight':
        if (this.canAdvanceAyahs()) {
          this.workflowState.update((state) => ({ ...state, step: 'review' }));
        }
        return;
      default:
        return;
    }
  }

  toggleAyah(verseKey: string): void {
    const state = this.workflowState();
    if (!this.access.canUseLinking() || state.sourceLoad.status !== 'success') {
      return;
    }
    this.setSelection(toggleLinkingSelection(state.selection, verseKey, this.ayahVerseKeys()));
  }

  selectAllAyahs(): void {
    if (this.access.canUseLinking() && this.sourceLoad().status === 'success') {
      this.setSelection(selectAllLinkingAyahs());
    }
  }

  clearAllAyahs(): void {
    if (this.access.canUseLinking() && this.sourceLoad().status === 'success') {
      this.setSelection(clearLinkingAyahs());
    }
  }

  setHighlightSourceWords(highlightSourceWords: boolean): void {
    const state = this.workflowState();
    if (!this.access.canUseLinking()) {
      return;
    }
    this.workflowState.update((current) => ({ ...current, highlightSourceWords }));
    if (state.sourceKey !== null) {
      this.workspace.setHighlightSourceWords(state.sourceKey, highlightSourceWords);
    }
  }

  confirm(): void {
    const state = this.workflowState();
    const door = this.selectedDoor();
    if (
      !this.access.canUseLinking() ||
      state.step !== 'review' ||
      state.source === null ||
      state.sourceLoad.status !== 'success' ||
      door === null ||
      this.selectedVerseKeys().length === 0
    ) {
      return;
    }
    try {
      const result = this.commandPort.execute({
        source: state.source,
        doorId: door.id,
        selectedVerseKeys: this.selectedVerseKeys(),
        highlightSourceWords: state.highlightSourceWords,
      });
      this.workflowState.update((current) => ({ ...current, step: 'result', result }));
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : LINKING_LABELS.sourceLoadError;
      this.workflowState.update((current) => ({ ...current, doorNotice: message }));
    }
  }

  back(): void {
    const state = this.workflowState();
    if (!this.access.canUseLinking()) {
      this.resetAndClose();
      return;
    }
    if (state.step !== 'door') {
      const nextStep = previousDirectLinkStep(state.step);
      this.workflowState.update((current) => ({ ...current, step: nextStep }));
      if (nextStep === 'door') {
        this.loadDoors();
      }
      return;
    }
    this.dismiss();
  }

  dismiss(): void {
    const origin = this.workflowState().origin;
    this.reset();
    if (origin === 'workspace') {
      this.workspace.openWorkspace();
    } else {
      this.workspace.close();
    }
  }

  retrySource(): void {
    const state = this.workflowState();
    if (state.source !== null && this.access.canUseLinking()) {
      this.resolveSource(state.source, state.sourceKey, state.capturedConfigurationRevision);
    }
  }

  private activate(
    source: LinkingSourceDescriptor,
    origin: DirectLinkOrigin,
    sourceKey: string | null = null,
    selection: LinkingSelection = DEFAULT_LINKING_SELECTION,
    highlightSourceWords = true,
    capturedConfigurationRevision: number | null = null,
  ): void {
    if (!this.access.canUseLinking()) {
      return;
    }
    this.cancelSourceLoad();
    this.workflowState.set({
      source,
      sourceKey,
      capturedConfigurationRevision,
      origin,
      step: 'door',
      selectedDoorId: null,
      doorNotice: null,
      selection,
      highlightSourceWords,
      sourceLoad: INITIAL_SOURCE_LOAD,
      result: null,
    });
    if (origin === 'source') {
      this.workspace.openEphemeralDirectLink();
    }
    this.loadDoors();
    this.resolveSource(source, sourceKey, capturedConfigurationRevision);
  }

  private resolveSource(
    source: LinkingSourceDescriptor,
    sourceKey: string | null,
    capturedConfigurationRevision: number | null,
  ): void {
    this.cancelSourceLoad();
    this.workflowState.update((state) => ({
      ...state,
      sourceLoad: { status: 'loading', ayahs: [], progress: { loaded: 0, total: null }, errorMessage: null },
    }));
    try {
      this.sourceLoadSubscription = this.resolver.resolve(source, (progress) => {
        this.workflowState.update((state) => ({
          ...state,
          sourceLoad: { ...state.sourceLoad, progress, status: 'loading' },
        }));
      }).subscribe({
        next: (ayahs) => {
          const universe = ayahs.map((ayah) => ayah.verseKey);
          if (
            sourceKey !== null &&
            capturedConfigurationRevision !== null &&
            this.workspace.item(sourceKey)?.configurationRevision === capturedConfigurationRevision
          ) {
            this.workspace.reconcileResolvedSource(sourceKey, universe);
          }
          this.workflowState.update((state) => ({
            ...state,
            selection: reconcileLinkingSelection(state.selection, universe),
            sourceLoad: {
              status: 'success',
              ayahs,
              progress: { loaded: ayahs.length, total: ayahs.length },
              errorMessage: null,
            },
          }));
        },
        error: (error: unknown) => {
          const message = error instanceof Error ? error.message : 'تعذر تحميل نتائج المصدر كاملة.';
          this.workflowState.update((state) => ({
            ...state,
            sourceLoad: { status: 'error', ayahs: [], progress: state.sourceLoad.progress, errorMessage: message },
          }));
        },
      });
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'تعذر تحميل نتائج المصدر كاملة.';
      this.workflowState.update((state) => ({
        ...state,
        sourceLoad: { status: 'unsupported', ayahs: [], progress: { loaded: 0, total: null }, errorMessage: message },
      }));
    }
  }

  private resetAndClose(): void {
    this.reset();
    this.workspace.close();
  }

  private setSelection(selection: LinkingSelection): void {
    const state = this.workflowState();
    const universe = this.ayahVerseKeys();
    const reconciled = reconcileLinkingSelection(selection, universe);
    this.workflowState.update((current) => ({ ...current, selection: reconciled }));
    if (state.sourceKey !== null) {
      this.workspace.updateSelection(state.sourceKey, reconciled, universe);
    }
  }

  private ayahVerseKeys(): readonly string[] {
    return this.allAyahs().map((ayah) => ayah.verseKey);
  }

  private reset(): void {
    this.pendingSourceStart = null;
    this.pendingOrigin = null;
    this.cancelSourceLoad();
    this.workflowState.set(INITIAL_WORKFLOW);
  }

  private cancelSourceLoad(): void {
    this.sourceLoadSubscription?.unsubscribe();
    this.sourceLoadSubscription = null;
  }
}
