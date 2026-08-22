import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, computed, effect, inject, signal, untracked } from '@angular/core';

import { AbwabNode } from '../models/abwab.models';
import { AbwabSnapshotFacade } from './abwab-snapshot.facade';

interface AbwabMainTreeSessionState {
  readonly expandedDoorIds: readonly number[];
}

const STORAGE_KEY = 'quran-dashboard.abwab.main-tree-session.v1';
const INITIAL_STATE: AbwabMainTreeSessionState = { expandedDoorIds: [] };

@Injectable({ providedIn: 'root' })
export class AbwabMainTreeSessionStore {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly facade = inject(AbwabSnapshotFacade);
  private readonly state = signal<AbwabMainTreeSessionState>(this.read());

  readonly expandedDoorIds = computed<ReadonlySet<number>>(
    () => new Set(this.state().expandedDoorIds),
  );

  constructor() {
    effect(() => {
      const snapshot = this.facade.snapshot();
      if (!snapshot) {
        return;
      }
      const validIds = collectExpandableDoorIds(snapshot.liveRoots);
      const expandedDoorIds = this.state().expandedDoorIds.filter((id) => validIds.has(id));
      if (!arraysEqual(expandedDoorIds, this.state().expandedDoorIds)) {
        untracked(() => this.update({ expandedDoorIds }));
      }
    });
  }

  rememberExpandedDoorIds(ids: ReadonlySet<number>): void {
    let expandedDoorIds = normalizeDoorIds(ids);
    const snapshot = this.facade.snapshot();
    if (snapshot) {
      const validIds = collectExpandableDoorIds(snapshot.liveRoots);
      expandedDoorIds = expandedDoorIds.filter((id) => validIds.has(id));
    }
    if (!arraysEqual(expandedDoorIds, this.state().expandedDoorIds)) {
      this.update({ expandedDoorIds });
    }
  }

  private update(state: AbwabMainTreeSessionState): void {
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

  private read(): AbwabMainTreeSessionState {
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

function parseState(raw: string): AbwabMainTreeSessionState {
  let parsed: unknown;
  try {
    parsed = JSON.parse(raw);
  } catch {
    return INITIAL_STATE;
  }
  if (!parsed || typeof parsed !== 'object') {
    return INITIAL_STATE;
  }
  const values = (parsed as Record<string, unknown>)['expandedDoorIds'];
  return {
    expandedDoorIds: Array.isArray(values) ? normalizeDoorIds(values) : [],
  };
}

function collectExpandableDoorIds(roots: readonly AbwabNode[]): ReadonlySet<number> {
  const ids = new Set<number>();
  const visit = (node: AbwabNode): void => {
    if (node.children.length > 0) {
      ids.add(node.id);
    }
    node.children.forEach(visit);
  };
  roots.forEach(visit);
  return ids;
}

function normalizeDoorIds(values: Iterable<unknown>): number[] {
  return [...new Set(Array.from(values).filter(isDoorId))].sort((left, right) => left - right);
}

function arraysEqual(left: readonly number[], right: readonly number[]): boolean {
  return left.length === right.length && left.every((value, index) => value === right[index]);
}

function isDoorId(value: unknown): value is number {
  return typeof value === 'number' && Number.isInteger(value) && value > 0;
}
