import { describe, expect, it } from 'vitest';

import {
  ACCESS_ACCOUNT_VARIANTS,
  AccessAccountVariant,
  accessAccountVariant,
  accessLifecycleActionsApply,
  accessLifecycleTone,
  accessUserNameLabel,
  canReplaceUserPermissions,
  canSelectUserPermissions,
} from './access-admin.models';

const TARGETS: readonly {
  readonly status: string;
  readonly isOwner: boolean;
  readonly variant: AccessAccountVariant;
}[] = [
  { status: 'pending', isOwner: false, variant: 'pending-non-owner' },
  { status: 'active', isOwner: false, variant: 'active-non-owner' },
  { status: 'disabled', isOwner: false, variant: 'disabled-non-owner' },
  { status: 'pending', isOwner: true, variant: 'pending-owner' },
  { status: 'active', isOwner: true, variant: 'active-owner' },
  { status: 'disabled', isOwner: true, variant: 'disabled-owner' },
  { status: 'archived', isOwner: false, variant: 'unknown-status' },
  { status: '', isOwner: true, variant: 'unknown-status' },
];

describe('accessAccountVariant', () => {
  it.each(TARGETS)(
    'reads status "$status" with isOwner=$isOwner as $variant',
    ({ status, isOwner, variant }) => {
      expect(accessAccountVariant({ status, isOwner })).toBe(variant);
    },
  );

  it('produces every modelled variant and nothing outside the closed set', () => {
    const produced = new Set(TARGETS.map((target) => target.variant));

    expect([...produced].sort()).toEqual([...ACCESS_ACCOUNT_VARIANTS].sort());
  });

  it('has no variant for an unselected account', () => {
    expect(accessAccountVariant(null)).toBeNull();
  });

  it('never routes an unknown status to a disabled body', () => {
    expect(accessAccountVariant({ status: 'archived', isOwner: false })).not.toBe(
      'disabled-non-owner',
    );
    expect(accessAccountVariant({ status: 'archived', isOwner: true })).not.toBe('disabled-owner');
  });
});

describe('accessLifecycleTone', () => {
  it.each([
    ['pending', 'pending'],
    ['active', 'active'],
    ['disabled', 'disabled'],
    ['archived', 'unknown'],
    ['', 'unknown'],
  ])('reads %s as the %s badge tone', (status, tone) => {
    expect(accessLifecycleTone(status)).toBe(tone);
  });
});

describe('permission predicates over the exhaustive variants', () => {
  it.each(TARGETS)(
    'offers a permission editor to $variant only when it is a pending or active non-Owner',
    ({ status, isOwner, variant }) => {
      const editable = variant === 'pending-non-owner' || variant === 'active-non-owner';

      expect(canSelectUserPermissions({ status, isOwner })).toBe(editable);
    },
  );

  it.each(TARGETS)('offers a permission replace to $variant only for an active non-Owner', ({
    status,
    isOwner,
    variant,
  }) => {
    expect(canReplaceUserPermissions({ status, isOwner }, true)).toBe(
      variant === 'active-non-owner',
    );
  });

  it('refuses a permission replace while assignment is unavailable', () => {
    expect(canReplaceUserPermissions({ status: 'active', isOwner: false }, false)).toBe(false);
  });

  it.each(TARGETS)(
    'offers lifecycle actions on $variant only when the account is a known-status non-Owner',
    ({ status, isOwner, variant }) => {
      const actionable =
        variant === 'pending-non-owner' ||
        variant === 'active-non-owner' ||
        variant === 'disabled-non-owner';

      expect(accessLifecycleActionsApply({ status, isOwner })).toBe(actionable);
    },
  );

  it('offers lifecycle actions on no account at all while none is selected', () => {
    expect(accessLifecycleActionsApply(null)).toBe(false);
  });
});

describe('accessUserNameLabel', () => {
  it.each([
    ['a stored name', 'عضو', 'عضو'],
    ['a whitespace-only stored name', '   ', 'member@example.test'],
    ['no stored name', null, 'member@example.test'],
  ])('labels an account with %s', (_scenario, displayName, expected) => {
    expect(accessUserNameLabel({ displayName, email: 'member@example.test' })).toBe(expected);
  });
});
