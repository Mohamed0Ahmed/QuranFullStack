import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError, map, shareReplay } from 'rxjs/operators';

import { AssociationOption } from '../state/words-association-filters';
import { RootsApi } from './roots.api';
import { LemmasApi } from './lemmas.api';
import { WordTypesApi } from './word-types.api';
import { RootsCache } from '../state/roots-cache';
import { LemmasCache } from '../state/lemmas-cache';

/**
 * Loads association-filter picker options (Feature 026, US7) by REUSING the existing reads — no new
 * endpoint. Root/lemma pickers search the roots/lemmas list apis (cached through the shared explorer
 * caches under a distinct picker namespace); the type select is fed from the word-types tree read,
 * flattening the noun and particle POS-leaf children (the "POS child catalogue"). Verb and muqatta'at
 * are represented non-granularly in the tree (by tense / as a main type) and so are not offered as
 * granular primary-type options here.
 */
@Injectable({ providedIn: 'root' })
export class WordsAssociationOptionsService {
  private readonly rootsApi = inject(RootsApi);
  private readonly lemmasApi = inject(LemmasApi);
  private readonly wordTypesApi = inject(WordTypesApi);
  private readonly rootsCache = inject(RootsCache);
  private readonly lemmasCache = inject(LemmasCache);

  private static readonly PickerPageSize = 30;
  private wordTypeOptions$?: Observable<readonly AssociationOption[]>;

  searchRoots(term: string): Observable<readonly AssociationOption[]> {
    const key = `roots:picker:${term.trim()}`;
    return this.rootsCache
      .getOrLoad(key, () =>
        this.rootsApi.getRootsList(term, 'occurrences', 1, WordsAssociationOptionsService.PickerPageSize),
      )
      .pipe(
        map((response) =>
          response.isSuccess && response.data
            ? response.data.items.map((root) => ({ id: root.id, label: root.rootText }))
            : [],
        ),
        catchError(() => of<readonly AssociationOption[]>([])),
      );
  }

  searchLemmas(term: string): Observable<readonly AssociationOption[]> {
    const key = `lemmas:picker:${term.trim()}`;
    return this.lemmasCache
      .getOrLoad(key, () =>
        this.lemmasApi.getLemmasList(term, 'occurrences', 1, WordsAssociationOptionsService.PickerPageSize),
      )
      .pipe(
        map((response) =>
          response.isSuccess && response.data
            ? response.data.items.map((lemma) => ({ id: lemma.id, label: lemma.lemmaText }))
            : [],
        ),
        catchError(() => of<readonly AssociationOption[]>([])),
      );
  }

  wordTypeOptions(): Observable<readonly AssociationOption[]> {
    this.wordTypeOptions$ ??= this.wordTypesApi.getTree().pipe(
      map((response) => {
        if (!response.isSuccess || !response.data) {
          return [];
        }
        return response.data.mainTypes
          .filter((node) => node.code === 'noun' || node.code === 'particle')
          .flatMap((node) =>
            node.children.map((child) => ({ id: child.code, label: child.label.ar })),
          );
      }),
      catchError(() => of<readonly AssociationOption[]>([])),
      shareReplay(1),
    );
    return this.wordTypeOptions$;
  }
}
