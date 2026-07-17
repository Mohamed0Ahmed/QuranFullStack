import { Routes } from '@angular/router';
import { NAV_ITEMS } from './core/navigation/nav-items';
import { navLabel } from './core/navigation/route-paths';

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
    loadComponent: () =>
      import('./features/dashboard/pages/dashboard-home/dashboard-home.component').then(
        (m) => m.DashboardHomeComponent,
      ),
  },
  {
    // `dashboard` (home) intentionally sets no title → brand-only tab. The mushaf/words
    // children inherit these parent titles unless a child overrides (see words.routes.ts).
    path: 'dashboard/mushaf',
    title: navLabel('mushaf'),
    loadChildren: () =>
      import('./features/mushaf/mushaf.routes').then((m) => m.MUSHAF_ROUTES),
  },
  {
    path: 'dashboard/words',
    title: navLabel('words'),
    loadChildren: () =>
      import('./features/words/words.routes').then((m) => m.WORDS_ROUTES),
  },
  ...placeholderRoutes,
  {
    path: '**',
    redirectTo: 'dashboard',
  },
];
