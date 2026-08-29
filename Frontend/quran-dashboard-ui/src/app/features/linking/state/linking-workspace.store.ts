import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { HttpLinkingWorkspaceRepository } from '../data-access/http-linking-workspace.repository';
import { LinkingWorkspaceRepository } from '../data-access/linking-workspace.repository';
import { LinkingManualLinkShape } from '../models/linking-manual-mushaf.models';
import { LinkingSourceLaunchInput } from '../models/linking-source-launch.models';
import {
  LinkingRemovedWorkspaceItem,
  LinkingWorkspaceItem,
  LinkingWorkspaceSnapshot,
  LinkingWorkspaceSurface,
} from '../models/linking-workspace.models';
import { LinkingAccessService } from './linking-access.service';
import { LinkingOperationDraftStore } from './linking-operation-draft.store';
import { LinkingRecoveryStore } from './linking-recovery.store';
import { LinkingSourcePagesFacade } from './linking-source-pages.facade';
import { mergeWorkspaceSnapshot } from './linking-workspace-merge';
import { toLinkingOperationDraft } from './linking-workspace-item-draft';
import { LinkingWorkspaceConfigurationSyncRunner } from './linking-workspace-configuration-sync.runner';
import {
  LinkingWorkspaceOperation,
  LinkingWorkspaceSyncRunner,
} from './linking-workspace-sync.runner';
import { LinkingWorkspaceSourceTypesUpdater } from './linking-workspace-source-types.updater';
import { LinkingWorkspaceSourceAdder } from './linking-workspace-source-adder';
import { LinkingWorkspaceAddResult } from '../models/linking-workspace-add.models';

@Injectable({ providedIn: 'root' })
export class LinkingWorkspaceStore {
  private readonly currentUserStore = inject(CurrentUserStore);
  private readonly linkingAccess = inject(LinkingAccessService);
  private readonly sync = inject(LinkingWorkspaceSyncRunner);
  private readonly configurationSync = inject(LinkingWorkspaceConfigurationSyncRunner);
  private readonly operationDraft = inject(LinkingOperationDraftStore);
  private readonly recovery = inject(LinkingRecoveryStore);
  private readonly sourcePages = inject(LinkingSourcePagesFacade);
  private readonly sourceTypes = inject(LinkingWorkspaceSourceTypesUpdater);
  private readonly sourceAdder = inject(LinkingWorkspaceSourceAdder);
  private readonly repository: LinkingWorkspaceRepository = inject(HttpLinkingWorkspaceRepository);
  private readonly itemsSignal = signal<readonly LinkingWorkspaceItem[]>([]);
  private readonly workspaceVersionSignal = signal<number | null>(null);
  private readonly checkedSourceKeysSignal = signal<readonly string[]>([]);
  private readonly activeSurfaceSignal = signal<LinkingWorkspaceSurface>('closed');
  private readonly editorSourceKeySignal = signal<string | null>(null);
  private readonly removedItemSignal = signal<LinkingRemovedWorkspaceItem | null>(null);
  private readonly clearAllRequestedSignal = signal(false);
  private readonly persistenceWarningSignal = signal<string | null>(null);
  private readonly editorExitPendingSignal = signal(false);
  private readonly currentActorSub = signal<string | null>(null);
  private readonly hydratedActorSub = signal<string | null>(null);
  private editorExitTask: Promise<boolean> | null = null;
  private actorGeneration = 0;

