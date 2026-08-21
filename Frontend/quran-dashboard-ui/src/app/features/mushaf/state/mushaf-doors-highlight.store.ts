import { Injectable, OnDestroy, computed, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';

import { MushafDoorHighlightsResponse } from '../../../core/api/generated/models';
import { AbwabSnapshotFacade } from '../../abwab/state/abwab-snapshot.facade';
import { MushafDoorHighlightsApi } from '../data-access/mushaf-door-highlights.api';
import {
  MUSHAF_DOOR_COLOR_SLOTS,
  MushafAppliedDoorViewModel,
  MushafDoorColorSlot,
  MushafDoorResolvedHighlight,
} from '../models/mushaf-door-highlights.models';
import { ResourceLoadState } from '../models/mushaf.models';
import { subscribeToApiLoad } from './mushaf-api-load.helpers';

const DEFAULT_DOOR_COLOR_SLOT: MushafDoorColorSlot = MUSHAF_DOOR_COLOR_SLOTS[0];
const EMPTY_LOAD_STATE: ResourceLoadState = {
  isLoading: false,
  isEmpty: false,
  errorMessage: null,
};

@Injectable()
export class MushafDoorsHighlightStore implements OnDestroy {
  private readonly api = inject(MushafDoorHighlightsApi);
  private readonly doors = inject(AbwabSnapshotFacade);

  private readonly pageNumberState = signal<number | null>(null);
  private readonly draftDoorIdsState = signal<ReadonlySet<number>>(new Set());
  private readonly appliedDoorIdsState = signal<ReadonlySet<number>>(new Set());
  private readonly doorColorSlotsState = signal<ReadonlyMap<number, MushafDoorColorSlot>>(new Map());
  private readonly responseState = signal<MushafDoorHighlightsResponse | null>(null);
  private readonly loadStateValue = signal<ResourceLoadState>(EMPTY_LOAD_STATE);
  private requestSubscription: Subscription | null = null;
  private requestToken = 0;

  readonly draftDoorIds = this.draftDoorIdsState.asReadonly();
  readonly appliedDoorIds = this.appliedDoorIdsState.asReadonly();
  readonly loadState = this.loadStateValue.asReadonly();
  readonly palette = MUSHAF_DOOR_COLOR_SLOTS;

  readonly appliedDoors = computed<readonly MushafAppliedDoorViewModel[]>(() => {
    const snapshot = this.doors.snapshot();
    const colorSlots = this.doorColorSlotsState();

    return Array.from(this.appliedDoorIdsState()).flatMap((id) => {
      const door = snapshot?.byId.get(id);
      return door
        ? [{ id, name: door.name, colorSlot: colorSlots.get(id) ?? DEFAULT_DOOR_COLOR_SLOT }]
        : [];
    });
  });

  readonly hasDraftChanges = computed(
    () => !setsEqual(this.draftDoorIdsState(), this.appliedDoorIdsState()),
  );

  readonly unavailableDoorCount = computed(
    () => this.responseState()?.unavailableDoorIds.length ?? 0,
  );

  readonly wordHighlights = computed<ReadonlyMap<string, MushafDoorResolvedHighlight>>(() => {
    const response = this.currentResponse();
    if (!response) {
      return new Map();
    }

    return new Map(
      response.words.flatMap((word) => {
        const highlight = this.resolveHighlight(word.doorIds);
        return highlight ? [[word.wordLocation, highlight] as const] : [];
      }),
    );
  });

  readonly ayahHighlights = computed<ReadonlyMap<string, MushafDoorResolvedHighlight>>(() => {
    const response = this.currentResponse();
    if (!response) {
      return new Map();
    }

    return new Map(
      response.ayahs.flatMap((ayah) => {
        const highlight = this.resolveHighlight(ayah.doorIds);
        return highlight ? [[ayah.verseKey, highlight] as const] : [];
      }),
    );
  });

  ngOnDestroy(): void {
    this.cancelRequest();
  }

  setPage(pageNumber: number | null): void {
    if (this.pageNumberState() === pageNumber) {
      return;
    }

    this.pageNumberState.set(pageNumber);
    this.responseState.set(null);
    this.cancelRequest();
    this.loadCurrentPage();
  }

  toggleDraftDoor(doorId: number): void {
    const next = new Set(this.draftDoorIdsState());
    if (next.has(doorId)) {
      next.delete(doorId);
    } else {
      next.add(doorId);
    }
    this.draftDoorIdsState.set(next);
  }

  confirmDraft(): void {
    const applied = new Set(this.draftDoorIdsState());
    this.appliedDoorIdsState.set(applied);
    this.pruneDoorColors(applied);
    this.responseState.set(null);
    this.cancelRequest();
    this.loadCurrentPage();
  }

  removeAppliedDoor(doorId: number): void {
    const applied = new Set(this.appliedDoorIdsState());
    const draft = new Set(this.draftDoorIdsState());
    applied.delete(doorId);
    draft.delete(doorId);
    this.appliedDoorIdsState.set(applied);
    this.draftDoorIdsState.set(draft);
    this.pruneDoorColors(applied);
    this.responseState.set(null);
    this.cancelRequest();
    this.loadCurrentPage();
  }

  setDoorColor(doorId: number, colorSlot: MushafDoorColorSlot): void {
    if (!this.appliedDoorIdsState().has(doorId)) {
      return;
    }

    const next = new Map(this.doorColorSlotsState());
    next.set(doorId, colorSlot);
    this.doorColorSlotsState.set(next);
  }

  retry(): void {
    this.cancelRequest();
    this.loadCurrentPage();
  }

  private loadCurrentPage(): void {
    const pageNumber = this.pageNumberState();
    const doorIds = Array.from(this.appliedDoorIdsState());
    if (pageNumber === null || doorIds.length === 0) {
      this.loadStateValue.set(EMPTY_LOAD_STATE);
      return;
    }

    const token = ++this.requestToken;
    this.loadStateValue.set({ isLoading: true, isEmpty: false, errorMessage: null });
    this.requestSubscription = subscribeToApiLoad(
      this.api.getPageHighlights(pageNumber, doorIds),
      {
        onSuccess: (data) => {
          if (token === this.requestToken && data.pageNumber === pageNumber) {
            this.responseState.set(data);
          }
        },
        onSettled: (loadState) => {
          if (token === this.requestToken) {
            this.requestSubscription = null;
            this.loadStateValue.set(loadState);
          }
        },
        emptyMessage: 'تعذّر تحميل تمييز الأبواب.',
        notFoundMessage: 'تعذّر العثور على تمييز الأبواب لهذه الصفحة.',
        connectionMessage: 'تعذّر الاتصال بالخادم لتحميل تمييز الأبواب.',
      },
    );
  }

  private currentResponse(): MushafDoorHighlightsResponse | null {
    const response = this.responseState();
    return response?.pageNumber === this.pageNumberState() ? response : null;
  }

  private resolveHighlight(doorIds: readonly number[]): MushafDoorResolvedHighlight | null {
    const applied = this.appliedDoorIdsState();
    const snapshot = this.doors.snapshot();
    const resolvedDoors = Array.from(new Set(doorIds)).flatMap((id) => {
      const door = applied.has(id) ? snapshot?.byId.get(id) : null;
      return door ? [{ id, name: door.name }] : [];
    });

    if (resolvedDoors.length === 0) {
      return null;
    }

    const colorSlot =
      resolvedDoors.length > 1
        ? 'multi'
        : (this.doorColorSlotsState().get(resolvedDoors[0].id) ?? DEFAULT_DOOR_COLOR_SLOT);

    return {
      doors: resolvedDoors,
      colorSlot,
    };
  }

  private pruneDoorColors(applied: ReadonlySet<number>): void {
    this.doorColorSlotsState.set(
      new Map(Array.from(this.doorColorSlotsState()).filter(([doorId]) => applied.has(doorId))),
    );
  }

  private cancelRequest(): void {
    this.requestToken += 1;
    this.requestSubscription?.unsubscribe();
    this.requestSubscription = null;
  }
}

function setsEqual(left: ReadonlySet<number>, right: ReadonlySet<number>): boolean {
  return left.size === right.size && Array.from(left).every((value) => right.has(value));
}
