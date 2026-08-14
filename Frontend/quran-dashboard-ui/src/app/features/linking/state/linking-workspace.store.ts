import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { HttpLinkingWorkspaceRepository } from '../data-access/http-linking-workspace.repository';
import { LinkingWorkspaceRepository } from '../data-access/linking-workspace.repository';
import {
  LinkingManualLinkShape,
  LinkingManualWordIdsByVerseKey,
  isCanonicalQuranWordId,
} from '../models/linking-manual-mushaf.models';
import { LinkingOperationMember } from '../models/linking-operation.models';
import { LinkingOperationSourceDraft } from '../models/linking-operation-draft.models';
import { isVerseKey, LinkingSourceDescriptor } from '../models/linking-source.models';
import {
  LinkingRemovedWorkspaceItem,
  LinkingSelection,
  LinkingSourceConfiguration,
  LinkingWorkspaceItem,
  LinkingWorkspaceSnapshot,
  LinkingWorkspaceSurface,
} from '../models/linking-workspace.models';
import { linkingSourceKey } from '../utils/linking-source-key';
import {
  clearLinkingAyahs,
  reconcileLinkingSelection,
  selectAllLinkingAyahs,
  toggleLinkingSelection,
} from '../utils/linking-selection';
import { LinkingAccessService } from './linking-access.service';
import { LinkingOperationDraftStore } from './linking-operation-draft.store';
import { LinkingSourceCache } from './linking-source.cache';
import { LinkingSourcePagesFacade } from './linking-source-pages.facade';
import { mergeWorkspaceSnapshot, toAyahIds } from './linking-workspace-merge';
import { LinkingWorkspaceConfigurationSyncRunner } from './linking-workspace-configuration-sync.runner';
import {
  LinkingWorkspaceOperation,
  LinkingWorkspaceSyncRunner,
} from './linking-workspace-sync.runner';

