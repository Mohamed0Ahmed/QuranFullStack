import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { Subscription, catchError, forkJoin, map, throwError } from 'rxjs';

import { LinkingSourceResolver } from '../data-access/linking-source-resolver';
import { LinkingAyah } from '../models/linking-ayah.models';
import { LinkingManualWordIdsByVerseKey } from '../models/linking-manual-mushaf.models';
import { LinkingSourceIntent } from '../models/linking-merge.models';
import { LinkingOperationMember, LinkingOperationMemberLoadState } from '../models/linking-operation.models';
import { LinkingSourceSetOperationResult } from '../models/linking-workflow.models';
import { applyLinkingSourceConfiguration } from '../utils/apply-linking-source-configuration';
import { createLinkingSourceIntent } from '../utils/linking-source-intents';
import { mergeLinkingSources, ResolvedLinkingSourceMember } from '../utils/linking-merge';
import { orderedOperationMembers } from '../utils/linking-operation-members';
import { reconcileLinkingSelection, selectedLinkingVerseKeys } from '../utils/linking-selection';
import { LinkingAccessService } from './linking-access.service';
import { LinkingWorkspaceStore } from './linking-workspace.store';

interface LinkingSourceSetState {
  generation: number;
  members: readonly LinkingOperationMemberLoadState[];
  result: LinkingSourceSetOperationResult | null;
}

const INITIAL_STATE: LinkingSourceSetState = { generation: 0, members: [], result: null };

@Injectable({ providedIn: 'root' })
export class LinkingSourceSetCoordinator {
  private readonly access = inject(LinkingAccessService);
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly resolver = inject(LinkingSourceResolver);
  private readonly stateSignal = signal<LinkingSourceSetState>(INITIAL_STATE);
  private subscription: Subscription | null = null;
  private members: readonly LinkingOperationMember[] = [];

  readonly state = this.stateSignal.asReadonly();
  readonly memberStates = computed(() => this.stateSignal().members);
  readonly result = computed(() => this.stateSignal().result);
  readonly canContinue = computed(() => this.result() !== null && this.result()!.mergedSelection.ayahs.length > 0);

  constructor() {
    effect(() => {
      if (!this.access.canUseLinking() && this.members.length > 0) {
        untracked(() => this.cancel());
      }
    });
    effect(() => {
      if (this.workspace.activeSurface() === 'closed' && this.members.length > 0) {
        untracked(() => this.cancel());
      }
    });
  }

  resolve(members: readonly LinkingOperationMember[]): void {
    this.cancelSubscription();
    const operationMembers = orderedOperationMembers(members);
    const generation = this.stateSignal().generation + 1;
    this.members = operationMembers;
    this.stateSignal.set({
      generation,
      result: null,
      members: operationMembers.map((member) => ({
        member,
        status: 'loading',
        progress: { loaded: 0, total: null },
        errorMessage: null,
        contributedAyahCount: null,
      })),
    });
    if (!this.access.canUseLinking() || operationMembers.length === 0) {
      return;
    }
    try {
      this.subscription = forkJoin(
        operationMembers.map((member) =>
          this.resolver.resolve(member.source, (progress) => this.publishProgress(generation, member.sourceKey, progress)).pipe(
            map((ayahs) => this.resolveMember(member, ayahs)),
            catchError((error: unknown) => throwError(() => new MemberResolutionError(member.sourceKey, error))),
          ),
        ),
      ).subscribe({
        next: (resolvedMembers) => this.publishSuccess(generation, resolvedMembers),
        error: (error: unknown) => this.publishFailure(generation, error),
      });
    } catch (error: unknown) {
      this.publishFailure(generation, error);
    }
  }

  retry(sourceKey: string): void {
    if (this.members.some((member) => member.sourceKey === sourceKey)) {
      this.resolve(this.members);
    }
  }

  cancel(): void {
    this.cancelSubscription();
    this.members = [];
    this.stateSignal.set({ ...INITIAL_STATE, generation: this.stateSignal().generation + 1 });
  }

