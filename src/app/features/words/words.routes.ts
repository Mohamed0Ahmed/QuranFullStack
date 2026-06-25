import { Routes } from '@angular/router';

import {
  WORDS_LEMMAS_SEGMENT,
  WORDS_ROOTS_SEGMENT,
  WORDS_STEMS_SEGMENT,
  WORDS_UNIQUE_MODE_SEGMENT,
} from '../../core/navigation/route-paths';

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

const loadLemmasExplorerPage = () =>
  import('./pages/lemmas-explorer-page/lemmas-explorer-page.component').then(
    (m) => m.LemmasExplorerPageComponent,
  );

const loadStemsExplorerPage = () =>
  import('./pages/stems-explorer-page/stems-explorer-page.component').then(
    (m) => m.StemsExplorerPageComponent,
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

export const WORDS_LEMMAS_ROUTE = {
  path: WORDS_LEMMAS_SEGMENT,
  loadComponent: loadLemmasExplorerPage,
} as const;

export const WORDS_STEMS_ROUTE = {
  path: WORDS_STEMS_SEGMENT,
  loadComponent: loadStemsExplorerPage,
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
  WORDS_LEMMAS_ROUTE,
  WORDS_STEMS_ROUTE,
];
