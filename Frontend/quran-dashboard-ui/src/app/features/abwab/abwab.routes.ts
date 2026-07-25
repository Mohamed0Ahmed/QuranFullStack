import { Routes } from '@angular/router';

// The app runs a public-browse posture: only the owner-only permission-administration route is
// route-guarded (app.routes.spec.ts). Composite-read visibility here is cosmetic, in-page gating —
// AbwabPermissions.canViewTree() (category.view + section.view) — the backend DTO projection
// (tree-read-contract.md) is the real authority; this route stays unguarded like mushaf/words.
export const ABWAB_ROUTES: Routes = [
  {
    path: '',
    title: 'الأبواب',
    loadComponent: () => import('./tree/abwab-tree-page.component').then((m) => m.AbwabTreePageComponent),
  },
  {
    path: 'relationships/:categoryId',
    title: 'علاقات الباب',
    loadComponent: () =>
      import('./relationships/abwab-relationships-page.component').then((m) => m.AbwabRelationshipsPageComponent),
  },
];
