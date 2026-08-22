import { Location } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { ABWAB_LABELS } from '../models/abwab.labels';
import { ABWAB_QUERY_KEYS, AbwabNode, AbwabView } from '../models/abwab.models';
import { buildAbwabQueryParams, currentAbwabSearchQuery } from './abwab-url-sync';
import { AbwabModalUrlController } from './abwab-modal-url.controller';
import { AbwabSnapshotFacade } from './abwab-snapshot.facade';

const NO_IDS: ReadonlySet<number> = new Set<number>();
const REVEAL_HOLD_MS = 3000;

@Injectable()
export class AbwabRevealController {
  private readonly facade = inject(AbwabSnapshotFacade);
  private readonly modalUrl = inject(AbwabModalUrlController);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly location = inject(Location);

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
      this.closeUnavailableOrigin();
      this.announcement.set(ABWAB_LABELS.revealUnavailable);
      return;
    }
    const returnModal = this.modalUrl.closeRevealOrigin();
    this.announcement.set(null);
    this.revealTargetId.set(doorId);
    this.revealSequence.update((sequence) => sequence + 1);
    this.revealPending = true;
    this.updateQueryParams(
      buildAbwabQueryParams({
        door: doorId,
        modal: returnModal,
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

  private closeUnavailableOrigin(): void {
    const origin = this.modalUrl.closeRevealOrigin();
    if (origin === null) {
      return;
    }
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
      queryParams: {
        [ABWAB_QUERY_KEYS.q]: currentAbwabSearchQuery(this.router, this.location),
        ...changes,
      },
      queryParamsHandling: 'merge',
      replaceUrl,
    });
  }
}
