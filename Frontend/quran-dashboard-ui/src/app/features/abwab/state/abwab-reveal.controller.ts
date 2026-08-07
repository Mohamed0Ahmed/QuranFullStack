import { Injectable, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { ABWAB_LABELS } from '../models/abwab.labels';
import { AbwabNode, AbwabView } from '../models/abwab.models';
import { buildAbwabQueryParams } from './abwab-url-sync';
import { AbwabModalUrlController } from './abwab-modal-url.controller';
import { AbwabPageOverlaysController } from './abwab-page-overlays.controller';
import { AbwabSnapshotFacade } from './abwab-snapshot.facade';

const NO_IDS: ReadonlySet<number> = new Set<number>();
const REVEAL_HOLD_MS = 3000;

@Injectable()
export class AbwabRevealController {
  private readonly facade = inject(AbwabSnapshotFacade);
  private readonly overlays = inject(AbwabPageOverlaysController);
  private readonly modalUrl = inject(AbwabModalUrlController);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private readonly revealTargetId = signal<number | null>(null);
  private readonly revealSequence = signal(0);
  readonly revealedId = signal<number | null>(null);
  readonly announcement = signal<string | null>(null);

  private revealTimer: ReturnType<typeof setTimeout> | null = null;
  private revealPending = false;

  readonly expandSeedIds = computed<ReadonlySet<number>>(() => {
    this.revealSequence();
    const targetId = this.revealTargetId();
    const byId = this.facade.snapshot()?.byId;
    if (targetId === null || !byId) {
      return NO_IDS;
    }
    const chain = new Set<number>();
    let parentId = byId.get(targetId)?.parentId ?? null;
    while (parentId !== null && !chain.has(parentId)) {
      chain.add(parentId);
      parentId = byId.get(parentId)?.parentId ?? null;
    }
    return chain;
  });

  onRevealRequested(doorId: number, activeSectionId: number | null, view: AbwabView): void {
    const node = this.facade.snapshot()?.byId.get(doorId);
    if (!node || node.isArchived) {
      this.closeUnavailableRelations();
      this.announcement.set(ABWAB_LABELS.revealUnavailable);
      return;
    }
    this.overlays.closeRelationsModal();
    this.modalUrl.releaseTracking();
    this.announcement.set(null);
    this.revealTargetId.set(doorId);
    this.revealSequence.update((sequence) => sequence + 1);
    this.revealPending = true;
    const anchorId = this.overlays.relationsAnchorDoorId();
    this.updateQueryParams(
      buildAbwabQueryParams({
        door: doorId,
        modal: anchorId === null ? null : { kind: 'relations' as const, closed: true, subjectDoorId: anchorId },
        ...(activeSectionId !== null && node.sectionId !== activeSectionId ? { section: node.sectionId } : {}),
        ...(view === 'cards' ? { view: 'tree' as AbwabView } : {}),
      }),
    );
  }

  syncFromUrl(doorId: number | null, host: HTMLElement): void {
    this.announcement.set(null);
    const targetId = this.revealTargetId();
    if (!this.revealPending || targetId === null || doorId !== targetId) {
      return;
    }
    this.revealPending = false;
    this.startReveal(targetId, host);
  }

  destroy(): void {
    this.clearRevealTimer();
  }

  private closeUnavailableRelations(): void {
    const kind = this.modalUrl.urlBackedKind(['relations']);
    this.overlays.closeRelationsModal();
    if (kind === null) {
      return;
    }
    this.modalUrl.releaseTracking();
    this.updateQueryParams(buildAbwabQueryParams({ modal: null }), true);
  }

  private startReveal(doorId: number, host: HTMLElement): void {
    this.revealedId.set(doorId);
    this.clearRevealTimer();
    this.revealTimer = setTimeout(() => {
      this.revealedId.set(null);
      this.revealTargetId.set(null);
      this.revealTimer = null;
    }, REVEAL_HOLD_MS);
    queueMicrotask(() => {
      host.querySelector<HTMLElement>(`[data-testid="abwab-tree-row-${doorId}"]`)?.scrollIntoView?.({ block: 'nearest' });
    });
  }

  private clearRevealTimer(): void {
    if (this.revealTimer !== null) {
      clearTimeout(this.revealTimer);
      this.revealTimer = null;
    }
  }

  private updateQueryParams(changes: Record<string, string | null>, replaceUrl = false): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: changes,
      queryParamsHandling: 'merge',
      replaceUrl,
    });
  }
}
