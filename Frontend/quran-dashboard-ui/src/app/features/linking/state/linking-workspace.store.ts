import { Injectable, computed, effect, inject, signal } from '@angular/core';

import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { LocalStorageLinkingWorkspaceRepository } from '../data-access/local-storage-linking-workspace.repository';
import { toWorkspaceItem } from '../data-access/linking-workspace.codec';
import { LinkingWorkspaceRepository } from '../data-access/linking-workspace.repository';
import {
  LinkingManualLinkShape,
  LinkingManualWordLocationsByVerseKey,
  isManualWordLocation,
} from '../models/linking-manual-mushaf.models';
import { LinkingOperationMember } from '../models/linking-operation.models';
import { isVerseKey, LinkingSourceDescriptor } from '../models/linking-source.models';
import {
  LinkingRemovedWorkspaceItem,
  LinkingSelection,
  LinkingSourceConfiguration,
  LinkingWorkspaceItem,
  LinkingWorkspaceSurface,
} from '../models/linking-workspace.models';
import { linkingSourceKey } from '../utils/linking-source-key';
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

@Injectable({ providedIn: 'root' })
export class LinkingWorkspaceStore {
  private readonly currentUserStore = inject(CurrentUserStore);
  private readonly linkingAccess = inject(LinkingAccessService);
  private readonly repository: LinkingWorkspaceRepository = inject(LocalStorageLinkingWorkspaceRepository);
  private readonly itemsSignal = signal<readonly LinkingWorkspaceItem[]>([]);
  private readonly checkedSourceKeysSignal = signal<readonly string[]>([]);
  private readonly activeSurfaceSignal = signal<LinkingWorkspaceSurface>('closed');
  private readonly editorSourceKeySignal = signal<string | null>(null);
  private readonly directLinkSourceKeySignal = signal<string | null>(null);
  private readonly removedItemSignal = signal<LinkingRemovedWorkspaceItem | null>(null);
  private readonly clearAllRequestedSignal = signal(false);
  private readonly persistenceWarningSignal = signal<string | null>(null);
  private currentActorSub: string | null = null;
  private hydratedActorSub: string | null = null;
  private actorGeneration = 0;
  private durableWorkspaceRevision = 0;
  private saveQueue: Promise<void> = Promise.resolve();

  readonly items = computed(() =>
    this.isReadyForCurrentActor() ? this.itemsSignal() : [],
  );
  readonly itemCount = computed(() => this.items().length);
  readonly checkedSourceKeys = computed(() =>
    this.isReadyForCurrentActor() ? this.checkedSourceKeysSignal() : [],
  );
  readonly activeSurface = this.activeSurfaceSignal.asReadonly();
  readonly editorSourceKey = this.editorSourceKeySignal.asReadonly();
  readonly activeSourceKey = computed(() =>
    this.activeSurfaceSignal() === 'direct-link'
      ? this.directLinkSourceKeySignal()
      : this.editorSourceKeySignal(),
  );
  readonly removedItem = this.removedItemSignal.asReadonly();
  readonly clearAllRequested = this.clearAllRequestedSignal.asReadonly();
  readonly persistenceWarning = this.persistenceWarningSignal.asReadonly();
  readonly isOpen = computed(() =>
    this.isReadyForCurrentActor() && this.activeSurfaceSignal() !== 'closed',
  );

  constructor() {
    effect(() => this.synchronizeActorWorkspace());
  }

  addSource(source: LinkingSourceDescriptor): string | null {
    if (!this.canMutate()) {
      return null;
    }

    const sourceKey = linkingSourceKey(source);
    const existing = this.findItem(sourceKey);
    if (existing !== null) {
      this.replaceItem(
        sourceKey,
        toWorkspaceItem(
          sourceKey,
          source,
          existing.configuration,
          existing.lastResolvedCount,
          existing.lastResolvedCountIsStale,
          existing.configurationRevision,
        ),
      );
      return sourceKey;
    }

    this.itemsSignal.update((items) => [
      ...items,
      toWorkspaceItem(sourceKey, source, initialConfiguration(source), null, true),
    ]);
    this.persist();
    return sourceKey;
  }

