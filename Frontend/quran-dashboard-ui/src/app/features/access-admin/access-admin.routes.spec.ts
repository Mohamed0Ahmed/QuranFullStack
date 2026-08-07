import { describe, expect, it } from 'vitest';

import { ACCESS_ADMIN_ROUTES } from './access-admin.routes';

describe('access-admin routes', () => {
  it('keeps the Owner-only feature page as the feature root without adding a navigation route', () => {
    expect(ACCESS_ADMIN_ROUTES).toHaveLength(1);
    expect(ACCESS_ADMIN_ROUTES[0]?.path).toBe('');
    expect(ACCESS_ADMIN_ROUTES[0]?.loadComponent).toBeTypeOf('function');
  });
});