  readonly items = computed(() => (this.isReadyForCurrentActor() ? this.itemsSignal() : []));
  readonly itemCount = computed(() => this.items().length);
  readonly checkedSourceKeys = computed(() =>
    this.isReadyForCurrentActor() ? this.checkedSourceKeysSignal() : [],
  );
  readonly activeSurface = this.activeSurfaceSignal.asReadonly();
  readonly editorSourceKey = this.editorSourceKeySignal.asReadonly();
  readonly removedItem = this.removedItemSignal.asReadonly();
  readonly clearAllRequested = this.clearAllRequestedSignal.asReadonly();
  readonly persistenceWarning = this.persistenceWarningSignal.asReadonly();
  readonly editorExitPending = this.editorExitPendingSignal.asReadonly();
  readonly isOpen = computed(() => {
    const activeSurface = this.activeSurfaceSignal();
    if (activeSurface === 'closed') {
      return false;
    }
    return activeSurface === 'linking-flow'
      ? this.linkingAccess.canUseLinking()
      : this.isReadyForCurrentActor();
  });

  constructor() {
    this.sourceAdder.connect(
      () => this.canMutate(),
      (sourceKey) => this.findItem(sourceKey) !== null,
      (launch) => this.enqueue((version) => this.repository.addSource(launch, version)),
    );
    this.sync.connect({
      isCurrentActor: (actorSub, actorGeneration) => this.isCurrentActor(actorSub, actorGeneration),
      workspaceVersion: () => this.workspaceVersionSignal(),
      items: () => this.itemsSignal(),
      findItem: (sourceKey) => this.findItem(sourceKey),
      applySnapshot: (snapshot) => this.applySnapshot(snapshot),
      restoreChecked: (sourceKey) =>
        this.checkedSourceKeysSignal.update((keys) =>
          keys.includes(sourceKey) ? keys : [...keys, sourceKey],
        ),
      restoreConfiguration: (removed) => this.restoreConfiguration(removed),
      warn: (message) => this.persistenceWarningSignal.set(message),
      invalidateLinkingDataRevision: () => this.invalidateLinkingDataRevision(),
      remapSourceKey: (sourceId, previousSourceKey, wasChecked) =>
        this.sourceTypes.remap(sourceId, previousSourceKey, wasChecked),
      completeSourceTypeUpdate: (sourceId) => this.sourceTypes.complete(sourceId),
    });
    this.sourceTypes.connect({
      canMutate: () => this.canMutate(),
      findItem: (sourceKey) => this.findItem(sourceKey),
      actor: () => {
        const sub = this.currentActorSub();
        return sub === null ? null : { sub, generation: this.actorGeneration };
      },
      isChecked: (sourceKey) => this.checkedSourceKeysSignal().includes(sourceKey),
      items: this.itemsSignal,
      checkedSourceKeys: this.checkedSourceKeysSignal,
      editorSourceKey: this.editorSourceKeySignal,
      cancelPage: (scope) => this.sourcePages.cancel(scope),
      warn: (message) => this.persistenceWarningSignal.set(message),
    });
    this.configurationSync.connect({
      reload: async (sourceId) => {
        const snapshot = await firstValueFrom(this.repository.load());
        const current = this.itemsSignal().find((candidate) => candidate.sourceId === sourceId);
        const item = mergeWorkspaceSnapshot(snapshot, current === undefined ? [] : [current]).items.find(
          (candidate) => candidate.sourceId === sourceId,
        ) ?? null;
        return item === null || item.linkingDataRevision === null
          ? null
          : toLinkingOperationDraft(item);
      },
      acknowledge: (sourceKey, response) => {
        const item = this.findItem(sourceKey);
        if (item !== null) {
          this.replaceItem(sourceKey, { ...item, sourceVersion: response.sourceVersion });
          this.workspaceVersionSignal.set(response.workspaceVersion);
        }
      },
      linkingDataStale: () => this.invalidateLinkingDataRevision(),
      conflict: (_sourceKey, message) => this.persistenceWarningSignal.set(message),
    });
    effect(() => this.synchronizeActorWorkspace());
  }

  dismissPersistenceWarning(): void {
    this.persistenceWarningSignal.set(null);
  }

