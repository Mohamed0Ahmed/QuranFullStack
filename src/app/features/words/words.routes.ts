import { Routes } from '@angular/router';

import { WORDS_ROOTS_SEGMENT, WORDS_UNIQUE_MODE_SEGMENT } from '../../core/navigation/route-paths';

const loadWordsHubPage = () =>
  import('./pages/words-hub-page/words-hub-page.component').then(
    (m) => m.WordsHubPageComponent,
  );

const loadUniqueWordsPage = () =>
  import('./pages/unique-words-page/unique-words-page.component').then(
    (m) => m.UniqueWordsPageComponent,
  );

const loadRootsExplorerPage = () =>
  import('./pages/roots-explorer-page/roots-explorer-page.component').then(
    (m) => m.RootsExplorerPageComponent,
  );

export const WORDS_HUB_ROUTE = {
  path: '',
  loadComponent: loadWordsHubPage,
} as const;

export const WORDS_UNIQUE_MODE_ROUTE = {
  path: WORDS_UNIQUE_MODE_SEGMENT,
  loadComponent: loadUniqueWordsPage,
} as const;

export const WORDS_ROOTS_ROUTE = {
  path: WORDS_ROOTS_SEGMENT,
  loadComponent: loadRootsExplorerPage,
} as const;

export const WORDS_ROUTES: Routes = [
  WORDS_HUB_ROUTE,
  {
    path: 'unique',
    redirectTo: 'unique/tashkeel',
    pathMatch: 'full',
  },
  WORDS_UNIQUE_MODE_ROUTE,
  WORDS_ROOTS_ROUTE,
];
