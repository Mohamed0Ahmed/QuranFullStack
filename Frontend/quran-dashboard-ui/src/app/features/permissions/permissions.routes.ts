import { Routes } from '@angular/router';

import { permissionGuard } from '../../core/auth/permission.guard';

// Owner-only permission-administration feature (US5). The route is gated client-side by permissionGuard —
// non-authoritative hiding; the backend SystemOwner policy is the authority.
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