@Injectable({ providedIn: 'root' })
export class LinkingWorkspaceStore {
  private readonly currentUserStore = inject(CurrentUserStore);
  private readonly linkingAccess = inject(LinkingAccessService);
  private readonly sync = inject(LinkingWorkspaceSyncRunner);
  private readonly configurationSync = inject(LinkingWorkspaceConfigurationSyncRunner);
  private readonly operationDraft = inject(LinkingOperationDraftStore);
  private readonly sourceCache = inject(LinkingSourceCache);
  private readonly sourcePages = inject(LinkingSourcePagesFacade);
  private readonly repository: LinkingWorkspaceRepository = inject(HttpLinkingWorkspaceRepository);
  private readonly itemsSignal = signal<readonly LinkingWorkspaceItem[]>([]);
  private readonly workspaceVersionSignal = signal<number | null>(null);
  private readonly checkedSourceKeysSignal = signal<readonly string[]>([]);
  private readonly activeSurfaceSignal = signal<LinkingWorkspaceSurface>('closed');
  private readonly editorSourceKeySignal = signal<string | null>(null);
  private readonly removedItemSignal = signal<LinkingRemovedWorkspaceItem | null>(null);
  private readonly clearAllRequestedSignal = signal(false);
  private readonly persistenceWarningSignal = signal<string | null>(null);
  private readonly currentActorSub = signal<string | null>(null);
  private readonly hydratedActorSub = signal<string | null>(null);
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
      warn: (message) => this.persistenceWarningSignal.set(message),
      invalidateLinkingDataRevision: () => this.invalidateLinkingDataRevision(),
    });
    this.configurationSync.connect({
      reload: async (sourceId) => {
        const snapshot = await firstValueFrom(this.repository.load());
        const current = this.itemsSignal().find((candidate) => candidate.sourceId === sourceId);
        const item = mergeWorkspaceSnapshot(snapshot, current === undefined ? [] : [current]).items.find(
          (candidate) => candidate.sourceId === sourceId,
        ) ?? null;
        return item === null || item.linkingDataRevision === null ? null : toOperationDraft(item);
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
    this.sourceCache.evictResolvedSources();
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
        ayahIdByVerseKey: {},
        configurationRevision: item.configurationRevision + 1,
        configuration:
          item.configuration.kind === 'manual'
            ? { ...item.configuration, quranWordIdsByVerseKey: {} }
            : item.configuration,
      })),
    );
  }

  addSource(source: LinkingSourceDescriptor): string | null {
    if (!this.canMutate()) {
      return null;
    }
    const sourceKey = linkingSourceKey(source);
    this.enqueue((version) => this.repository.addSource(source, version));
    return sourceKey;
  }

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
    return true;
  }

  returnToWorkspace(): void {
    if (!this.canMutate()) {
      return;
    }
    void this.leaveEditor('workspace');
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

  updateSelection(sourceKey: string, selection: LinkingSelection, universe: readonly string[]): void {
    this.updateConfiguration(sourceKey, (configuration) => ({
      ...configuration,
      ayahInclusion: reconcileLinkingSelection(selection, universe),
    }));
  }

  toggleSelection(sourceKey: string, verseKey: string, universe: readonly string[]): void {
    const item = this.findItem(sourceKey);
    if (item !== null) {
      this.updateSelection(
        sourceKey,
        toggleLinkingSelection(item.configuration.ayahInclusion, verseKey, universe),
        universe,
      );
    }
  }

  selectAll(sourceKey: string): void {
    this.updateConfiguration(sourceKey, (configuration) => ({
      ...configuration,
      ayahInclusion: selectAllLinkingAyahs(),
    }));
  }

  clearAll(sourceKey: string): void {
    this.updateConfiguration(sourceKey, (configuration) => ({
      ...configuration,
      ayahInclusion: clearLinkingAyahs(),
    }));
  }

  setAutomaticWordMatchesEnabled(sourceKey: string, enabled: boolean): void {
    this.updateConfiguration(sourceKey, (configuration) =>
      configuration.kind === 'automatic'
        ? { ...configuration, automaticWordMatchesEnabled: enabled }
        : configuration,
    );
  }

  setManualWordIds(sourceKey: string, quranWordIdsByVerseKey: LinkingManualWordIdsByVerseKey): void {
    const item = this.findItem(sourceKey);
    if (item?.source.kind !== 'manual-mushaf-ayahs') {
      return;
    }
    const normalized = normalizeManualWordIds(item.source, quranWordIdsByVerseKey);
    if (normalized === null) {
      return;
    }
    this.updateConfiguration(sourceKey, (configuration) =>
      configuration.kind === 'manual'
        ? { ...configuration, quranWordIdsByVerseKey: normalized }
        : configuration,
    );
  }

  setManualLinkShape(sourceKey: string, linkShape: LinkingManualLinkShape): void {
    this.updateConfiguration(sourceKey, (configuration) =>
      configuration.kind === 'manual' ? { ...configuration, linkShape } : configuration,
    );
  }

  reconcileResolvedSource(
    sourceKey: string,
    resolvedAyahs: readonly { ayahId: number; verseKey: string }[],
    linkingDataRevision: number,
  ): void {
    const item = this.findItem(sourceKey);
    if (item === null) {
      return;
    }
    const ayahIdByVerseKey = Object.fromEntries(
      resolvedAyahs.map((ayah) => [ayah.verseKey, ayah.ayahId]),
    );
    const universe = resolvedAyahs.map((ayah) => ayah.verseKey);
    const knownVerseKeys = new Map(resolvedAyahs.map((ayah) => [ayah.ayahId, ayah.verseKey]));
    const verseKeys = item.ayahOverrideIds
      .map((ayahId) => knownVerseKeys.get(ayahId))
      .filter((verseKey): verseKey is string => verseKey !== undefined);
    const ayahInclusion = reconcileLinkingSelection(
      { mode: item.configuration.ayahInclusion.mode, verseKeys },
      universe,
    );
    const reconciled: LinkingWorkspaceItem = {
      ...item,
      ayahIdByVerseKey,
      ayahOverrideIds: toAyahIds(ayahInclusion.verseKeys, ayahIdByVerseKey),
      configuration: { ...item.configuration, ayahInclusion },
      lastResolvedCount: universe.length,
      linkingDataRevision,
    };
    this.replaceItem(sourceKey, reconciled);
    if (reconciled.sourceVersion !== null) {
      this.configurationSync.resume();
      this.configurationSync.track(toOperationDraft(reconciled));
    }
  }

  captureOperationMembers(): readonly LinkingOperationMember[] {
    if (!this.canMutate()) {
      return [];
    }
    const checked = new Set(this.checkedSourceKeysSignal());
    return this.itemsSignal()
      .filter((item) => checked.has(item.sourceKey))
      .map((item) => ({
        sourceKey: item.sourceKey,
        source: item.source,
        configuration: item.configuration,
        origin: 'workspace' as const,
        configurationRevision: item.configurationRevision,
        operationOrder: this.itemsSignal().indexOf(item),
      }));
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
  }

  private updateConfiguration(
    sourceKey: string,
    update: (configuration: LinkingSourceConfiguration) => LinkingSourceConfiguration,
  ): void {
    this.updateItem(sourceKey, (item) => {
      const configuration = update(item.configuration);
      return configuration === item.configuration
        ? item
        : {
            ...item,
            configuration,
            ayahOverrideIds: toAyahIds(configuration.ayahInclusion.verseKeys, item.ayahIdByVerseKey),
          };
    });
  }

  private async leaveEditor(surface: 'closed' | 'workspace'): Promise<void> {
    const sourceKey = this.editorSourceKeySignal();
    if (sourceKey !== null) {
      try {
        await this.configurationSync.flush([sourceKey]);
      } catch {
        return;
      }
    }
    if (surface === 'workspace' && !this.canMutate()) {
      return;
    }
    this.editorSourceKeySignal.set(null);
    this.activeSurfaceSignal.set(surface);
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
      this.configurationSync.schedule(toOperationDraft(next));
    }
  }

  private enqueue(operation: LinkingWorkspaceOperation): void {
    const actorSub = this.currentActorSub();
    if (actorSub !== null) {
      this.sync.run(actorSub, this.actorGeneration, operation);
    }
  }

  private applySnapshot(snapshot: LinkingWorkspaceSnapshot): void {
    const merged = mergeWorkspaceSnapshot(snapshot, this.itemsSignal());
    this.itemsSignal.set(merged.items);
    for (const item of merged.items) {
      if (item.linkingDataRevision !== null && item.sourceVersion !== null) {
        this.configurationSync.track(toOperationDraft(item));
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

function toOperationDraft(item: LinkingWorkspaceItem): LinkingOperationSourceDraft {
  if (item.linkingDataRevision === null) {
    throw new Error('مراجعة بيانات الربط غير متاحة للمصدر.');
  }
  const selectedWordIdsByAyahId: Record<number, readonly number[]> = {};
  if (item.configuration.kind === 'manual') {
    for (const [verseKey, wordIds] of Object.entries(item.configuration.quranWordIdsByVerseKey)) {
      const ayahId = item.ayahIdByVerseKey[verseKey];
      if (ayahId !== undefined) {
        selectedWordIdsByAyahId[ayahId] = wordIds;
      }
    }
  }
  return {
    sourceKey: item.sourceKey,
    sourceId: item.sourceId,
    sourceVersion: item.sourceVersion,
    linkingDataRevision: item.linkingDataRevision,
    descriptor: item.source,
    label: item.source.label,
    selection: {
      mode: item.configuration.ayahInclusion.mode,
      ayahIds: item.ayahOverrideIds,
    },
    selectedWordIdsByAyahId,
    descriptions: [],
    automaticWordMatchesEnabled:
      item.configuration.kind === 'automatic'
        ? item.configuration.automaticWordMatchesEnabled
        : null,
    manualLinkShape:
      item.configuration.kind === 'manual' ? item.configuration.linkShape : null,
  };
}

function orderedSourceIdsWith(
  items: readonly LinkingWorkspaceItem[],
  removed: LinkingRemovedWorkspaceItem,
): readonly number[] | null {
  const restored = items.find((item) => item.sourceKey === removed.item.sourceKey);
  if (restored?.sourceId == null) {
    return null;
  }
  const others = items.filter((item) => item.sourceKey !== removed.item.sourceKey);
  if (others.some((item) => item.sourceId === null)) {
    return null;
  }
  const ordered = [...others];
  ordered.splice(Math.min(removed.index, ordered.length), 0, restored);
  return ordered.map((item) => item.sourceId!);
}

function normalizeManualWordIds(
  source: Extract<LinkingSourceDescriptor, { kind: 'manual-mushaf-ayahs' }>,
  quranWordIdsByVerseKey: LinkingManualWordIdsByVerseKey,
): LinkingManualWordIdsByVerseKey | null {
  const manualVerseKeys = new Set(source.manualAyahs.map((ayah) => ayah.verseKey));
  const normalized: Record<string, readonly number[]> = {};
  for (const [verseKey, quranWordIds] of Object.entries(quranWordIdsByVerseKey)) {
    if (!isVerseKey(verseKey) || !manualVerseKeys.has(verseKey) || !quranWordIds.every(isCanonicalQuranWordId)) {
      return null;
    }
    normalized[verseKey] = [...new Set(quranWordIds)].sort((left, right) => left - right);
  }
  return normalized;
}
