import { Routes } from '@angular/router';

import { PhraseRepetitionsApi } from './data-access/phrase-repetitions.api';
import { PhraseRepetitionsFacade } from './state/phrase-repetitions.facade';

export const PHRASE_REPETITIONS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/phrase-repetitions-page/phrase-repetitions-page.component').then(
        (m) => m.PhraseRepetitionsPageComponent,
      ),
    providers: [PhraseRepetitionsApi, PhraseRepetitionsFacade],
    title: 'تكرارات العبارات القرآنية',
  },
];
