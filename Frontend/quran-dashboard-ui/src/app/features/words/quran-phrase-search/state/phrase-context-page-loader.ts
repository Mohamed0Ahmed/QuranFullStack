import { Observable, concat, of } from 'rxjs';
import { concatMap } from 'rxjs/operators';

import { PhraseContextGroupsResponseApiResponse } from '../../../../core/api/generated/models/phrase-context-groups-response-api-response';
import { PhraseContextApi } from '../data-access/phrase-context.api';
import { PHRASE_CONTEXT_PAGE_SIZE, PhraseContextUrlState } from '../models/phrase-context.models';

export function loadPhraseContextGroupPages(
  api: PhraseContextApi,
  route: PhraseContextUrlState,
  cursor: string | null,
  remaining: number,
): Observable<PhraseContextGroupsResponseApiResponse> {
  if (!cursor || remaining <= 0 || !route.resolution) {
    return of();
  }
  return api
    .getGroups(
      route.resolution,
      route.before,
      route.after,
      cursor,
      PHRASE_CONTEXT_PAGE_SIZE,
    )
    .pipe(
      concatMap((response) => {
        if (!response.isSuccess || !response.data || remaining === 1) {
          return of(response);
        }
        return concat(
          of(response),
          loadPhraseContextGroupPages(api, route, response.data.nextCursor, remaining - 1),
        );
      }),
    );
}
