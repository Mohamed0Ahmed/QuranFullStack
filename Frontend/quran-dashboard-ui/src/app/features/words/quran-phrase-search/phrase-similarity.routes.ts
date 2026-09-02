import { Routes } from '@angular/router';

import { PhraseResolutionApi } from './data-access/phrase-resolution.api';
import { PhraseSimilarityApi } from './data-access/phrase-similarity.api';
import { PhraseActionRequestGate } from './state/phrase-action-request-gate';
import { PhraseLongStateSessionStore } from './state/phrase-long-state-session.store';
import { PhraseNoticeStore } from './state/phrase-notice.store';
import { PhraseRouteNavigationCoordinator } from './state/phrase-route-navigation.coordinator';
import { PhraseSimilarityQueryCoordinator } from './state/phrase-similarity-query.coordinator';
import { PhraseLinkingAyahSelectionStore } from './state/phrase-linking-ayah-selection.store';
import { PhraseSimilarityLinkingCoordinator } from './state/phrase-similarity-linking.coordinator';
import { PhraseSimilarityResolutionStore } from './state/phrase-similarity-resolution.store';
import { PhraseSimilarityResultStore } from './state/phrase-similarity-result.store';
import { PhraseSimilarityResultsLoader } from './state/phrase-similarity-results.loader';
import { PhraseSimilarityFacade } from './state/phrase-similarity.facade';

export const PHRASE_SIMILARITY_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/phrase-similarity-page/phrase-similarity-page.component').then(
        (m) => m.PhraseSimilarityPageComponent,
      ),
    providers: [
      PhraseResolutionApi,
      PhraseSimilarityApi,
      PhraseSimilarityFacade,
      PhraseLinkingAyahSelectionStore,
      PhraseSimilarityLinkingCoordinator,
      PhraseSimilarityQueryCoordinator,
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
];