  invalidateLinkingDataRevision(): void {
    for (const item of this.itemsSignal()) {
      if (item.linkingDataRevision !== null) {
        this.sourcePages.evictRevision(item.linkingDataRevision);
      }
      this.sourcePages.cancel(`manual-word-editor:${item.sourceKey}`);
    }
    this.operationDraft.requireFreshGeneration();
    this.itemsSignal.update((items) =>
      items.map((item) => ({
        ...item,
        linkingDataRevision: null,
        ayahOverrideIds: [],
        selectedWordIdsByAyahId: {},
        ayahIdByVerseKey: {},
        configurationRevision: item.configurationRevision + 1,
        configuration:
          item.configuration.kind === 'manual'
            ? { ...item.configuration, quranWordIdsByVerseKey: {} }
            : item.configuration,
      })),
    );
  }

  addSource(source: LinkingSourceLaunchInput): LinkingWorkspaceAddResult | null { return this.sourceAdder.add(source); }

  checkSource(sourceKey: string): void {
    if (!this.canMutate() || this.findItem(sourceKey) === null) {
      return;
    }
    this.checkedSourceKeysSignal.update((keys) => (keys.includes(sourceKey) ? keys : [...keys, sourceKey]));
  }

  uncheckSource(sourceKey: string): void {
    if (this.canMutate()) {
      this.checkedSourceKeysSignal.update((keys) => keys.filter((key) => key !== sourceKey));
    }
  }

  clearCheckedSources(): void {
    if (this.canMutate()) {
      this.checkedSourceKeysSignal.set([]);
    }
  }

  openAyahEditor(sourceKey: string): void {
    if (this.canMutate() && this.findItem(sourceKey) !== null) {
      this.editorSourceKeySignal.set(sourceKey);
      this.activeSurfaceSignal.set('source-ayah-editor');
    }
  }

  remove(sourceKey: string): void {
    const index = this.itemsSignal().findIndex((item) => item.sourceKey === sourceKey);
    const item = this.findItem(sourceKey);
    if (!this.canMutate() || index < 0 || item === null || item.sourceId === null) {
      return;
    }
    const sourceId = item.sourceId;
    this.configurationSync.remove(sourceKey);
    this.removedItemSignal.set({ item, index, wasChecked: this.checkedSourceKeysSignal().includes(sourceKey) });
    this.checkedSourceKeysSignal.update((keys) => keys.filter((key) => key !== sourceKey));
    if (this.editorSourceKeySignal() === sourceKey) {
      this.editorSourceKeySignal.set(null);
    }
    this.enqueue((version) => this.repository.removeSource(sourceId, version));
  }

  undoRemove(): void {
    const removed = this.removedItemSignal();
    if (!this.canMutate() || removed === null || this.findItem(removed.item.sourceKey) !== null) {
      return;
    }
    this.removedItemSignal.set(null);
    const actorSub = this.currentActorSub();
    if (actorSub !== null) {
      this.sync.restore(actorSub, this.actorGeneration, removed);
    }
  }

  requestClearAll(): void {
    if (this.canMutate() && this.itemsSignal().length > 0) {
      this.clearAllRequestedSignal.set(true);
    }
  }

  confirmClearAll(): void {
    if (!this.canMutate() || !this.clearAllRequestedSignal()) {
      return;
    }
    this.checkedSourceKeysSignal.set([]);
    this.editorSourceKeySignal.set(null);
    this.removedItemSignal.set(null);
    this.clearAllRequestedSignal.set(false);
    this.enqueue((version) => this.repository.clearSources(version));
  }

  cancelClearAll(): void {
    if (this.canMutate()) {
      this.clearAllRequestedSignal.set(false);
    }
  }

  openWorkspace(): void {
    if (!this.canMutate()) {
      return;
    }
    const actorSub = this.currentActorSub();
    if (actorSub !== null) {
      this.recovery.recover(actorSub);
    }
    void this.leaveEditor('workspace');
  }

  close(): void {
    void this.leaveEditor('closed');
  }

