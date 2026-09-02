import { Routes } from '@angular/router';

import { PhraseContextApi } from './data-access/phrase-context.api';
import { PhraseResolutionApi } from './data-access/phrase-resolution.api';
import { PhraseActionRequestGate } from './state/phrase-action-request-gate';
import { PhraseContextActionCoordinator } from './state/phrase-context-action.coordinator';
import { PhraseLinkingAyahSelectionStore } from './state/phrase-linking-ayah-selection.store';
import { PhraseContextLinkingCoordinator } from './state/phrase-context-linking.coordinator';
import { PhraseContextQueryCoordinator } from './state/phrase-context-query.coordinator';
import { PhraseContextRequestStatusStore } from './state/phrase-context-request-status.store';
import { PhraseContextResolutionStore } from './state/phrase-context-resolution.store';
import { PhraseContextSelectionStore } from './state/phrase-context-selection.store';
import { PhraseContextWorkspaceLoader } from './state/phrase-context-workspace.loader';
import { PhraseContextWorkspaceRequestFence } from './state/phrase-context-workspace-request-fence';
import { PhraseContextFacade } from './state/phrase-context.facade';
import { PhraseLongStateSessionStore } from './state/phrase-long-state-session.store';
import { PhraseNoticeStore } from './state/phrase-notice.store';
import { PhraseRouteNavigationCoordinator } from './state/phrase-route-navigation.coordinator';

export const PHRASE_CONTEXT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/phrase-context-page/phrase-context-page.component').then(
        (m) => m.PhraseContextPageComponent,
      ),
    providers: [
      PhraseContextApi,
      PhraseResolutionApi,
      PhraseContextFacade,
      PhraseContextActionCoordinator,
      PhraseLinkingAyahSelectionStore,
      PhraseContextLinkingCoordinator,
      PhraseContextQueryCoordinator,
      PhraseContextSelectionStore,
      PhraseContextRequestStatusStore,
      PhraseContextResolutionStore,
      PhraseContextWorkspaceLoader,
      PhraseContextWorkspaceRequestFence,
      PhraseActionRequestGate,
      PhraseLongStateSessionStore,
      PhraseNoticeStore,
      PhraseRouteNavigationCoordinator,
    ],
    title: 'سياق العبارة القرآنية',
  },
];