  private resolveMember(member: LinkingOperationMember, ayahs: readonly LinkingAyah[]): ResolvedLinkingSourceMember {
    const universe = ayahs.map((ayah) => ayah.verseKey);
    let ayahInclusion = member.configuration.ayahInclusion;
    if (member.origin === 'workspace' && this.workspace.item(member.sourceKey)?.configurationRevision === member.configurationRevision) {
      this.workspace.reconcileResolvedSource(
        member.sourceKey,
        ayahs.map((ayah) => ({ ayahId: ayah.ayahId, verseKey: ayah.verseKey })),
      );
      ayahInclusion = this.workspace.item(member.sourceKey)?.configuration.ayahInclusion ?? ayahInclusion;
    }
    const selection = reconcileLinkingSelection(ayahInclusion, universe);
    if (member.configuration.kind === 'manual') {
      assertKnownManualWordIds(ayahs, member.configuration.quranWordIdsByVerseKey);
    }
    const includedVerseKeys = new Set(selectedLinkingVerseKeys(selection, universe));
    return {
      member,
      ayahs: ayahs
        .filter((ayah) => includedVerseKeys.has(ayah.verseKey))
        .map((ayah) => applyLinkingSourceConfiguration(member.configuration, ayah)),
    };
  }

  private publishProgress(
    generation: number,
    sourceKey: string,
    progress: { loaded: number; total: number },
  ): void {
    if (generation !== this.stateSignal().generation) {
      return;
    }
    this.stateSignal.update((state) => ({
      ...state,
      members: state.members.map((entry) =>
        entry.member.sourceKey === sourceKey
          ? { ...entry, status: 'loading', progress: { loaded: progress.loaded, total: progress.total } }
          : entry,
      ),
    }));
  }

  private publishSuccess(generation: number, resolvedMembers: readonly ResolvedLinkingSourceMember[]): void {
    if (generation !== this.stateSignal().generation || !this.access.canUseLinking()) {
      return;
    }
    try {
      const mergedSelection = mergeLinkingSources(resolvedMembers);
      const sourceIntents: readonly LinkingSourceIntent[] = resolvedMembers.map(({ member, ayahs }) =>
        createLinkingSourceIntent(member, ayahs),
      );
      this.stateSignal.update((state) => ({
        ...state,
        members: state.members.map((entry) => {
          const resolved = resolvedMembers.find((candidate) => candidate.member.sourceKey === entry.member.sourceKey);
          return resolved
            ? {
                ...entry,
                status: 'ready',
                contributedAyahCount: resolved.ayahs.length,
              }
            : entry;
        }),
        result: { mergedSelection, sourceIntents },
      }));
    } catch (error: unknown) {
      this.publishFailure(generation, error);
    }
  }

  private publishFailure(generation: number, error: unknown): void {
    if (generation !== this.stateSignal().generation) {
      return;
    }
    const failedSourceKey = error instanceof MemberResolutionError ? error.sourceKey : null;
    const message = error instanceof Error ? error.message : 'تعذر تحميل مصادر الربط كاملة.';
    this.stateSignal.update((state) => ({
      ...state,
      result: null,
      members: state.members.map((entry) =>
        failedSourceKey === null || entry.member.sourceKey === failedSourceKey
          ? { ...entry, status: 'error', errorMessage: message }
          : entry,
      ),
    }));
  }

  private cancelSubscription(): void {
    this.subscription?.unsubscribe();
    this.subscription = null;
  }
}

function assertKnownManualWordIds(
  ayahs: readonly LinkingAyah[],
  quranWordIdsByVerseKey: LinkingManualWordIdsByVerseKey,
): void {
  const wordIdsByVerseKey = new Map(
    ayahs.map((ayah) => [
      ayah.verseKey,
      new Set(ayah.words.filter((word) => !word.isAyahMarker).map((word) => word.canonicalQuranWordId)),
    ]),
  );
  for (const [verseKey, quranWordIds] of Object.entries(quranWordIdsByVerseKey)) {
    const knownWordIds = wordIdsByVerseKey.get(verseKey);
    if (!knownWordIds || quranWordIds.some((quranWordId) => !knownWordIds.has(quranWordId))) {
      throw new Error('الكلمات المحفوظة لهذا المصدر لم تعد صالحة.');
    }
  }
}

class MemberResolutionError extends Error {
  constructor(
    readonly sourceKey: string,
    error: unknown,
  ) {
    super(error instanceof Error ? error.message : 'تعذر تحميل المصدر كاملاً.');
  }
}
