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
    // The nav carries this entry as a child of «الأبواب» in the navbar's menu model
    // (`nav-menu.ts`); `NAV_ITEMS` still carries no `templates` key, so the title remains its
    // own page title rather than a `navLabel`: `navLabel` throws on a key `NAV_ITEMS` does not
    // carry.
    path: 'templates',
    loadComponent: loadAbwabTemplatesPage,
    title: ABWAB_LABELS.templatesPageTitle,
  },
];
