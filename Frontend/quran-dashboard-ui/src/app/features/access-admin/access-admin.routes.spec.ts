import { describe, expect, it } from 'vitest';

import { accessAdminUnsavedChangesGuard } from './access-admin-unsaved-changes.guard';
import { ACCESS_ADMIN_ROUTES } from './access-admin.routes';

describe('access-admin routes', () => {
  it('keeps the Owner-only feature page as the feature root without adding a navigation route', () => {
    expect(ACCESS_ADMIN_ROUTES).toHaveLength(1);
    expect(ACCESS_ADMIN_ROUTES[0]?.path).toBe('');
    expect(ACCESS_ADMIN_ROUTES[0]?.loadComponent).toBeTypeOf('function');
  });

  it('protects navigation away from an unsaved permission draft', () => {
    expect(ACCESS_ADMIN_ROUTES[0]?.canDeactivate).toEqual([accessAdminUnsavedChangesGuard]);
  });

  it('puts no identifier in a visible URL: no route carries a parameter segment', () => {
    const paths = ACCESS_ADMIN_ROUTES.flatMap(function collect(route): string[] {
      return [route.path ?? '', ...(route.children ?? []).flatMap(collect)];
    });

    expect(paths).toEqual(['']);
    expect(paths.some((path) => path.includes(':'))).toBe(false);
  });
});
