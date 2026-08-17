import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';

interface AbwabManagementPickerSessionState {
  readonly expandedDoorIds: readonly number[];
  readonly anchorDoorId: number | null;
}

const STORAGE_KEY = 'quran-dashboard.abwab.management-picker-session.v1';
const INITIAL_STATE: AbwabManagementPickerSessionState = {
  expandedDoorIds: [],
  anchorDoorId: null,
};

@Injectable({ providedIn: 'root' })
export class AbwabManagementPickerSessionStore {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly state = signal<AbwabManagementPickerSessionState>(this.read());

  readonly expandedDoorIds = computed<ReadonlySet<number>>(
    () => new Set(this.state().expandedDoorIds),
  );
  readonly anchorDoorId = computed(() => this.state().anchorDoorId);

  rememberExpandedDoorIds(ids: ReadonlySet<number>): void {
    this.update({
      ...this.state(),
      expandedDoorIds: normalizeDoorIds(ids),
    });
  }

  rememberAnchorDoor(doorId: number): void {
    if (!isDoorId(doorId) || this.state().anchorDoorId === doorId) {
      return;
    }
    this.update({ ...this.state(), anchorDoorId: doorId });
  }

  forgetAnchorDoor(doorId: number): void {
    if (this.state().anchorDoorId !== doorId) {
      return;
    }
    this.update({ ...this.state(), anchorDoorId: null });
  }

  private update(state: AbwabManagementPickerSessionState): void {
    this.state.set(state);
    if (!this.isBrowser) {
      return;
    }
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    } catch {
      return;
    }
  }

  private read(): AbwabManagementPickerSessionState {
    if (!this.isBrowser) {
      return INITIAL_STATE;
    }
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      return raw === null ? INITIAL_STATE : parseState(raw);
    } catch {
      return INITIAL_STATE;
    }
  }
}

function parseState(raw: string): AbwabManagementPickerSessionState {
  let parsed: unknown;
  try {
    parsed = JSON.parse(raw);
  } catch {
    return INITIAL_STATE;
  }
  if (!parsed || typeof parsed !== 'object') {
    return INITIAL_STATE;
  }
  const record = parsed as Record<string, unknown>;
  const expandedDoorIds = Array.isArray(record['expandedDoorIds'])
    ? normalizeDoorIds(record['expandedDoorIds'])
    : [];
  const anchorDoorId = isDoorId(record['anchorDoorId']) ? record['anchorDoorId'] : null;
  return { expandedDoorIds, anchorDoorId };
}

function normalizeDoorIds(values: Iterable<unknown>): readonly number[] {
  return [...new Set(Array.from(values).filter(isDoorId))].sort((left, right) => left - right);
}

function isDoorId(value: unknown): value is number {
  return typeof value === 'number' && Number.isInteger(value) && value > 0;
}