  addOrFocus(source: LinkingSourceDescriptor): string | null {
    const sourceKey = this.addSource(source);
    if (sourceKey !== null) {
      this.editorSourceKeySignal.set(sourceKey);
    }
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
      this.activeSurfaceSignal.set('workspace');
    }
  }

  openManualWordEditor(sourceKey: string): void {
    const item = this.findItem(sourceKey);
    if (this.canMutate() && item?.configuration.kind === 'manual') {
      this.editorSourceKeySignal.set(sourceKey);
      this.activeSurfaceSignal.set('workspace');
    }
  }

  remove(sourceKey: string): void {
    const index = this.itemsSignal().findIndex((item) => item.sourceKey === sourceKey);
    if (!this.canMutate() || index < 0) {
      return;
    }

    const item = this.itemsSignal()[index];
    this.removedItemSignal.set({ item, index });
    this.itemsSignal.update((items) => items.filter((candidate) => candidate.sourceKey !== sourceKey));
    this.checkedSourceKeysSignal.update((keys) => keys.filter((key) => key !== sourceKey));
    if (this.editorSourceKeySignal() === sourceKey) {
      this.editorSourceKeySignal.set(null);
    }
    if (this.directLinkSourceKeySignal() === sourceKey) {
      this.directLinkSourceKeySignal.set(null);
      this.activeSurfaceSignal.set('workspace');
    }
    this.persist();
  }

  undoRemove(): void {
    const removed = this.removedItemSignal();
    if (!this.canMutate() || removed === null || this.findItem(removed.item.sourceKey) !== null) {
      return;
    }
    this.itemsSignal.update((items) => [
      ...items.slice(0, removed.index),
      removed.item,
      ...items.slice(removed.index),
    ]);
    this.removedItemSignal.set(null);
    this.persist();
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
    this.itemsSignal.set([]);
    this.checkedSourceKeysSignal.set([]);
    this.editorSourceKeySignal.set(null);
    this.directLinkSourceKeySignal.set(null);
    this.removedItemSignal.set(null);
    this.clearAllRequestedSignal.set(false);
    this.persist();
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
    this.editorSourceKeySignal.set(null);
    this.directLinkSourceKeySignal.set(null);
    this.activeSurfaceSignal.set('workspace');
  }

  close(): void {
    this.editorSourceKeySignal.set(null);
    this.directLinkSourceKeySignal.set(null);
    this.activeSurfaceSignal.set('closed');
  }

  openDirectLink(sourceKey: string): void {
    if (!this.canMutate() || this.findItem(sourceKey) === null) {
      return;
    }
    this.directLinkSourceKeySignal.set(sourceKey);
    this.activeSurfaceSignal.set('direct-link');
  }

  openEphemeralDirectLink(): void {
    if (this.canMutate()) {
      this.directLinkSourceKeySignal.set(null);
      this.activeSurfaceSignal.set('direct-link');
    }
  }

  addAndOpenDirectLink(source: LinkingSourceDescriptor): void {
    const sourceKey = this.addSource(source);
    if (sourceKey !== null) {
      this.openDirectLink(sourceKey);
    }
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

  setManualWordLocations(
    sourceKey: string,
    wordLocationsByVerseKey: LinkingManualWordLocationsByVerseKey,
  ): void {
    const item = this.findItem(sourceKey);
    if (item?.source.kind !== 'manual-mushaf-ayahs') {
      return;
    }
    const normalized = normalizeManualWordLocations(item.source, wordLocationsByVerseKey);
    if (normalized === null) {
      return;
    }
    this.updateConfiguration(sourceKey, (configuration) =>
      configuration.kind === 'manual'
        ? { ...configuration, wordLocationsByVerseKey: normalized }
        : configuration,
    );
  }

  setManualLinkShape(sourceKey: string, linkShape: LinkingManualLinkShape): void {
    this.updateConfiguration(sourceKey, (configuration) =>
      configuration.kind === 'manual' ? { ...configuration, linkShape } : configuration,
    );
  }

  refreshResultCount(sourceKey: string, resultCount: number): void {
    if (!Number.isSafeInteger(resultCount) || resultCount < 0) {
      return;
    }
    this.updateItem(sourceKey, (item) =>
      toWorkspaceItem(
        item.sourceKey,
        item.source,
        item.configuration,
        resultCount,
        false,
        item.configurationRevision,
      ),
    );
  }

  reconcileResolvedSource(sourceKey: string, universe: readonly string[]): void {
    const item = this.findItem(sourceKey);
    if (item === null) {
      return;
    }
    const configuration: LinkingSourceConfiguration = {
      ...item.configuration,
      ayahInclusion: reconcileLinkingSelection(item.configuration.ayahInclusion, universe),
    };
    this.updateItem(sourceKey, (current) =>
      toWorkspaceItem(
        current.sourceKey,
        current.source,
        configuration,
        universe.length,
        false,
        current.configurationRevision + 1,
      ),
    );
  }

  setHighlightSourceWords(sourceKey: string, highlightSourceWords: boolean): void {
    this.setAutomaticWordMatchesEnabled(sourceKey, highlightSourceWords);
  }

  selectedVerseKeys(sourceKey: string, universe: readonly string[]): readonly string[] {
    const item = this.findItem(sourceKey);
    return item ? selectedLinkingVerseKeys(item.configuration.ayahInclusion, universe) : [];
  }

  selectedCount(sourceKey: string, universe: readonly string[]): number {
    const item = this.findItem(sourceKey);
    return item ? selectedLinkingAyahCount(item.configuration.ayahInclusion, universe) : 0;
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
        origin: 'workspace',
        configurationRevision: item.configurationRevision,
      }));
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

    if (this.currentActorSub !== currentUser.sub) {
      this.activateActor(currentUser.sub);
    }
  }

  private activateActor(actorSub: string): void {
    this.actorGeneration += 1;
    this.currentActorSub = actorSub;
    this.hydratedActorSub = null;
    this.durableWorkspaceRevision = 0;
    this.resetWorkspaceSignals();
    const actorGeneration = this.actorGeneration;
    void this.hydrate(actorSub, actorGeneration);
  }

  private async hydrate(actorSub: string, actorGeneration: number): Promise<void> {
    try {
      const result = await this.repository.load(actorSub);
      if (!this.isCurrentActor(actorSub, actorGeneration)) {
        return;
      }
      if (result.invalidPayload) {
        void this.invalidateMalformedActor(actorSub, actorGeneration);
      }
      this.itemsSignal.set(result.items);
      this.checkedSourceKeysSignal.set([]);
      this.hydratedActorSub = actorSub;
    } catch {
      if (this.isCurrentActor(actorSub, actorGeneration)) {
        this.hydratedActorSub = actorSub;
        this.persistenceWarningSignal.set('تعذر استعادة مساحة الربط المحفوظة محلياً.');
      }
    }
  }

  private async invalidateMalformedActor(actorSub: string, actorGeneration: number): Promise<void> {
    try {
      await this.repository.invalidateActiveActor(actorSub);
    } catch {
      if (this.isCurrentActor(actorSub, actorGeneration)) {
        this.persistenceWarningSignal.set('تعذر حذف بيانات الربط المحلية غير الصالحة.');
      }
    }
  }

  private resetInMemoryWorkspace(): void {
    if (this.currentActorSub === null && this.itemsSignal().length === 0) {
      return;
    }
    this.actorGeneration += 1;
    this.currentActorSub = null;
    this.hydratedActorSub = null;
    this.durableWorkspaceRevision = 0;
    this.resetWorkspaceSignals();
  }

  private resetWorkspaceSignals(): void {
    this.itemsSignal.set([]);
    this.checkedSourceKeysSignal.set([]);
    this.activeSurfaceSignal.set('closed');
    this.editorSourceKeySignal.set(null);
    this.directLinkSourceKeySignal.set(null);
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
      if (configuration === item.configuration) {
        return item;
      }
      return toWorkspaceItem(
        item.sourceKey,
        item.source,
        configuration,
        item.lastResolvedCount,
        item.lastResolvedCountIsStale,
        item.configurationRevision + 1,
      );
    });
  }

  private updateItem(
    sourceKey: string,
    update: (item: LinkingWorkspaceItem) => LinkingWorkspaceItem,
  ): void {
    if (!this.canMutate() || this.findItem(sourceKey) === null) {
      return;
    }
    const current = this.findItem(sourceKey);
    if (current === null) {
      return;
    }
    const next = update(current);
    if (next === current) {
      return;
    }
    this.replaceItem(sourceKey, next);
  }

  private replaceItem(sourceKey: string, item: LinkingWorkspaceItem): void {
    this.itemsSignal.update((items) =>
      items.map((candidate) => (candidate.sourceKey === sourceKey ? item : candidate)),
    );
    this.persist();
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
      actorSub === this.currentActorSub &&
      actorSub === this.hydratedActorSub
    );
  }

  private persist(): void {
    const actorSub = this.currentActorSub;
    if (actorSub === null || !this.canMutate()) {
      return;
    }
    const actorGeneration = this.actorGeneration;
    const revision = ++this.durableWorkspaceRevision;
    const items = this.itemsSignal();
    this.saveQueue = this.saveQueue
      .catch(() => undefined)
      .then(async () => {
        if (!this.isCurrentActor(actorSub, actorGeneration)) {
          return;
        }
        try {
          await this.repository.save(actorSub, revision, items);
        } catch {
          if (this.isCurrentActor(actorSub, actorGeneration)) {
            this.persistenceWarningSignal.set('تعذر حفظ مساحة الربط محلياً.');
          }
        }
      });
  }

  private isCurrentActor(actorSub: string, actorGeneration: number): boolean {
    return (
      this.actorGeneration === actorGeneration &&
      this.currentActorSub === actorSub &&
      this.linkingAccess.canUseLinking() &&
      this.currentUserStore.currentUser()?.sub === actorSub
    );
  }

  private isReadyForCurrentActor(): boolean {
    const actorSub = this.currentUserStore.currentUser()?.sub;
    return (
      this.linkingAccess.canUseLinking() &&
      actorSub !== undefined &&
      actorSub === this.currentActorSub &&
      actorSub === this.hydratedActorSub
    );
  }
}

