import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { Subscription, forkJoin } from 'rxjs';

import { LinkingSourceResolver } from '../data-access/linking-source-resolver';
import { LinkingAyah } from '../models/linking-ayah.models';
import {
  LinkingAyahClassification,
  LinkingAyahPreflight,
  LinkingPreflightResult,
  LinkingSourcePreflight,
} from '../models/linking-preflight.models';
import { LinkingSourceDescriptor } from '../models/linking-source.models';
import { LinkingSourceSetOperationResult } from '../models/linking-workflow.models';
import { LINKING_LABELS } from '../models/linking.labels';

export type LinkingPreflightAyahFilter = 'ALL' | LinkingAyahClassification;
export type LinkingPreflightPreviewStatus = 'idle' | 'loading' | 'ready' | 'error';

export interface LinkingPreflightAyahView {
  preflight: LinkingAyahPreflight;
  ayah: LinkingAyah;
}

interface LinkingPreflightSourcePreviewState {
  expanded: boolean;
  filter: LinkingPreflightAyahFilter;
  status: LinkingPreflightPreviewStatus;
  errorMessage: string | null;
  ayahsByVerseKey: ReadonlyMap<string, LinkingAyah>;
}

const PREVIEW_CHUNK_SIZE = 1_000;

@Injectable()
export class LinkingPreflightPreviewFacade {
  private readonly resolver = inject(LinkingSourceResolver);
  private readonly destroyRef = inject(DestroyRef);
  private readonly statesSignal = signal<ReadonlyMap<string, LinkingPreflightSourcePreviewState>>(
    new Map(),
  );
  private readonly subscriptions = new Map<string, Subscription>();
  private preflightToken: string | null = null;
  private generation = 0;

  constructor() {
    this.destroyRef.onDestroy(() => this.reset());
  }

  synchronize(
    preflight: LinkingPreflightResult | null,
    operation: LinkingSourceSetOperationResult | null,
  ): void {
    const nextToken = preflight?.preflightToken ?? null;
    if (nextToken === this.preflightToken) {
      return;
    }

    this.cancelSubscriptions();
    this.preflightToken = nextToken;
    this.generation += 1;

    if (preflight === null) {
      this.statesSignal.set(new Map());
      return;
    }

    const operationAyahs = new Map(
      (operation?.mergedSelection.ayahs ?? []).map((selection) => [
        selection.verseKey,
        selection.ayah,
      ]),
    );
    const nextStates = new Map<string, LinkingPreflightSourcePreviewState>();

    for (const source of preflight.sources) {
      const ayahsByVerseKey = new Map<string, LinkingAyah>();
      for (const ayah of source.ayahs) {
        const resolvedAyah = operationAyahs.get(ayah.verseKey);
        if (resolvedAyah !== undefined) {
          ayahsByVerseKey.set(ayah.verseKey, resolvedAyah);
        }
      }
      nextStates.set(source.sourceIdentity, {
        expanded: false,
        filter: 'ALL',
        status: ayahsByVerseKey.size === uniqueVerseKeyCount(source) ? 'ready' : 'idle',
        errorMessage: null,
        ayahsByVerseKey,
      });
    }

    this.statesSignal.set(nextStates);
  }

  isExpanded(sourceIdentity: string): boolean {
    return this.statesSignal().get(sourceIdentity)?.expanded ?? false;
  }

  toggleSource(source: LinkingSourcePreflight): void {
    const current = this.statesSignal().get(source.sourceIdentity);
    if (current === undefined) {
      return;
    }

    const expanded = !current.expanded;
    this.updateSource(source.sourceIdentity, (state) => ({ ...state, expanded }));
    if (expanded && current.status === 'idle') {
      this.loadMissingAyahs(source);
    }
  }

  statusFor(sourceIdentity: string): LinkingPreflightPreviewStatus {
    return this.statesSignal().get(sourceIdentity)?.status ?? 'idle';
  }

  errorFor(sourceIdentity: string): string | null {
    return this.statesSignal().get(sourceIdentity)?.errorMessage ?? null;
  }

  filterFor(sourceIdentity: string): LinkingPreflightAyahFilter {
    return this.statesSignal().get(sourceIdentity)?.filter ?? 'ALL';
  }

  setFilter(sourceIdentity: string, filter: LinkingPreflightAyahFilter): void {
    this.updateSource(sourceIdentity, (state) => ({ ...state, filter }));
  }

