import { Injectable, OnDestroy, computed, inject, signal } from '@angular/core';
import { Subscription, finalize } from 'rxjs';

import { LinkingAccessService } from '../../../linking/state/linking-access.service';
import { LinkingFocusCoordinator } from '../../../linking/state/linking-focus.coordinator';
import { LinkingWorkflowFacade } from '../../../linking/state/linking-workflow.facade';
import { LinkingWorkspaceStore } from '../../../linking/state/linking-workspace.store';
import { PhraseContextApi } from '../data-access/phrase-context.api';
import { PhraseContextUrlState } from '../models/phrase-context.models';
import { createPhraseContextLinkingLaunch } from '../utils/phrase-context-linking-launch';
import {
  PhraseLinkingAyahSelectionSnapshot,
  PhraseLinkingAyahSelectionStore,
} from './phrase-linking-ayah-selection.store';
import { phraseContextResultSetKey } from './phrase-context-url-sync';
import { phraseEnvelopeFailure, phraseRequestFailure } from './phrase-request-failure';

type PhraseContextLinkingAction = 'workspace' | 'direct';

interface PhraseContextLinkingError {
  readonly revision: number;
  readonly resultSetKey: string;
  readonly message: string;
}

@Injectable()
export class PhraseContextLinkingCoordinator implements OnDestroy {
  private readonly api = inject(PhraseContextApi);
  private readonly selection = inject(PhraseLinkingAyahSelectionStore);
  private readonly access = inject(LinkingAccessService);
  private readonly focus = inject(LinkingFocusCoordinator);
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly workflow = inject(LinkingWorkflowFacade);
  private readonly activeRevision = signal<number | null>(null);
  private readonly errorState = signal<PhraseContextLinkingError | null>(null);
  private requestId = 0;
  private requestSubscription?: Subscription;

  readonly canUseLinking = this.access.canUseLinking;
  readonly resolving = computed(() => this.activeRevision() === this.selection.revision());
  readonly errorMessage = computed(() => {
    const error = this.errorState();
    return error !== null &&
      error.revision === this.selection.revision() &&
      error.resultSetKey === this.selection.resultSetKey()
      ? error.message
      : '';
  });

  addToWorkspace(route: PhraseContextUrlState): void {
    this.resolve(route, 'workspace');
  }

  startDirectLink(route: PhraseContextUrlState): void {
    this.resolve(route, 'direct');
  }

  dismissError(): void {
    this.errorState.set(null);
  }

  ngOnDestroy(): void {
    this.cancelRequest();
  }

  private resolve(route: PhraseContextUrlState, action: PhraseContextLinkingAction): void {
    const selection = this.selection.snapshot();
    if (!this.canResolve(route, selection) || this.resolving()) {
      return;
    }

    this.cancelRequest();
    const requestId = this.requestId;
    this.activeRevision.set(selection.revision);
    this.errorState.set(null);
    this.focus.capture('inline-source-action');
    this.requestSubscription = this.api
      .resolveLinkingSelection({
        resolutionRef: route.resolution,
        previousRef: route.before,
        followingRef: route.after,
        previousAlternativesRef: route.previousAlternatives,
        followingAlternativesRef: route.followingAlternatives,
        selectionMode: selection.mode,
        ayahIds: [...selection.ayahIds],
      })
      .pipe(finalize(() => this.settleRequest(requestId)))
      .subscribe({
        next: (response) => {
          if (!this.isCurrent(requestId, selection)) {
            return;
          }
          if (!response.isSuccess || !response.data) {
            this.fail(selection, phraseEnvelopeFailure(response.errors, response.message).message);
            return;
          }
          if (!sameBuild(route.build, response.data.activeBuildId)) {
            this.fail(selection, 'تغيّر فهرس البحث. حدّث النتائج ثم أعد محاولة الربط.');
            return;
          }
          const launch = createPhraseContextLinkingLaunch(response.data, route.q, selection);
          if (launch === null) {
            this.fail(selection, 'تعذر تجهيز الآيات المحددة للربط. حدّث النتائج ثم أعد المحاولة.');
            return;
          }
          if (action === 'workspace') {
            if (this.workspace.addSource(launch) === null) {
              this.fail(selection, 'تعذر إضافة الآيات إلى مساحة الربط الآن.');
            }
            return;
          }
          if (!this.workflow.startFromSource(launch)) {
            this.fail(selection, 'تعذر بدء الربط المباشر الآن.');
          }
        },
        error: (error: unknown) => {
          if (this.isCurrent(requestId, selection)) {
            this.fail(selection, phraseRequestFailure(error).message);
          }
        },
      });
  }

  private canResolve(
    route: PhraseContextUrlState,
    selection: PhraseLinkingAyahSelectionSnapshot,
  ): boolean {
    return this.canUseLinking() &&
      route.resolution !== null &&
      route.build !== null &&
      route.q.trim().length > 0 &&
      selection.resultSetKey === phraseContextResultSetKey(route) &&
      selection.selectedCount > 0 &&
      selection.selectedCount <= selection.totalAyahCount;
  }

  private isCurrent(
    requestId: number,
    selection: PhraseLinkingAyahSelectionSnapshot,
  ): boolean {
    return requestId === this.requestId &&
      selection.revision === this.selection.revision() &&
      selection.resultSetKey === this.selection.resultSetKey();
  }

  private fail(selection: PhraseLinkingAyahSelectionSnapshot, message: string): void {
    this.errorState.set({
      revision: selection.revision,
      resultSetKey: selection.resultSetKey,
      message,
    });
  }

  private cancelRequest(): void {
    this.requestId += 1;
    this.requestSubscription?.unsubscribe();
    this.requestSubscription = undefined;
    this.activeRevision.set(null);
  }

  private settleRequest(requestId: number): void {
    if (requestId !== this.requestId) {
      return;
    }
    this.requestSubscription = undefined;
    this.activeRevision.set(null);
  }
}

function sameBuild(expected: string | null, actual: string): boolean {
  return expected !== null && expected.toLowerCase() === actual.toLowerCase();
}
