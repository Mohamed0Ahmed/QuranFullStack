import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { AssociationOptionsResult } from '../state/words-association-filters';
import { RootsApi } from './roots.api';
import { LemmasApi } from './lemmas.api';
import { WordTypesApi } from './word-types.api';
import { RootsCache } from '../state/roots-cache';
import { LemmasCache } from '../state/lemmas-cache';
import { WordTypesCache, WordTypesCacheKeys } from '../state/word-types-cache';

@Injectable({ providedIn: 'root' })
export class WordsAssociationOptionsService {
  private readonly rootsApi = inject(RootsApi);
  private readonly lemmasApi = inject(LemmasApi);
  private readonly wordTypesApi = inject(WordTypesApi);
  private readonly rootsCache = inject(RootsCache);
  private readonly lemmasCache = inject(LemmasCache);
  private readonly wordTypesCache = inject(WordTypesCache);

  private static readonly PickerPageSize = 30;

  searchRoots(term: string): Observable<AssociationOptionsResult> {
    const key = `roots:picker:${term.trim()}`;
    return this.rootsCache
      .getOrLoad(key, () =>
        this.rootsApi.getRootsList(term, 'occurrences', 1, WordsAssociationOptionsService.PickerPageSize),
      )
      .pipe(
        map((response): AssociationOptionsResult =>
          response.isSuccess && response.data
            ? {
                status: 'success',
                options: response.data.items.map((root) => ({ id: root.id, label: root.rootText })),
              }
            : { status: 'error' },
        ),
        catchError(() => of<AssociationOptionsResult>({ status: 'error' })),
      );
  }

  searchLemmas(term: string): Observable<AssociationOptionsResult> {
    const key = `lemmas:picker:${term.trim()}`;
    return this.lemmasCache
      .getOrLoad(key, () =>
        this.lemmasApi.getLemmasList(term, 'occurrences', 1, WordsAssociationOptionsService.PickerPageSize),
      )
      .pipe(
        map((response): AssociationOptionsResult =>
          response.isSuccess && response.data
            ? {
                status: 'success',
                options: response.data.items.map((lemma) => ({ id: lemma.id, label: lemma.lemmaText })),
              }
            : { status: 'error' },
        ),
        catchError(() => of<AssociationOptionsResult>({ status: 'error' })),
      );
  }

  wordTypeOptions(): Observable<AssociationOptionsResult> {
    return this.wordTypesCache
      .getOrLoad(WordTypesCacheKeys.tree, () => this.wordTypesApi.getTree())
      .pipe(
        map((response): AssociationOptionsResult => {
          if (!response.isSuccess || !response.data) {
            return { status: 'error' };
          }
          const options = response.data.mainTypes
            .filter((node) => node.code === 'noun' || node.code === 'particle')
            .flatMap((node) =>
              node.children.map((child) => ({ id: child.code, label: child.label.ar })),
            );
          return { status: 'success', options };
        }),
        catchError(() => of<AssociationOptionsResult>({ status: 'error' })),
      );
  }
}
