import { Routes } from '@angular/router';

import {
  WORDS_PHRASES_CONTEXT_SEGMENT,
  WORDS_PHRASES_REPETITIONS_SEGMENT,
  WORDS_PHRASES_SIMILARITY_SEGMENT,
} from '../../../core/navigation/route-paths';
import { PhraseRepetitionsFacade } from './state/phrase-repetitions.facade';

const loadShell = () =>
  import('./pages/quran-phrase-search-shell/quran-phrase-search-shell.component').then(
    (m) => m.QuranPhraseSearchShellComponent,
  );

const loadRepetitionsPage = () =>
  import('./pages/phrase-repetitions-page/phrase-repetitions-page.component').then(
    (m) => m.PhraseRepetitionsPageComponent,
  );

const loadDeferredPage = () =>
  import('./pages/phrase-search-deferred-page/phrase-search-deferred-page.component').then(
    (m) => m.PhraseSearchDeferredPageComponent,
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
        loadComponent: loadDeferredPage,
        title: 'سياق العبارة القرآنية',
        data: {
          titleAr: 'البحث اليدوي في السياق',
          messageAr: 'ستُستكمل أدوات هذا القسم في المرحلة التالية من مساحة البحث.',
        },
      },
      {
        path: WORDS_PHRASES_SIMILARITY_SEGMENT,
        loadComponent: loadDeferredPage,
        title: 'تشابه العبارات القرآنية',
        data: {
          titleAr: 'المتشابهات الموضعية',
          messageAr: 'ستُستكمل أدوات هذا القسم في المرحلة التالية من مساحة البحث.',
        },
      },
    ],
  },
];
