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
    path: 'templates',
    loadComponent: loadAbwabTemplatesPage,
    title: ABWAB_LABELS.templatesPageTitle,
  },
];
