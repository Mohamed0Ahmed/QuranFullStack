import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, map, of, switchMap, toArray } from 'rxjs';

import { PhraseContextBranchesResponse } from '../../../../core/api/generated/models/phrase-context-branches-response';
import { PhraseContextGroupsResponse } from '../../../../core/api/generated/models/phrase-context-groups-response';
import { PhraseContextOccurrencesResponse } from '../../../../core/api/generated/models/phrase-context-occurrences-response';
import { PhraseContextApi } from '../data-access/phrase-context.api';
import { PHRASE_CONTEXT_PAGE_SIZE, PhraseContextUrlState } from '../models/phrase-context.models';
import { PhraseRequestFailure, phraseEnvelopeFailure } from './phrase-request-failure';
import { loadPhraseContextGroupPages } from './phrase-context-page-loader';

interface PhraseContextLoadFailure {
  readonly kind: 'failure';
  readonly failure: PhraseRequestFailure;
}

interface PhraseContextWorkspaceSuccess {
  readonly kind: 'workspace';
  readonly activeBuildId: string;
  readonly branches: PhraseContextBranchesResponse;
  readonly groupPages: readonly PhraseContextGroupsResponse[];
}

interface PhraseContextBranchesSuccess {
  readonly kind: 'branches';
  readonly activeBuildId: string;
  readonly branches: PhraseContextBranchesResponse;
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
        PHRASE_CONTEXT_PAGE_SIZE,
      ),
      this.api.getGroups(
        route.resolution!,
        route.before,
        route.after,
        null,
        PHRASE_CONTEXT_PAGE_SIZE,
      ),
    ]).pipe(
      switchMap(([branches, groups]) => {
        if (!branches.isSuccess || !branches.data) {
          return of(failureResult(branches.errors, branches.message));
        }
        if (!groups.isSuccess || !groups.data) {
          return of(failureResult(groups.errors, groups.message));
        }
        if (!sameBuild(branches.data.activeBuildId, groups.data.activeBuildId)) {
          return of(invalidResult('استجابات السياق لا تنتمي إلى جيل فهرس واحد.'));
        }
        const firstGroupPage = groups.data;
        return loadPhraseContextGroupPages(
          this.api,
          route,
          firstGroupPage.nextCursor,
          route.contextsPage - 1,
        ).pipe(
          toArray(),
          map((additionalResponses): PhraseContextLoadResult => {
            const failed = additionalResponses.find(
              (response) => !response.isSuccess || !response.data,
            );
            if (failed) {
              return failureResult(failed.errors, failed.message);
            }
            const additionalPages = additionalResponses.map((response) => response.data!);
            const wrongBuild = additionalPages.some(
              (page) => !sameBuild(branches.data!.activeBuildId, page.activeBuildId),
            );
            return wrongBuild
              ? invalidResult('صفحات السياق لا تنتمي إلى جيل فهرس واحد.')
              : {
                  kind: 'workspace',
                  activeBuildId: branches.data!.activeBuildId,
                  branches: branches.data!,
                  groupPages: [firstGroupPage, ...additionalPages],
                };
          }),
        );
      }),
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
        PHRASE_CONTEXT_PAGE_SIZE,
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
        PHRASE_CONTEXT_PAGE_SIZE,
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
    return this.api.getOccurrences(contextRef, cursor, PHRASE_CONTEXT_PAGE_SIZE).pipe(
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