  openOperationFlow(): boolean {
    if (!this.linkingAccess.canUseLinking()) {
      return false;
    }
    this.editorSourceKeySignal.set(null);
    this.activeSurfaceSignal.set('linking-flow');
    const actorSub = this.currentActorSub();
    if (actorSub !== null) {
      this.recovery.recover(actorSub);
    }
    return true;
  }

  returnToWorkspace(): Promise<boolean> {
    if (!this.canMutate()) {
      return Promise.resolve(false);
    }
    return this.leaveEditor('workspace');
  }

  returnToSourceAyahEditor(): void {
    if (!this.canMutate()) {
      return;
    }
    const sourceKey = this.editorSourceKeySignal();
    if (sourceKey === null || this.findItem(sourceKey) === null) {
      this.editorSourceKeySignal.set(null);
      this.activeSurfaceSignal.set('workspace');
      return;
    }
    this.activeSurfaceSignal.set('source-ayah-editor');
  }

  setAutomaticWordMatchesEnabled(sourceKey: string, enabled: boolean): void {
    this.updateItem(sourceKey, (item) =>
      item.configuration.kind === 'automatic'
        ? {
            ...item,
            configuration: { ...item.configuration, automaticWordMatchesEnabled: enabled },
          }
        : item,
    );
  }

  setSourceTypeCodes(sourceKey: string, typeCodes: readonly string[]): void {
    this.sourceTypes.set(sourceKey, typeCodes);
  }

  isSourceTypeUpdatePending(sourceId: number | null): boolean {
    return this.sourceTypes.isPending(sourceId);
  }

  setManualLinkShape(sourceKey: string, linkShape: LinkingManualLinkShape): void {
    this.updateItem(sourceKey, (item) =>
      item.configuration.kind === 'manual'
        ? { ...item, configuration: { ...item.configuration, linkShape } }
        : item,
    );
  }

  toggleAyahId(sourceKey: string, ayahId: number): void {
    this.updateItem(sourceKey, (item) => {
      const overrides = new Set(item.ayahOverrideIds);
      overrides.has(ayahId) ? overrides.delete(ayahId) : overrides.add(ayahId);
      return { ...item, ayahOverrideIds: [...overrides].sort((left, right) => left - right) };
    });
  }

  selectAllAyahIds(sourceKey: string): void {
    this.updateItem(sourceKey, (item) => ({
      ...item,
      ayahOverrideIds: [],
      configuration: {
        ...item.configuration,
        ayahInclusion: { mode: 'all-except', verseKeys: [] },
      },
    }));
  }

  clearAllAyahIds(sourceKey: string): void {
    this.updateItem(sourceKey, (item) => ({
      ...item,
      ayahOverrideIds: [],
      configuration: {
        ...item.configuration,
        ayahInclusion: { mode: 'only', verseKeys: [] },
      },
    }));
  }

  toggleManualWordId(sourceKey: string, ayahId: number, quranWordId: number): void {
    this.updateItem(sourceKey, (item) => {
      if (item.configuration.kind !== 'manual') {
        return item;
      }
      const selected = new Set(item.selectedWordIdsByAyahId[ayahId] ?? []);
      selected.has(quranWordId) ? selected.delete(quranWordId) : selected.add(quranWordId);
      return {
        ...item,
        selectedWordIdsByAyahId: {
          ...item.selectedWordIdsByAyahId,
          [ayahId]: [...selected].sort((left, right) => left - right),
        },
      };
    });
  }

  setManualWordIdsByAyahId(
    sourceKey: string,
    selectedWordIdsByAyahId: Readonly<Record<number, readonly number[]>>,
  ): void {
    this.updateItem(sourceKey, (item) =>
      item.configuration.kind === 'manual'
        ? { ...item, selectedWordIdsByAyahId }
        : item,
    );
  }

