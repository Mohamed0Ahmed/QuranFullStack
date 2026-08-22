import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject } from '@angular/core';

import { AbwabNode, AbwabTreeSnapshotVm } from '../../abwab/models/abwab.models';
import {
  MUSHAF_DOOR_COLOR_SLOTS,
  MushafDoorColorSlot,
} from '../models/mushaf-door-highlights.models';

export interface MushafDoorsHighlightSessionState {
  readonly draftDoorIds: readonly number[];
  readonly appliedDoorIds: readonly number[];
  readonly colorSlots: readonly (readonly [number, MushafDoorColorSlot])[];
}

const STORAGE_KEY = 'quran-dashboard.mushaf.doors-highlight-session.v1';
const STORAGE_VERSION = 1;
const MAX_SELECTED_DOORS = MUSHAF_DOOR_COLOR_SLOTS.length;
const EMPTY_STATE: MushafDoorsHighlightSessionState = {
  draftDoorIds: [],
  appliedDoorIds: [],
  colorSlots: [],
};
const ALLOWED_COLOR_SLOTS = new Set<number>(MUSHAF_DOOR_COLOR_SLOTS);

@Injectable({ providedIn: 'root' })
export class MushafDoorsHighlightSessionStore {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  read(): MushafDoorsHighlightSessionState {
    if (!this.isBrowser) {
      return EMPTY_STATE;
    }
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      if (raw === null) {
        return EMPTY_STATE;
      }
      const state = parseState(raw);
      this.write(state);
      return state;
    } catch {
      return EMPTY_STATE;
    }
  }

  write(state: MushafDoorsHighlightSessionState): void {
    if (!this.isBrowser) {
      return;
    }
    const normalized = normalizeState(state);
    try {
      if (!hasDoorSelection(normalized)) {
        sessionStorage.removeItem(STORAGE_KEY);
        return;
      }
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify({
        v: STORAGE_VERSION,
        d: normalized.draftDoorIds,
        a: normalized.appliedDoorIds,
        c: normalized.colorSlots,
      }));
    } catch {
      return;
    }
  }

  rebind(
    state: MushafDoorsHighlightSessionState,
    snapshot: AbwabTreeSnapshotVm,
  ): MushafDoorsHighlightSessionState {
    const liveDoorIds = collectLiveDoorIds(snapshot.liveRoots);
    return normalizeState({
      draftDoorIds: state.draftDoorIds.filter((id) => liveDoorIds.has(id)),
      appliedDoorIds: state.appliedDoorIds.filter((id) => liveDoorIds.has(id)),
      colorSlots: state.colorSlots,
    });
  }
}

export function hasDoorSelection(state: MushafDoorsHighlightSessionState): boolean {
  return state.draftDoorIds.length > 0 || state.appliedDoorIds.length > 0;
}

function parseState(raw: string): MushafDoorsHighlightSessionState {
  let parsed: unknown;
  try {
    parsed = JSON.parse(raw);
  } catch {
    return EMPTY_STATE;
  }
  if (!parsed || typeof parsed !== 'object') {
    return EMPTY_STATE;
  }
  const record = parsed as Record<string, unknown>;
  if (record['v'] !== STORAGE_VERSION) {
    return EMPTY_STATE;
  }
  return normalizeState({
    draftDoorIds: readDoorIds(record['d']),
    appliedDoorIds: readDoorIds(record['a']),
    colorSlots: readColorSlots(record['c']),
  });
}

function normalizeState(state: MushafDoorsHighlightSessionState): MushafDoorsHighlightSessionState {
  const draftDoorIds = normalizeDoorIds(state.draftDoorIds);
  const appliedDoorIds = normalizeDoorIds(state.appliedDoorIds);
  const appliedSet = new Set(appliedDoorIds);
  const usedDoorIds = new Set<number>();
  const usedSlots = new Set<MushafDoorColorSlot>();
  const colorByDoorId = new Map<number, MushafDoorColorSlot>();
  for (const [doorId, colorSlot] of state.colorSlots) {
    if (!appliedSet.has(doorId) || usedDoorIds.has(doorId) || usedSlots.has(colorSlot)) {
      continue;
    }
    usedDoorIds.add(doorId);
    usedSlots.add(colorSlot);
    colorByDoorId.set(doorId, colorSlot);
  }
  return {
    draftDoorIds,
    appliedDoorIds,
    colorSlots: appliedDoorIds.flatMap((doorId) => {
      const colorSlot = colorByDoorId.get(doorId);
      return colorSlot === undefined ? [] : [[doorId, colorSlot] as const];
    }),
  };
}

function readDoorIds(value: unknown): readonly number[] {
  return Array.isArray(value) ? value.filter(isDoorId) : [];
}

function normalizeDoorIds(values: readonly number[]): readonly number[] {
  return [...new Set(values.filter(isDoorId))].slice(0, MAX_SELECTED_DOORS);
}

function readColorSlots(value: unknown): readonly (readonly [number, MushafDoorColorSlot])[] {
  if (!Array.isArray(value)) {
    return [];
  }
  return value.flatMap((entry) => {
    if (!Array.isArray(entry) || entry.length !== 2 || !isDoorId(entry[0]) || !isColorSlot(entry[1])) {
      return [];
    }
    return [[entry[0], entry[1]] as const];
  });
}

function collectLiveDoorIds(roots: readonly AbwabNode[]): ReadonlySet<number> {
  const ids = new Set<number>();
  const visit = (node: AbwabNode): void => {
    ids.add(node.id);
    node.children.forEach(visit);
  };
  roots.forEach(visit);
  return ids;
}

function isDoorId(value: unknown): value is number {
  return typeof value === 'number' && Number.isInteger(value) && value > 0;
}

function isColorSlot(value: unknown): value is MushafDoorColorSlot {
  return typeof value === 'number' && ALLOWED_COLOR_SLOTS.has(value);
}
