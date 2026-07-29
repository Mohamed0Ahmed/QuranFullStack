import { Routes } from '@angular/router';

import { navLabel } from '../../core/navigation/route-paths';

const loadAbwabPage = () =>
  import('./pages/abwab-page/abwab-page.component').then((m) => m.AbwabPageComponent);

export const ABWAB_ROUTES: Routes = [
  {
    path: '',
    loadComponent: loadAbwabPage,
    title: navLabel('abwab'),
  },
];
