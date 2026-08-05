import { describe, expect, it } from 'vitest';

import { ACCESS_ME_CONTRACT_FIXTURES } from './auth.testing';

describe('access contract fixtures', () => {
  it('covers pending, read-only, exact-permission, and Owner target responses', () => {
    expect(Object.keys(ACCESS_ME_CONTRACT_FIXTURES)).toEqual([
      'pending',
      'readOnly',
      'exactPermission',
      'owner',
    ]);
    expect(ACCESS_ME_CONTRACT_FIXTURES.pending.isOwner).toBe(false);
    expect(ACCESS_ME_CONTRACT_FIXTURES.readOnly.permissions).toEqual([]);
    expect(ACCESS_ME_CONTRACT_FIXTURES.exactPermission.permissions).toEqual(['abwab.doors.create']);
    expect(ACCESS_ME_CONTRACT_FIXTURES.owner.isOwner).toBe(true);
  });

  it('keeps roleName transitional and never uses it as the permission authority', () => {
    expect(ACCESS_ME_CONTRACT_FIXTURES.owner.roleName).toBe('Owner');
    expect(ACCESS_ME_CONTRACT_FIXTURES.exactPermission.roleName).toBeNull();
  });
});
