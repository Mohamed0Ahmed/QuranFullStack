import { afterEach, describe, expect, it } from 'vitest';

import { AuthReturnLocationStore } from './auth-return-location.store';

describe('AuthReturnLocationStore', () => {
  const store = new AuthReturnLocationStore();

  afterEach(() => {
    store.clear();
  });

  it('restores one internal protected location including query and fragment', () => {
    store.remember('/settings/access?tab=roles#owner');

    expect(store.consume()).toBe('/settings/access?tab=roles#owner');
    expect(store.consume()).toBe('/dashboard');
  });

  it.each(['https://example.test', '//example.test', '/\\example.test'])(
    'does not retain an unsafe return location: %s',
    (unsafeLocation) => {
      store.remember(unsafeLocation);

      expect(store.consume('/fallback')).toBe('/fallback');
    },
  );
});
