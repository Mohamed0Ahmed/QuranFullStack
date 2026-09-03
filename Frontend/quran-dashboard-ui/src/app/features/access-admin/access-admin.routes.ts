import { Routes } from '@angular/router';

import { accessAdminUnsavedChangesGuard } from './access-admin-unsaved-changes.guard';
import { AccessAccountWorkflowSession } from './state/access-account-workflow.session';

export const ACCESS_ADMIN_ROUTES: Routes = [
  {
    path: '',
    title: 'إدارة الوصول',
    canDeactivate: [accessAdminUnsavedChangesGuard],
    providers: [AccessAccountWorkflowSession],
    loadComponent: () =>
      import('./pages/access-admin-page/access-admin-page.component').then(
        (m) => m.AccessAdminPageComponent,
      ),
  },
];
