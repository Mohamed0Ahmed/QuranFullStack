import { Injectable, Signal, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';

import {
  DEFAULT_WORD_TYPE,
  DEFAULT_WORD_TYPE_CASE,
  DEFAULT_WORD_TYPE_SORT,
  DEFAULT_WORD_TYPE_TENSE,
  DEFAULT_WORD_TYPE_VOICE,
  DEFAULT_WORD_TYPES_DETAIL_PAGE,
  DEFAULT_WORD_TYPES_DETAIL_VIEW,
  DEFAULT_WORD_TYPES_PAGE,
  ParsedWordTypesQuery,
  WordTypeRowIdentity,
  WordTypesListState,
} from '../models/word-types.models';
import { parseWordTypesQueryParams } from './word-types-url-sync';

const DEFAULT_QUERY: ParsedWordTypesQuery = {
  type: DEFAULT_WORD_TYPE,
  childCode: null,
  case: DEFAULT_WORD_TYPE_CASE,
  tense: DEFAULT_WORD_TYPE_TENSE,
  voice: DEFAULT_WORD_TYPE_VOICE,
  sort: DEFAULT_WORD_TYPE_SORT,
  page: DEFAULT_WORD_TYPES_PAGE,
  word: null,
  tashkeelWordId: 0,
  contextCode: '',
  view: DEFAULT_WORD_TYPES_DETAIL_VIEW,
  detailPage: DEFAULT_WORD_TYPES_DETAIL_PAGE,
  location: null,
  column: null,
};

@Injectable({ providedIn: 'root' })
export class WordTypesExplorerFacade {
  private routeSub?: Subscription;
  private readonly state = signal<WordTypesListState>({
    status: 'idle',
    tree: null,
    rows: null,
    query: DEFAULT_QUERY,
    errorMessage: '',
  });

  readonly listState: Signal<WordTypesListState> = this.state.asReadonly();

  bindToRoute(route: ActivatedRoute): void {
    this.unbindFromRoute();
    this.routeSub = route.queryParamMap.subscribe((params) => {
      const query = parseWordTypesQueryParams(params);
      this.state.update((current) => ({ ...current, query }));
    });
  }

  unbindFromRoute(): void {
    this.routeSub?.unsubscribe();
    this.routeSub = undefined;
  }

  selectRow(row: WordTypeRowIdentity | null): void {
    this.state.update((current) => ({
      ...current,
      query: {
        ...current.query,
        word: row?.tashkeelWordId ?? null,
        tashkeelWordId: row?.tashkeelWordId ?? 0,
        contextCode: row?.contextCode ?? '',
      },
    }));
  }
}
