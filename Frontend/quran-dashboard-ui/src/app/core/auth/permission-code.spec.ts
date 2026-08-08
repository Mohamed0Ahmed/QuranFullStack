import { describe, expect, it } from 'vitest';

import { ABWAB_PERMISSION_CODES, PERMISSION_CODES, isPermissionCode } from './permission-code';

const generatedGroups = Object.values(ABWAB_PERMISSION_CODES);
const generatedCodeCount = generatedGroups.reduce(
  (total, group) => total + Object.keys(group).length,
  0,
);

describe('permission codes', () => {
  it('offers the codes of every generated group, each exactly once', () => {
    expect(generatedCodeCount).toBeGreaterThan(0);
    expect(PERMISSION_CODES).toHaveLength(generatedCodeCount);
    expect(new Set(PERMISSION_CODES).size).toBe(generatedCodeCount);

    for (const group of generatedGroups) {
      expect(PERMISSION_CODES).toEqual(expect.arrayContaining(Object.values(group)));
    }
  });

  it('carries the server code shape on every entry', () => {
    expect(PERMISSION_CODES.length).toBeGreaterThan(0);
    expect(PERMISSION_CODES.every((code) => /^abwab\.[a-z0-9_]+\.[a-z0-9_]+$/.test(code))).toBe(true);
  });

  it('recognises every allowlisted code', () => {
    expect(PERMISSION_CODES.length).toBeGreaterThan(0);
    expect(PERMISSION_CODES.filter((code) => !isPermissionCode(code))).toEqual([]);
  });

  it.each([
    ['a code no group declares', 'abwab.doors.publish'],
    ['a group sentinel', 'doors.manage-all'],
    ['a bare group name', 'abwab.doors'],
    ['a prefixed code', ' abwab.doors.create'],
    ['a suffixed code', 'abwab.doors.create.'],
    ['an empty value', ''],
  ])('refuses %s', (_case, value) => {
    expect(isPermissionCode(value)).toBe(false);
  });
});
