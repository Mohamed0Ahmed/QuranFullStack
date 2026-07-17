import { Routes } from '@angular/router';

import {
  WORDS_LEMMAS_SEGMENT,
  WORDS_ROOTS_SEGMENT,
  WORDS_STEMS_SEGMENT,
  WORDS_TYPES_SEGMENT,
  WORDS_UNIQUE_MODE_SEGMENT,
} from '../../core/navigation/route-paths';
// Tab titles reuse each explorer's own page-title label (its <h1>) so the two never drift.
import { LEMMAS_PAGE_TITLE } from './models/lemmas.labels';
import { ROOTS_PAGE_TITLE } from './models/roots.labels';
import { STEMS_PAGE_TITLE } from './models/stems.labels';
import { ACTIVE_HUB_SECTION } from './models/unique-words.labels';
import { WORD_TYPES_PAGE_TITLE } from './models/word-types.labels';

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

const loadWordTypesExplorerPage = () =>
  import('./pages/word-types-explorer-page/word-types-explorer-page.component').then(
    (m) => m.WordTypesExplorerPageComponent,
  );

export const WORDS_HUB_ROUTE = {
  path: '',
  loadComponent: loadWordsHubPage,
} as const;

export const WORDS_UNIQUE_MODE_ROUTE = {
  path: WORDS_UNIQUE_MODE_SEGMENT,
  loadComponent: loadUniqueWordsPage,
  title: ACTIVE_HUB_SECTION.labelAr,
} as const;

export const WORDS_ROOTS_ROUTE = {
  path: WORDS_ROOTS_SEGMENT,
  loadComponent: loadRootsExplorerPage,
  title: ROOTS_PAGE_TITLE,
} as const;

export const WORDS_LEMMAS_ROUTE = {
  path: WORDS_LEMMAS_SEGMENT,
  loadComponent: loadLemmasExplorerPage,
  title: LEMMAS_PAGE_TITLE,
} as const;

export const WORDS_STEMS_ROUTE = {
  path: WORDS_STEMS_SEGMENT,
  loadComponent: loadStemsExplorerPage,
  title: STEMS_PAGE_TITLE,
} as const;

export const WORDS_TYPES_ROUTE = {
  path: WORDS_TYPES_SEGMENT,
  loadComponent: loadWordTypesExplorerPage,
  title: WORD_TYPES_PAGE_TITLE,
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
  WORDS_TYPES_ROUTE,
];
