import { Routes } from '@angular/router';

const loadWordsHubPage = () =>
  import('./pages/words-hub-page/words-hub-page.component').then(
    (m) => m.WordsHubPageComponent,
  );

const loadUniqueWordsPage = () =>
  import('./pages/unique-words-page/unique-words-page.component').then(
    (m) => m.UniqueWordsPageComponent,
  );

/**
 * The hub route renders at the empty child path, so mounting `WORDS_ROUTES`
 * under `/dashboard/words` lands the hub page there.
 */
export const WORDS_HUB_ROUTE = {
  path: '',
  loadComponent: loadWordsHubPage,
} as const;

/**
 * Explorer route. The active mode (`tashkeel` / `simple`) is a stable route
 * key carried by the `:mode` path segment, which the page and facade read via
 * `paramMap.get('mode')`. `tashkeel` and `simple` are route keys, not
 * translated labels; an unknown mode is normalized to `tashkeel` by the facade.
 */
export const WORDS_UNIQUE_MODE_ROUTE = {
  path: 'unique/:mode',
  loadComponent: loadUniqueWordsPage,
} as const;

/**
 * Words feature routes, lazy-loaded at `/dashboard/words` (T021).
 *
 * `unique` redirects to the default `tashkeel` mode. The mode lives in the
 * `:mode` path segment; list state (`search`/`sort`/`page`) lives in query
 * params and is read by the facade (T043). Modal query params
 * (`word`/`view`/`ap`) are added in US4.
 */
export const WORDS_ROUTES: Routes = [
  WORDS_HUB_ROUTE,
  {
    path: 'unique',
    redirectTo: 'unique/tashkeel',
    pathMatch: 'full',
  },
  WORDS_UNIQUE_MODE_ROUTE,
];
