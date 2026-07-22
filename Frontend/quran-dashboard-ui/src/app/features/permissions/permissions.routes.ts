import { Routes } from '@angular/router';

import { permissionGuard } from '../../core/auth/permission.guard';

// permissionGuard is non-authoritative client gating; the backend SystemOwner policy is the authority.
export const PERMISSIONS_ROUTES: Routes = [
  {
    path: '',
    canActivate: [permissionGuard('permission.administer')],
    title: 'إدارة الصلاحيات',
    loadComponent: () =>
      import('./pages/permissions-page/permissions-page.component').then(
        (m) => m.PermissionsPageComponent,
      ),
  },
];
