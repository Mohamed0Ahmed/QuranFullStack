import { Routes } from '@angular/router';

import { SHELL_LAYOUT_PAGE_SCROLL } from '../../core/layout/shell-layout.model';

const loadMushafReaderPage = () =>
  import('./pages/mushaf-reader-page/mushaf-reader-page.component').then(
    (m) => m.MushafReaderPageComponent,
  );

export const MUSHAF_ROUTES: Routes = [
  {
    path: '',
    loadComponent: loadMushafReaderPage,
    data: { shellLayout: SHELL_LAYOUT_PAGE_SCROLL },
  },
];
