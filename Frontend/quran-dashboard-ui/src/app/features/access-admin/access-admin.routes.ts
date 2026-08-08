import { Routes } from '@angular/router';

import { accessAdminUnsavedChangesGuard } from './access-admin-unsaved-changes.guard';

export const ACCESS_ADMIN_ROUTES: Routes = [
  {
    path: '',
    title: 'إدارة الوصول',
    canDeactivate: [accessAdminUnsavedChangesGuard],
    loadComponent: () =>
      import('./pages/access-admin-page/access-admin-page.component').then(
        (m) => m.AccessAdminPageComponent,
      ),
  },
];
