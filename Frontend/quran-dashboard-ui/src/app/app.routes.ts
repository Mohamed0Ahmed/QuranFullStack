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
    // Public-browse by default (Feature 033, Phase 2, §G1): intentionally no guard here. A
    // reusable `roleGuard` exists (core/auth/role.guard.ts) but is attached to nothing until the
    // first admin feature. See core/README.md for the route-posture contract.
    path: 'dashboard',
    children: [
      {
        // No title here is intentional → brand-only tab (AppTitleStrategy shows the brand alone
        // when no route in the tree carries a title).
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
    ],
  },
  {
    // Must sit before the `**` wildcard, which would otherwise swallow this OIDC landing route.
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
