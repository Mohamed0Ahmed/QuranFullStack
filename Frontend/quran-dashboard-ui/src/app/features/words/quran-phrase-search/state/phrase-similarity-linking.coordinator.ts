import { Injectable, OnDestroy, computed, inject, signal } from '@angular/core';
import { Subscription, finalize } from 'rxjs';

import { LinkingAccessService } from '../../../linking/state/linking-access.service';
import { LinkingFocusCoordinator } from '../../../linking/state/linking-focus.coordinator';
import { LinkingWorkflowFacade } from '../../../linking/state/linking-workflow.facade';
import { LinkingWorkspaceStore } from '../../../linking/state/linking-workspace.store';
import { PhraseSimilarityApi } from '../data-access/phrase-similarity.api';
import {
  PhraseSimilarityLinkingPopulationSnapshot,
  createPhraseSimilarityLinkingLaunch,
} from '../utils/phrase-similarity-linking-launch';
import {
  PhraseSimilarityAyahSelectionSnapshot,
  PhraseSimilarityAyahSelectionStore,
  phraseSimilarityResultSetKey,
} from './phrase-similarity-ayah-selection.store';
import { PhraseSimilarityFacade } from './phrase-similarity.facade';
import { phraseEnvelopeFailure, phraseRequestFailure } from './phrase-request-failure';

type PhraseSimilarityLinkingAction = 'workspace' | 'direct';

interface PhraseSimilarityLinkingRequestIdentity {
  readonly revision: number;
  readonly resultSetKey: string;
  readonly populationKey: string;
}

interface PhraseSimilarityLinkingError extends PhraseSimilarityLinkingRequestIdentity {
  readonly message: string;
}

@Injectable()
export class PhraseSimilarityLinkingCoordinator implements OnDestroy {
  private readonly api = inject(PhraseSimilarityApi);
  private readonly facade = inject(PhraseSimilarityFacade);
  private readonly selection = inject(PhraseSimilarityAyahSelectionStore);
  private readonly access = inject(LinkingAccessService);
  private readonly focus = inject(LinkingFocusCoordinator);
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly workflow = inject(LinkingWorkflowFacade);
  private readonly activeRequest = signal<PhraseSimilarityLinkingRequestIdentity | null>(null);
  private readonly errorState = signal<PhraseSimilarityLinkingError | null>(null);
  private requestId = 0;
  private requestSubscription?: Subscription;

  readonly canUseLinking = this.access.canUseLinking;
  readonly resolving = computed(() => {
    const active = this.activeRequest();
    return active !== null && this.matchesCurrentIdentity(active);
  });
  readonly errorMessage = computed(() => {
    const error = this.errorState();
    return error !== null && this.matchesCurrentIdentity(error) ? error.message : '';
  });

  addToWorkspace(): void {
    this.resolve('workspace');
  }

  startDirectLink(): void {
    this.resolve('direct');
  }

  dismissError(): void {
    this.errorState.set(null);
  }

  ngOnDestroy(): void {
    this.cancelRequest();
  }

  private resolve(action: PhraseSimilarityLinkingAction): void {
    const snapshot = this.capturePopulation();
    if (snapshot === null || this.resolving()) {
      return;
    }

    this.cancelRequest();
    const requestId = this.requestId;
    const identity = requestIdentity(snapshot);
    this.activeRequest.set(identity);
    this.errorState.set(null);
    this.focus.capture('inline-source-action');
    this.requestSubscription = this.api
      .resolveLinkingSelection({
        resolutionRef: snapshot.resolutionRef,
        minimumMatchedWords: snapshot.minimumMatchedWords,
        selectionMode: snapshot.selection.mode,
        ayahIds: [...snapshot.selection.ayahIds],
      })
      .pipe(finalize(() => this.settleRequest(requestId)))
      .subscribe({
        next: (response) => {
          if (!this.isCurrent(requestId, snapshot)) {
            return;
          }
          if (!response.isSuccess || !response.data) {
            this.fail(identity, phraseEnvelopeFailure(response.errors, response.message).message);
            return;
          }
          const launch = createPhraseSimilarityLinkingLaunch(response.data, snapshot);
          if (launch === null) {
            this.fail(identity, 'تعذر تجهيز آيات التشابه المحددة. حدّث النتائج ثم أعد المحاولة.');
            return;
          }
          if (action === 'workspace') {
            if (this.workspace.addSource(launch) === null) {
              this.fail(identity, 'تعذر إضافة آيات التشابه إلى مساحة الربط الآن.');
            }
            return;
          }
          if (!this.workflow.startFromSource(launch)) {
            this.fail(identity, 'تعذر بدء الربط المباشر لآيات التشابه الآن.');
          }
        },
        error: (error: unknown) => {
          if (this.isCurrent(requestId, snapshot)) {
            this.fail(identity, phraseRequestFailure(error).message);
          }
        },
      });
  }

