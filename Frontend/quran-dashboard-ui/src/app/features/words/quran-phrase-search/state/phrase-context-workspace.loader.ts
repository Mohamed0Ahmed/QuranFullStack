import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, map, of, switchMap } from 'rxjs';

import { PhraseContextBranchesResponse } from '../../../../core/api/generated/models/phrase-context-branches-response';
import { PhraseContextGroupsResponse } from '../../../../core/api/generated/models/phrase-context-groups-response';
import { PhraseContextOccurrencesResponse } from '../../../../core/api/generated/models/phrase-context-occurrences-response';
import { PhraseContextResultsResponse } from '../../../../core/api/generated/models/phrase-context-results-response';
import { PhraseContextApi } from '../data-access/phrase-context.api';
import {
  PHRASE_CONTEXT_BRANCH_PAGE_SIZE,
  PHRASE_CONTEXT_RESULT_PAGE_SIZE,
  PhraseContextUrlState,
} from '../models/phrase-context.models';
import { PhraseRequestFailure, phraseEnvelopeFailure } from './phrase-request-failure';

interface PhraseContextLoadFailure {
  readonly kind: 'failure';
  readonly failure: PhraseRequestFailure;
}

interface PhraseContextWorkspaceSuccess {
  readonly kind: 'workspace';
  readonly activeBuildId: string;
  readonly branches: PhraseContextBranchesResponse;
  readonly results: PhraseContextResultsResponse;
}

interface PhraseContextBranchesSuccess {
  readonly kind: 'branches';
  readonly activeBuildId: string;
  readonly branches: PhraseContextBranchesResponse;
}

interface PhraseContextResultsSuccess {
  readonly kind: 'results';
  readonly activeBuildId: string;
  readonly results: PhraseContextResultsResponse;
}

interface PhraseContextGroupsSuccess {
  readonly kind: 'groups';
  readonly activeBuildId: string;
  readonly groups: PhraseContextGroupsResponse;
}

interface PhraseContextOccurrencesSuccess {
  readonly kind: 'occurrences';
  readonly activeBuildId: string;
  readonly occurrences: PhraseContextOccurrencesResponse;
}

export type PhraseContextLoadResult =
  | PhraseContextLoadFailure
  | PhraseContextWorkspaceSuccess
  | PhraseContextBranchesSuccess
  | PhraseContextResultsSuccess
  | PhraseContextGroupsSuccess
  | PhraseContextOccurrencesSuccess;

@Injectable()
export class PhraseContextWorkspaceLoader {
  private readonly api = inject(PhraseContextApi);

  loadWorkspace(route: PhraseContextUrlState): Observable<PhraseContextLoadResult> {
    return forkJoin([
      this.api.getBranches(
        route.resolution!,
        route.before,
        route.after,
        null,
        null,
        PHRASE_CONTEXT_BRANCH_PAGE_SIZE,
      ),
      this.api.getResults(
        route.resolution!,
        route.before,
        route.after,
        route.contextsPage,
        PHRASE_CONTEXT_RESULT_PAGE_SIZE,
      ),
    ]).pipe(
      switchMap(([branches, results]) => {
        if (!branches.isSuccess || !branches.data) {
          return of(failureResult(branches.errors, branches.message));
        }
        if (!results.isSuccess || !results.data) {
          return of(failureResult(results.errors, results.message));
        }
        if (!sameBuild(branches.data.activeBuildId, results.data.activeBuildId)) {
          return of(invalidResult('استجابات السياق لا تنتمي إلى جيل فهرس واحد.'));
        }
        return of({
          kind: 'workspace' as const,
          activeBuildId: branches.data.activeBuildId,
          branches: branches.data,
          results: results.data,
        });
      }),
    );
  }

  loadResultsPage(route: PhraseContextUrlState): Observable<PhraseContextLoadResult> {
    return this.api
      .getResults(
        route.resolution!,
        route.before,
        route.after,
        route.contextsPage,
        PHRASE_CONTEXT_RESULT_PAGE_SIZE,
      )
      .pipe(
        map((response) =>
          response.isSuccess && response.data
            ? {
                kind: 'results' as const,
                activeBuildId: response.data.activeBuildId,
                results: response.data,
              }
            : failureResult(response.errors, response.message),
        ),
      );
  }

  loadBranchPage(
    route: PhraseContextUrlState,
    previousCursor: string | null,
    followingCursor: string | null,
  ): Observable<PhraseContextLoadResult> {
    return this.api
      .getBranches(
        route.resolution!,
        route.before,
        route.after,
        previousCursor,
        followingCursor,
        PHRASE_CONTEXT_BRANCH_PAGE_SIZE,
      )
      .pipe(
        map((response) =>
          response.isSuccess && response.data
            ? {
                kind: 'branches' as const,
                activeBuildId: response.data.activeBuildId,
                branches: response.data,
              }
            : failureResult(response.errors, response.message),
        ),
      );
  }

  loadGroupsPage(
    route: PhraseContextUrlState,
    cursor: string,
  ): Observable<PhraseContextLoadResult> {
    return this.api
      .getGroups(
        route.resolution!,
        route.before,
        route.after,
        cursor,
        PHRASE_CONTEXT_RESULT_PAGE_SIZE,
      )
      .pipe(
        map((response) =>
          response.isSuccess && response.data
            ? {
                kind: 'groups' as const,
                activeBuildId: response.data.activeBuildId,
                groups: response.data,
              }
            : failureResult(response.errors, response.message),
        ),
      );
  }

  loadOccurrences(
    contextRef: string,
    cursor: string | null,
  ): Observable<PhraseContextLoadResult> {
    return this.api.getOccurrences(contextRef, cursor, PHRASE_CONTEXT_RESULT_PAGE_SIZE).pipe(
      map((response) =>
        response.isSuccess && response.data
          ? {
              kind: 'occurrences' as const,
              activeBuildId: response.data.activeBuildId,
              occurrences: response.data,
            }
          : failureResult(response.errors, response.message),
      ),
    );
  }
}

function failureResult(
  errors: readonly string[] | null,
  message: string | null,
): PhraseContextLoadFailure {
  return { kind: 'failure', failure: phraseEnvelopeFailure(errors, message) };
}

function invalidResult(message: string): PhraseContextLoadFailure {
  return { kind: 'failure', failure: { status: 'invalid', message } };
}

function sameBuild(expected: string, actual: string): boolean {
  return expected.toLowerCase() === actual.toLowerCase();
}
