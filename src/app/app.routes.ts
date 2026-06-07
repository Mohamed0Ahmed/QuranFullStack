import { Routes } from '@angular/router';
import { NAV_ITEMS } from './core/navigation/nav-items';

const loadPlaceholderPage = () =>
  import('./shared/ui/placeholder-page/placeholder-page.component').then(
    (m) => m.PlaceholderPageComponent,
  );

const placeholderRoutes: Routes = NAV_ITEMS.filter((item) => item.key !== 'dashboard').map(
  (item) => ({
    path: item.route.replace(/^\//, ''),
    loadComponent: loadPlaceholderPage,
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
  ...placeholderRoutes,
  {
    path: '**',
    redirectTo: 'dashboard',
  },
];