function initialConfiguration(source: LinkingSourceDescriptor): LinkingSourceConfiguration {
  if (source.kind === 'manual-mushaf-ayahs') {
    return {
      kind: 'manual',
      ayahInclusion: DEFAULT_LINKING_SELECTION,
      wordLocationsByVerseKey: {},
      linkShape: 'independent',
    };
  }
  return {
    kind: 'automatic',
    ayahInclusion: DEFAULT_LINKING_SELECTION,
    automaticWordMatchesEnabled: true,
  };
}

function normalizeManualWordLocations(
  source: Extract<LinkingSourceDescriptor, { kind: 'manual-mushaf-ayahs' }>,
  wordLocationsByVerseKey: LinkingManualWordLocationsByVerseKey,
): LinkingManualWordLocationsByVerseKey | null {
  const manualVerseKeys = new Set(source.manualAyahs.map((ayah) => ayah.verseKey));
  const normalized: Record<string, readonly string[]> = {};
  for (const [verseKey, locations] of Object.entries(wordLocationsByVerseKey)) {
    if (!isVerseKey(verseKey) || !manualVerseKeys.has(verseKey) || !locations.every(isManualWordLocation)) {
      return null;
    }
    normalized[verseKey] = [...new Set(locations)];
  }
  return normalized;
}
