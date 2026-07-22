import { Routes } from '@angular/router';
import { NAV_ITEMS } from './core/navigation/nav-items';
import { CALLBACK_PATH, navLabel } from './core/navigation/route-paths';

const loadPlaceholderPage = () =>
  import('./shared/ui/placeholder-page/placeholder-page.component').then(
    (m) => m.PlaceholderPageComponent,
  );

const placeholderRoutes: Routes = NAV_ITEMS.filter(
  (item) => item.key !== 'dashboard' && item.key !== 'mushaf' && item.key !== 'words',
).map(
  (item) => ({
    path: item.route.replace(/^\//, ''),
    loadComponent: loadPlaceholderPage,
    title: item.labelAr,
    data: { titleAr: item.labelAr },
  }),
);

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard',
  },
  {
    path: 'dashboard',
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () =>
          import('./features/dashboard/pages/dashboard-home/dashboard-home.component').then(
            (m) => m.DashboardHomeComponent,
          ),
      },
      {
        path: 'mushaf',
        title: navLabel('mushaf'),
        loadChildren: () =>
          import('./features/mushaf/mushaf.routes').then((m) => m.MUSHAF_ROUTES),
      },
      {
        path: 'words',
        title: navLabel('words'),
        loadChildren: () =>
          import('./features/words/words.routes').then((m) => m.WORDS_ROUTES),
      },
      {
        path: 'permissions',
        loadChildren: () =>
          import('./features/permissions/permissions.routes').then((m) => m.PERMISSIONS_ROUTES),
      },
    ],
  },
  {
    path: CALLBACK_PATH,
    title: 'تسجيل الدخول',
    loadComponent: () =>
      import('./features/auth/pages/auth-callback/auth-callback.component').then(
        (m) => m.AuthCallbackComponent,
      ),
  },
  ...placeholderRoutes,
  {
    path: '**',
    redirectTo: 'dashboard',
  },
];
