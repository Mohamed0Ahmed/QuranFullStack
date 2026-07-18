import { Routes } from '@angular/router';
import { NAV_ITEMS } from './core/navigation/nav-items';
import { CALLBACK_PATH, navLabel } from './core/navigation/route-paths';
import { authGuard } from './core/auth/auth.guard';

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
    // One guarded parent covers the whole `/dashboard` subtree (Feature 033, G1). URLs are
    // unchanged: parent `dashboard` + child `''`/`mushaf`/`words` still resolve to
    // `/dashboard`, `/dashboard/mushaf`, `/dashboard/words`.
    path: 'dashboard',
    canActivate: [authGuard],
    children: [
      {
        // The dashboard home intentionally sets no title → brand-only tab (AppTitleStrategy
        // renders the brand alone when no route in the tree carries a title). The mushaf/words
        // children set their own titles below (words children override further; see words.routes.ts).
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
    // Public OIDC landing route (Feature 033) — no guard. Must sit before the `**` wildcard,
    // which would otherwise swallow it.
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
