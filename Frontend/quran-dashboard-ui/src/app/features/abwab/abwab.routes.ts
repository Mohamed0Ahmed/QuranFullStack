import { Routes } from '@angular/router';

import { navLabel } from '../../core/navigation/route-paths';
import { ABWAB_LABELS } from './models/abwab.labels';

const loadAbwabPage = () =>
  import('./pages/abwab-page/abwab-page.component').then((m) => m.AbwabPageComponent);

const loadAbwabTemplatesPage = () =>
  import('./pages/abwab-templates-page/abwab-templates-page.component').then((m) => m.AbwabTemplatesPageComponent);

export const ABWAB_ROUTES: Routes = [
  {
    path: '',
    loadComponent: loadAbwabPage,
    title: navLabel('abwab'),
  },
  {
    // The workshop is reached from the doors page header, not the sidebar, so its title is its
    // own page title rather than a `navLabel`: `navLabel` throws on a key `NAV_ITEMS` does not
    // carry, and adding one would put an item in the nav nobody asked for.
    path: 'templates',
    loadComponent: loadAbwabTemplatesPage,
    title: ABWAB_LABELS.templatesPageTitle,
  },
];
