import { Routes } from '@angular/router';

export const ACCESS_ADMIN_ROUTES: Routes = [
  {
    path: '',
    title: 'إدارة الوصول',
    loadComponent: () =>
      import('./pages/access-admin-page/access-admin-page.component').then(
        (m) => m.AccessAdminPageComponent,
      ),
  },
];
