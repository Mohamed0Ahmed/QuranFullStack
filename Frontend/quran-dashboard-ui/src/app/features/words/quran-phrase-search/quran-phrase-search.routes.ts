import { Routes } from '@angular/router';

import {
  WORDS_PHRASES_CONTEXT_SEGMENT,
  WORDS_PHRASES_REPETITIONS_SEGMENT,
  WORDS_PHRASES_SIMILARITY_SEGMENT,
} from '../../../core/navigation/route-paths';
import { PhraseRepetitionsFacade } from './state/phrase-repetitions.facade';
import { PhraseContextFacade } from './state/phrase-context.facade';
import { PhraseContextSelectionStore } from './state/phrase-context-selection.store';
import { PhraseContextRequestStatusStore } from './state/phrase-context-request-status.store';
import { PhraseContextResolutionStore } from './state/phrase-context-resolution.store';
import { PhraseContextWorkspaceLoader } from './state/phrase-context-workspace.loader';
import { PhraseLongStateSessionStore } from './state/phrase-long-state-session.store';
import { PhraseRouteNavigationCoordinator } from './state/phrase-route-navigation.coordinator';
import { PhraseSimilarityFacade } from './state/phrase-similarity.facade';
import { PhraseSimilarityResultsLoader } from './state/phrase-similarity-results.loader';
import { PhraseActionRequestGate } from './state/phrase-action-request-gate';
import { PhraseNoticeStore } from './state/phrase-notice.store';
import { PhraseContextActionCoordinator } from './state/phrase-context-action.coordinator';
import { PhraseSimilarityResultStore } from './state/phrase-similarity-result.store';
import { PhraseSimilarityResolutionStore } from './state/phrase-similarity-resolution.store';

const loadShell = () =>
  import('./pages/quran-phrase-search-shell/quran-phrase-search-shell.component').then(
    (m) => m.QuranPhraseSearchShellComponent,
  );

const loadRepetitionsPage = () =>
  import('./pages/phrase-repetitions-page/phrase-repetitions-page.component').then(
    (m) => m.PhraseRepetitionsPageComponent,
  );

const loadContextPage = () =>
  import('./pages/phrase-context-page/phrase-context-page.component').then(
    (m) => m.PhraseContextPageComponent,
  );

const loadSimilarityPage = () =>
  import('./pages/phrase-similarity-page/phrase-similarity-page.component').then(
    (m) => m.PhraseSimilarityPageComponent,
  );

export const QURAN_PHRASE_SEARCH_ROUTES: Routes = [
  {
    path: '',
    loadComponent: loadShell,
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: WORDS_PHRASES_REPETITIONS_SEGMENT,
      },
      {
        path: WORDS_PHRASES_REPETITIONS_SEGMENT,
        loadComponent: loadRepetitionsPage,
        providers: [PhraseRepetitionsFacade],
        title: 'تكرارات العبارات القرآنية',
      },
      {
        path: WORDS_PHRASES_CONTEXT_SEGMENT,
        loadComponent: loadContextPage,
        providers: [
          PhraseContextFacade,
          PhraseContextActionCoordinator,
          PhraseContextSelectionStore,
          PhraseContextRequestStatusStore,
          PhraseContextResolutionStore,
          PhraseContextWorkspaceLoader,
          PhraseActionRequestGate,
          PhraseLongStateSessionStore,
          PhraseNoticeStore,
          PhraseRouteNavigationCoordinator,
        ],
        title: 'سياق العبارة القرآنية',
      },
      {
        path: WORDS_PHRASES_SIMILARITY_SEGMENT,
        loadComponent: loadSimilarityPage,
        providers: [
          PhraseSimilarityFacade,
          PhraseSimilarityResultStore,
          PhraseSimilarityResolutionStore,
          PhraseSimilarityResultsLoader,
          PhraseActionRequestGate,
          PhraseLongStateSessionStore,
          PhraseNoticeStore,
          PhraseRouteNavigationCoordinator,
        ],
        title: 'تشابه العبارات القرآنية',
      },
    ],
  },
];
