import { Routes } from '@angular/router';

import {
  WORDS_PHRASES_CONTEXT_SEGMENT,
  WORDS_PHRASES_REPETITIONS_SEGMENT,
  WORDS_PHRASES_SIMILARITY_SEGMENT,
} from '../../../core/navigation/route-paths';
import { PhraseSearchBuildAuthorityApi } from './data-access/phrase-search-build-authority.api';
import { PhraseSearchCache } from './state/phrase-search-cache';

const loadShell = () =>
  import('./pages/quran-phrase-search-shell/quran-phrase-search-shell.component').then(
    (m) => m.QuranPhraseSearchShellComponent,
  );

export const QURAN_PHRASE_SEARCH_ROUTES: Routes = [
  {
    path: '',
    loadComponent: loadShell,
    providers: [PhraseSearchBuildAuthorityApi, PhraseSearchCache],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: WORDS_PHRASES_REPETITIONS_SEGMENT,
      },
      {
        path: WORDS_PHRASES_REPETITIONS_SEGMENT,
        loadChildren: () =>
          import('./phrase-repetitions.routes').then(
            (m) => m.PHRASE_REPETITIONS_ROUTES,
          ),
        data: { scrollStateKey: 'words.viewport.phrase-search.repetitions' },
      },
      {
        path: WORDS_PHRASES_CONTEXT_SEGMENT,
        loadChildren: () =>
          import('./phrase-context.routes').then((m) => m.PHRASE_CONTEXT_ROUTES),
        data: { scrollStateKey: 'words.viewport.phrase-search.context' },
      },
      {
        path: WORDS_PHRASES_SIMILARITY_SEGMENT,
        loadChildren: () =>
          import('./phrase-similarity.routes').then(
            (m) => m.PHRASE_SIMILARITY_ROUTES,
          ),
        data: { scrollStateKey: 'words.viewport.phrase-search.similarity' },
      },
    ],
  },
];
