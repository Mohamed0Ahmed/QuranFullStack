import { Injectable, OnDestroy, computed, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';

import { MushafAyahDoorsResponse } from '../../../core/api/generated/models';
import { AbwabNode } from '../../abwab/models/abwab.models';
import { AbwabSnapshotFacade } from '../../abwab/state/abwab-snapshot.facade';
import { MushafAyahDoorsApi } from '../data-access/mushaf-ayah-doors.api';
import { ResourceLoadState } from '../models/mushaf.models';
import { subscribeToApiLoad } from './mushaf-api-load.helpers';
import type { QuranVerseKey } from '../../../shared/quran/quran-location';

const EMPTY_LOAD_STATE: ResourceLoadState = {
  isLoading: false,
  isEmpty: false,
  errorMessage: null,
};

@Injectable({ providedIn: 'root' })
export class MushafAyahDoorsStore implements OnDestroy {
  private readonly api = inject(MushafAyahDoorsApi);
  private readonly tree = inject(AbwabSnapshotFacade);
  private readonly verseKeyState = signal<QuranVerseKey | null>(null);
  private readonly responseState = signal<MushafAyahDoorsResponse | null>(null);
  private readonly loadStateValue = signal<ResourceLoadState>(EMPTY_LOAD_STATE);
  private requestSubscription: Subscription | null = null;
  private requestToken = 0;

  readonly verseKey = this.verseKeyState.asReadonly();
  readonly loadState = this.loadStateValue.asReadonly();
  readonly relatedDoorIds = computed<ReadonlySet<number>>(
    () => new Set(this.currentResponse()?.doorIds ?? []),
  );
  readonly doors = computed<readonly AbwabNode[]>(() => {
    const snapshot = this.tree.snapshot();
    const response = this.currentResponse();
    if (!snapshot || !response) {
      return [];
    }

    return collectRelatedDoors(snapshot.liveRoots, new Set(response.doorIds));
  });

  load(verseKey: QuranVerseKey | null): void {
    if (verseKey === null) {
      this.clear();
      return;
    }

    if (
      verseKey === this.verseKeyState()
      && (this.responseState() !== null || this.loadStateValue().isLoading)
    ) {
      return;
    }

    this.cancelRequest();
    this.tree.ensureLoaded();
    this.verseKeyState.set(verseKey);
    this.responseState.set(null);
    this.fetch(verseKey);
  }

  retry(): void {
    const verseKey = this.verseKeyState();
    if (verseKey === null) {
      return;
    }

    this.cancelRequest();
    this.responseState.set(null);
    this.fetch(verseKey);
  }

  ngOnDestroy(): void {
    this.cancelRequest();
  }

  private fetch(verseKey: QuranVerseKey): void {
    const token = ++this.requestToken;
    this.loadStateValue.set({ isLoading: true, isEmpty: false, errorMessage: null });
    this.requestSubscription = subscribeToApiLoad(
      this.api.getDoors(verseKey),
      {
        onSuccess: (response) => {
          if (token === this.requestToken && response.verseKey === verseKey) {
            this.responseState.set(response);
          }
        },
        onSettled: (loadState) => {
          if (token === this.requestToken) {
            this.requestSubscription = null;
            this.loadStateValue.set(loadState);
          }
        },
        emptyMessage: 'تعذّر تحميل أبواب الآية.',
        notFoundMessage: 'تعذّر العثور على الآية المحددة.',
        connectionMessage: 'تعذّر الاتصال بالخادم لتحميل أبواب الآية.',
      },
    );
  }

  private currentResponse(): MushafAyahDoorsResponse | null {
    const response = this.responseState();
    return response?.verseKey === this.verseKeyState() ? response : null;
  }

  private clear(): void {
    this.cancelRequest();
    this.verseKeyState.set(null);
    this.responseState.set(null);
    this.loadStateValue.set(EMPTY_LOAD_STATE);
  }

  private cancelRequest(): void {
    this.requestToken++;
    this.requestSubscription?.unsubscribe();
    this.requestSubscription = null;
  }
}

function collectRelatedDoors(
  roots: readonly AbwabNode[],
  relatedDoorIds: ReadonlySet<number>,
): readonly AbwabNode[] {
  const doors: AbwabNode[] = [];
  const visit = (door: AbwabNode): void => {
    if (relatedDoorIds.has(door.id)) {
      doors.push(door);
    }
    door.children.forEach(visit);
  };
  roots.forEach(visit);
  return doors;
}