  viewsFor(source: LinkingSourcePreflight): readonly LinkingPreflightAyahView[] {
    const state = this.statesSignal().get(source.sourceIdentity);
    if (state === undefined || state.status !== 'ready') {
      return [];
    }

    return source.ayahs.flatMap((preflight) => {
      if (!matchesFilter(preflight.classification, state.filter)) {
        return [];
      }
      const ayah = state.ayahsByVerseKey.get(preflight.verseKey);
      return ayah === undefined ? [] : [{ preflight, ayah }];
    });
  }

  retry(source: LinkingSourcePreflight): void {
    this.loadMissingAyahs(source);
  }

  private loadMissingAyahs(source: LinkingSourcePreflight): void {
    const current = this.statesSignal().get(source.sourceIdentity);
    if (current === undefined) {
      return;
    }

    const missingVerseKeys = [...new Set(source.ayahs.map((ayah) => ayah.verseKey))].filter(
      (verseKey) => !current.ayahsByVerseKey.has(verseKey),
    );
    if (missingVerseKeys.length === 0) {
      this.updateSource(source.sourceIdentity, (state) => ({
        ...state,
        status: 'ready',
        errorMessage: null,
      }));
      return;
    }

    this.subscriptions.get(source.sourceIdentity)?.unsubscribe();
    this.updateSource(source.sourceIdentity, (state) => ({
      ...state,
      status: 'loading',
      errorMessage: null,
    }));

    const requests = chunk(missingVerseKeys, PREVIEW_CHUNK_SIZE).map((verseKeys) =>
      this.resolver.resolve(manualPreviewSource(verseKeys), () => undefined),
    );
    const requestGeneration = this.generation;
    const subscription = forkJoin(requests).subscribe({
      next: (chunks) => {
        if (requestGeneration !== this.generation) {
          return;
        }
        this.updateSource(source.sourceIdentity, (state) => {
          const ayahsByVerseKey = new Map(state.ayahsByVerseKey);
          for (const ayah of chunks.flat()) {
            ayahsByVerseKey.set(ayah.verseKey, ayah);
          }
          return { ...state, status: 'ready', errorMessage: null, ayahsByVerseKey };
        });
      },
      error: () => {
        if (requestGeneration !== this.generation) {
          return;
        }
        this.updateSource(source.sourceIdentity, (state) => ({
          ...state,
          status: 'error',
          errorMessage: LINKING_LABELS.preflightAyahPreviewError,
        }));
      },
    });
    this.subscriptions.set(source.sourceIdentity, subscription);
  }

  private updateSource(
    sourceIdentity: string,
    update: (state: LinkingPreflightSourcePreviewState) => LinkingPreflightSourcePreviewState,
  ): void {
    const currentStates = this.statesSignal();
    const current = currentStates.get(sourceIdentity);
    if (current === undefined) {
      return;
    }
    const nextStates = new Map(currentStates);
    nextStates.set(sourceIdentity, update(current));
    this.statesSignal.set(nextStates);
  }

  private reset(): void {
    this.cancelSubscriptions();
    this.generation += 1;
    this.statesSignal.set(new Map());
  }

  private cancelSubscriptions(): void {
    for (const subscription of this.subscriptions.values()) {
      subscription.unsubscribe();
    }
    this.subscriptions.clear();
  }
}

function uniqueVerseKeyCount(source: LinkingSourcePreflight): number {
  return new Set(source.ayahs.map((ayah) => ayah.verseKey)).size;
}

function matchesFilter(
  classification: LinkingAyahClassification,
  filter: LinkingPreflightAyahFilter,
): boolean {
  if (filter === 'ALL') {
    return true;
  }
  return filter === 'UPDATE'
    ? classification === 'UPDATE' || classification === 'OVERLAP_OTHER_SOURCE'
    : classification === filter;
}

function manualPreviewSource(verseKeys: readonly string[]): LinkingSourceDescriptor {
  return {
    kind: 'manual-mushaf-ayahs',
    label: LINKING_LABELS.preflightAyahPreviewSource,
    manualAyahs: verseKeys.map((verseKey) => ({ verseKey, pageNumber: null, displayHint: verseKey })),
  };
}

function chunk<T>(items: readonly T[], size: number): readonly (readonly T[])[] {
  const chunks: T[][] = [];
  for (let index = 0; index < items.length; index += size) {
    chunks.push(items.slice(index, index + size));
  }
  return chunks;
}