  reconcilePage(sourceKey: string, linkingDataRevision: number, totalAyahCount: number): void {
    const item = this.findItem(sourceKey);
    if (item === null) {
      return;
    }
    if (item.linkingDataRevision === linkingDataRevision) {
      if (item.lastResolvedCount !== totalAyahCount) {
        this.replaceItem(sourceKey, { ...item, lastResolvedCount: totalAyahCount });
      }
      return;
    }
    const reconciled = {
      ...item,
      linkingDataRevision,
      lastResolvedCount: totalAyahCount,
    };
    this.replaceItem(sourceKey, reconciled);
    if (reconciled.sourceVersion !== null) {
      this.configurationSync.resume();
      this.configurationSync.track(toLinkingOperationDraft(reconciled));
    }
  }

  flushSelectedSources(): Promise<void> {
    return this.configurationSync.flush(this.checkedSourceKeysSignal());
  }

  item(sourceKey: string): LinkingWorkspaceItem | null {
    return this.findItem(sourceKey);
  }

  private synchronizeActorWorkspace(): void {
    if (!this.currentUserStore.authStateKnown() || this.currentUserStore.loadState() === 'loading') {
      return;
    }
    const currentUser = this.currentUserStore.currentUser();
    if (!this.linkingAccess.canUseLinking() || currentUser === null) {
      this.resetInMemoryWorkspace();
      return;
    }
    if (this.currentActorSub() !== currentUser.sub) {
      this.activateActor(currentUser.sub);
    }
  }

  private activateActor(actorSub: string): void {
    this.actorGeneration += 1;
    this.currentActorSub.set(actorSub);
    this.hydratedActorSub.set(null);
    this.resetWorkspaceSignals();
    this.recovery.recover(actorSub);
    void this.hydrate(actorSub, this.actorGeneration);
  }

  private async hydrate(actorSub: string, actorGeneration: number): Promise<void> {
    await this.sync.hydrate(actorSub, actorGeneration);
    if (this.isCurrentActor(actorSub, actorGeneration)) {
      this.hydratedActorSub.set(actorSub);
    }
  }

  private resetInMemoryWorkspace(): void {
    if (this.currentActorSub() === null && this.itemsSignal().length === 0) {
      return;
    }
    this.actorGeneration += 1;
    this.currentActorSub.set(null);
    this.hydratedActorSub.set(null);
    this.resetWorkspaceSignals();
  }

  private resetWorkspaceSignals(): void {
    this.itemsSignal().forEach((item) => this.configurationSync.remove(item.sourceKey));
    this.itemsSignal.set([]);
    this.workspaceVersionSignal.set(null);
    this.checkedSourceKeysSignal.set([]);
    this.activeSurfaceSignal.set('closed');
    this.editorSourceKeySignal.set(null);
    this.removedItemSignal.set(null);
    this.clearAllRequestedSignal.set(false);
    this.persistenceWarningSignal.set(null);
    this.sourceTypes.reset();
  }

  private leaveEditor(surface: 'closed' | 'workspace'): Promise<boolean> {
    if (this.editorExitTask !== null) {
      return this.editorExitTask;
    }
    this.editorExitPendingSignal.set(true);
    const task = this.performEditorExit(surface).finally(() => {
      if (this.editorExitTask === task) {
        this.editorExitTask = null;
        this.editorExitPendingSignal.set(false);
      }
    });
    this.editorExitTask = task;
    return task;
  }

  private async performEditorExit(surface: 'closed' | 'workspace'): Promise<boolean> {
    const sourceKey = this.editorSourceKeySignal();
    if (sourceKey !== null) {
      try {
        await this.configurationSync.flush([sourceKey]);
      } catch (error: unknown) {
        this.persistenceWarningSignal.set(
          error instanceof Error ? error.message : 'تعذر مزامنة إعدادات المصدر.',
        );
      }
    }
    if (surface === 'workspace' && !this.canMutate()) {
      return false;
    }
    this.editorSourceKeySignal.set(null);
    this.activeSurfaceSignal.set(surface);
    return true;
  }

