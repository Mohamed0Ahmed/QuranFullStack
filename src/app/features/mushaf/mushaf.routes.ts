import { Routes } from '@angular/router';

const loadMushafReaderPage = () =>
  import('./pages/mushaf-reader-page/mushaf-reader-page.component').then(
    (m) => m.MushafReaderPageComponent,
  );

/**
 * Mushaf reader feature routes, lazy-loaded at `/dashboard/mushaf`.
 */
export const MUSHAF_ROUTES: Routes = [
  {
    path: '',
    loadComponent: loadMushafReaderPage,
  },
];
