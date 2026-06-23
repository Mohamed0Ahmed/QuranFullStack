import { Routes } from '@angular/router';

const loadWordsHubPage = () =>
  import('./pages/words-hub-page/words-hub-page.component').then(
    (m) => m.WordsHubPageComponent,
  );

const loadUniqueWordsPage = () =>
  import('./pages/unique-words-page/unique-words-page.component').then(
    (m) => m.UniqueWordsPageComponent,
  );

export const WORDS_HUB_ROUTE = {
  path: '',
  loadComponent: loadWordsHubPage,
} as const;

export const WORDS_UNIQUE_MODE_ROUTE = {
  path: 'unique/:mode',
  loadComponent: loadUniqueWordsPage,
} as const;

export const WORDS_ROUTES: Routes = [
  WORDS_HUB_ROUTE,
  {
    path: 'unique',
    redirectTo: 'unique/tashkeel',
    pathMatch: 'full',
  },
  WORDS_UNIQUE_MODE_ROUTE,
];
