import { Injectable, computed, effect, inject, signal } from '@angular/core';

import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { LinkingSourceDescriptor } from '../models/linking-source.models';
import {
  LinkingSelection,
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
import { LinkingWorkspaceSession } from './linking-workspace-session';

@Injectable({ providedIn: 'root' })
export class LinkingWorkspaceStore {
  private readonly currentUserStore = inject(CurrentUserStore);
  private readonly linkingAccess = inject(LinkingAccessService);
  private readonly session = inject(LinkingWorkspaceSession);
  private readonly itemsSignal = signal<readonly LinkingWorkspaceItem[]>([]);
  private readonly activeSurfaceSignal = signal<LinkingWorkspaceSurface>('closed');
  private readonly activeSourceKeySignal = signal<string | null>(null);
  private hydratedActorSub: string | null = null;
  private currentActorSub: string | null = null;

  readonly items = this.itemsSignal.asReadonly();
  readonly itemCount = computed(() => this.itemsSignal().length);
  readonly activeSurface = this.activeSurfaceSignal.asReadonly();
  readonly activeSourceKey = this.activeSourceKeySignal.asReadonly();
  readonly isOpen = computed(() => this.activeSurfaceSignal() !== 'closed');

  constructor() {
    effect(() => this.synchronizeActorSession());
  }

  addOrFocus(source: LinkingSourceDescriptor): string | null {
    if (!this.canMutate()) {
      return null;
    }

    const sourceKey = linkingSourceKey(source);
    if (!this.itemsSignal().some((item) => item.sourceKey === sourceKey)) {
      this.itemsSignal.update((items) => [
        ...items,
        {
          sourceKey,
          source,
          selection: DEFAULT_LINKING_SELECTION,
          resultCount: null,
          highlightSourceWords: true,
        },
      ]);
      this.persist();
    }

    this.activeSourceKeySignal.set(sourceKey);
    return sourceKey;
  }

  remove(sourceKey: string): void {
    if (!this.canMutate() || !this.itemsSignal().some((item) => item.sourceKey === sourceKey)) {
      return;
    }

    this.itemsSignal.update((items) => items.filter((item) => item.sourceKey !== sourceKey));
    if (this.activeSourceKeySignal() === sourceKey) {
      this.activeSourceKeySignal.set(null);
    }
    if (this.activeSurfaceSignal() === 'direct-link') {
      this.activeSurfaceSignal.set('workspace');
    }
    this.persist();
  }

  openWorkspace(): void {
    if (!this.canMutate()) {
      return;
    }

    this.activeSourceKeySignal.set(null);
    this.activeSurfaceSignal.set('workspace');
  }

  close(): void {
    this.activeSourceKeySignal.set(null);
    this.activeSurfaceSignal.set('closed');
  }

  openDirectLink(sourceKey: string): void {
    if (!this.canMutate() || !this.itemsSignal().some((item) => item.sourceKey === sourceKey)) {
      return;
    }

    this.activeSourceKeySignal.set(sourceKey);
    this.activeSurfaceSignal.set('direct-link');
  }

  addAndOpenDirectLink(source: LinkingSourceDescriptor): void {
    const sourceKey = this.addOrFocus(source);
    if (sourceKey) {
      this.openDirectLink(sourceKey);
    }
  }

  updateSelection(sourceKey: string, selection: LinkingSelection, universe: readonly string[]): void {
    this.updateItem(sourceKey, (item) => ({
      ...item,
      selection: reconcileLinkingSelection(selection, universe),
    }));
  }

  toggleSelection(sourceKey: string, verseKey: string, universe: readonly string[]): void {
    this.updateItem(sourceKey, (item) => ({
      ...item,
      selection: toggleLinkingSelection(item.selection, verseKey, universe),
    }));
  }

  selectAll(sourceKey: string): void {
    this.updateItem(sourceKey, (item) => ({ ...item, selection: selectAllLinkingAyahs() }));
  }

  clearAll(sourceKey: string): void {
    this.updateItem(sourceKey, (item) => ({ ...item, selection: clearLinkingAyahs() }));
  }

  refreshResultCount(sourceKey: string, resultCount: number): void {
    if (!Number.isSafeInteger(resultCount) || resultCount < 0) {
      return;
    }

    this.updateItem(sourceKey, (item) => ({ ...item, resultCount }));
  }

  setHighlightSourceWords(sourceKey: string, highlightSourceWords: boolean): void {
    this.updateItem(sourceKey, (item) => ({ ...item, highlightSourceWords }));
  }

  selectedVerseKeys(sourceKey: string, universe: readonly string[]): readonly string[] {
    const item = this.findItem(sourceKey);
    return item ? selectedLinkingVerseKeys(item.selection, universe) : [];
  }

  selectedCount(sourceKey: string, universe: readonly string[]): number {
    const item = this.findItem(sourceKey);
    return item ? selectedLinkingAyahCount(item.selection, universe) : 0;
  }

  private synchronizeActorSession(): void {
    if (!this.currentUserStore.authStateKnown()) {
      return;
    }

    const currentUser = this.currentUserStore.currentUser();
    if (!this.currentUserStore.isAuthenticated() || !currentUser) {
      this.clearForActorChange();
      return;
    }

    this.synchronizeWorkspaceForActor(currentUser.sub);
  }

  private clearForActorChange(): void {
    this.session.clear();
    this.itemsSignal.set([]);
    this.activeSourceKeySignal.set(null);
    this.activeSurfaceSignal.set('closed');
    this.currentActorSub = null;
    this.hydratedActorSub = null;
  }

  private updateItem(
    sourceKey: string,
    update: (item: LinkingWorkspaceItem) => LinkingWorkspaceItem,
  ): void {
    if (!this.canMutate() || !this.itemsSignal().some((item) => item.sourceKey === sourceKey)) {
      return;
    }

    this.itemsSignal.update((items) =>
      items.map((item) => (item.sourceKey === sourceKey ? update(item) : item)),
    );
    this.persist();
  }

  private findItem(sourceKey: string): LinkingWorkspaceItem | null {
    return this.itemsSignal().find((item) => item.sourceKey === sourceKey) ?? null;
  }

  private canMutate(): boolean {
    if (!this.linkingAccess.canUseLinking()) {
      return false;
    }

    const actorSub = this.currentUserStore.currentUser()?.sub;
    if (!actorSub) {
      return false;
    }

    this.synchronizeWorkspaceForActor(actorSub);
    return true;
  }

  private persist(): void {
    const actorSub = this.currentUserStore.currentUser()?.sub;
    if (actorSub && this.canMutate()) {
      this.session.save(actorSub, this.itemsSignal());
    }
  }

  private synchronizeWorkspaceForActor(actorSub: string): void {
    if (this.currentActorSub && this.currentActorSub !== actorSub) {
      this.clearForActorChange();
    }

    this.currentActorSub = actorSub;
    if (!this.linkingAccess.canUseLinking() || this.hydratedActorSub === actorSub) {
      return;
    }

    this.itemsSignal.set(this.session.load(actorSub) ?? []);
    this.hydratedActorSub = actorSub;
  }
}