  private capturePopulation(): PhraseSimilarityLinkingPopulationSnapshot | null {
    const state = this.facade.state();
    const route = state.route;
    const query = state.queryPhrase;
    const selection = this.selection.snapshot();
    const minimumMatchedWords = this.facade.minimumMatchedWords();
    const resultSetKey = phraseSimilarityResultSetKey(
      route.build,
      route.resolution,
      minimumMatchedWords,
    );

    if (
      !this.canUseLinking() ||
      state.resultsStatus !== 'success' ||
      state.queryDraftPending ||
      route.build === null ||
      route.build.trim().length === 0 ||
      route.resolution === null ||
      route.resolution.trim().length === 0 ||
      route.q.trim().length === 0 ||
      query === null ||
      !Number.isSafeInteger(query.variantId) ||
      query.variantId <= 0 ||
      query.displayText.trim().length === 0 ||
      query.wordCount !== route.length ||
      selection.resultSetKey !== resultSetKey ||
      selection.selectedCount <= 0 ||
      selection.selectedCount > selection.totalAyahCount ||
      selection.totalAyahCount !== state.totalAyahCount
    ) {
      return null;
    }

    return {
      resultSetKey,
      routeQuery: route.q,
      activeBuildId: route.build,
      resolutionRef: route.resolution,
      minimumMatchedWords,
      queryVariantId: query.variantId,
      queryDisplayText: query.displayText,
      queryWordCount: query.wordCount,
      selection,
    };
  }

  private isCurrent(
    requestId: number,
    snapshot: PhraseSimilarityLinkingPopulationSnapshot,
  ): boolean {
    const current = this.capturePopulation();
    return requestId === this.requestId &&
      current !== null &&
      samePopulation(snapshot, current) &&
      sameSelection(snapshot.selection, current.selection);
  }

  private matchesCurrentIdentity(identity: PhraseSimilarityLinkingRequestIdentity): boolean {
    const current = this.capturePopulation();
    return current !== null &&
      identity.revision === current.selection.revision &&
      identity.resultSetKey === current.resultSetKey &&
      identity.populationKey === populationKey(current);
  }

  private fail(identity: PhraseSimilarityLinkingRequestIdentity, message: string): void {
    this.errorState.set({ ...identity, message });
  }

  private cancelRequest(): void {
    this.requestId += 1;
    this.requestSubscription?.unsubscribe();
    this.requestSubscription = undefined;
    this.activeRequest.set(null);
  }

  private settleRequest(requestId: number): void {
    if (requestId !== this.requestId) {
      return;
    }
    this.requestSubscription = undefined;
    this.activeRequest.set(null);
  }
}

function requestIdentity(
  snapshot: PhraseSimilarityLinkingPopulationSnapshot,
): PhraseSimilarityLinkingRequestIdentity {
  return {
    revision: snapshot.selection.revision,
    resultSetKey: snapshot.resultSetKey,
    populationKey: populationKey(snapshot),
  };
}

function populationKey(snapshot: PhraseSimilarityLinkingPopulationSnapshot): string {
  return JSON.stringify([
    snapshot.resultSetKey,
    snapshot.routeQuery,
    snapshot.activeBuildId.toLowerCase(),
    snapshot.resolutionRef,
    snapshot.minimumMatchedWords,
    snapshot.queryVariantId,
    snapshot.queryDisplayText,
    snapshot.queryWordCount,
  ]);
}

function samePopulation(
  left: PhraseSimilarityLinkingPopulationSnapshot,
  right: PhraseSimilarityLinkingPopulationSnapshot,
): boolean {
  return populationKey(left) === populationKey(right);
}

function sameSelection(
  left: PhraseSimilarityAyahSelectionSnapshot,
  right: PhraseSimilarityAyahSelectionSnapshot,
): boolean {
  return left.revision === right.revision &&
    left.resultSetKey === right.resultSetKey &&
    left.mode === right.mode &&
    left.selectedCount === right.selectedCount &&
    left.totalAyahCount === right.totalAyahCount &&
    left.ayahIds.length === right.ayahIds.length &&
    left.ayahIds.every((ayahId, index) => ayahId === right.ayahIds[index]);
}