  private async restoreConfiguration(removed: LinkingRemovedWorkspaceItem): Promise<void> {
    const restored = this.findItem(removed.item.sourceKey);
    const linkingDataRevision = removed.item.linkingDataRevision;
    if (restored?.sourceId == null || restored.sourceVersion === null || linkingDataRevision === null) {
      return;
    }
    const acknowledged = toLinkingOperationDraft({ ...restored, linkingDataRevision });
    const desiredItem: LinkingWorkspaceItem = {
      ...removed.item,
      sourceId: restored.sourceId,
      sourceVersion: restored.sourceVersion,
      linkingDataRevision,
    };
    await this.configurationSync.restore(acknowledged, toLinkingOperationDraft(desiredItem));
    const current = this.findItem(desiredItem.sourceKey);
    this.replaceItem(desiredItem.sourceKey, {
      ...desiredItem,
      sourceVersion: current?.sourceVersion ?? desiredItem.sourceVersion,
    });
  }

  private updateItem(
    sourceKey: string,
    update: (item: LinkingWorkspaceItem) => LinkingWorkspaceItem,
  ): void {
    const item = this.findItem(sourceKey);
    if (!this.canMutate() || item === null || item.sourceId === null) {
      return;
    }
    const updated = update(item);
    if (updated === item) {
      return;
    }
    const next: LinkingWorkspaceItem = {
      ...updated,
      configurationRevision: item.configurationRevision + 1,
    };
    this.replaceItem(sourceKey, next);
    if (next.linkingDataRevision !== null && next.sourceVersion !== null) {
      this.configurationSync.schedule(toLinkingOperationDraft(next));
    }
  }

  private enqueue(operation: LinkingWorkspaceOperation): Promise<void> {
    const actorSub = this.currentActorSub();
    return actorSub === null
      ? Promise.resolve()
      : this.sync.run(actorSub, this.actorGeneration, operation);
  }

  private applySnapshot(snapshot: LinkingWorkspaceSnapshot): void {
    const merged = mergeWorkspaceSnapshot(snapshot, this.itemsSignal());
    this.itemsSignal.set(merged.items);
    for (const item of merged.items) {
      if (item.linkingDataRevision !== null && item.sourceVersion !== null) {
        this.configurationSync.track(toLinkingOperationDraft(item));
      }
    }
    this.workspaceVersionSignal.set(merged.workspaceVersion);
    const known = new Set(merged.items.map((item) => item.sourceKey));
    this.checkedSourceKeysSignal.update((keys) => keys.filter((key) => known.has(key)));
  }

  private replaceItem(sourceKey: string, item: LinkingWorkspaceItem): void {
    this.itemsSignal.update((items) =>
      items.map((candidate) => (candidate.sourceKey === sourceKey ? item : candidate)),
    );
  }

  private findItem(sourceKey: string): LinkingWorkspaceItem | null {
    return this.itemsSignal().find((item) => item.sourceKey === sourceKey) ?? null;
  }

  private canMutate(): boolean {
    this.synchronizeActorWorkspace();
    const actorSub = this.currentUserStore.currentUser()?.sub;
    return (
      this.linkingAccess.canUseLinking() &&
      actorSub !== undefined &&
      actorSub === this.currentActorSub() &&
      actorSub === this.hydratedActorSub()
    );
  }

  private isCurrentActor(actorSub: string, actorGeneration: number): boolean {
    return (
      this.actorGeneration === actorGeneration &&
      this.currentActorSub() === actorSub &&
      this.linkingAccess.canUseLinking() &&
      this.currentUserStore.currentUser()?.sub === actorSub
    );
  }

  private isReadyForCurrentActor(): boolean {
    const actorSub = this.currentUserStore.currentUser()?.sub;
    return (
      this.linkingAccess.canUseLinking() &&
      actorSub !== undefined &&
      actorSub === this.currentActorSub() &&
      actorSub === this.hydratedActorSub()
    );
  }
}
