import { ABWAB_PERMISSION_CODES } from './permission-codes.generated';

export { ABWAB_PERMISSION_CODES };

type PermissionCodeGroups = typeof ABWAB_PERMISSION_CODES;

export type PermissionCode = {
  [Group in keyof PermissionCodeGroups]: PermissionCodeGroups[Group][keyof PermissionCodeGroups[Group]];
}[keyof PermissionCodeGroups];

export const PERMISSION_CODES: readonly PermissionCode[] = Object.values(
  ABWAB_PERMISSION_CODES,
).flatMap((group) => Object.values(group));

const permissionCodeSet = new Set<string>(PERMISSION_CODES);

export function isPermissionCode(value: string): value is PermissionCode {
  return permissionCodeSet.has(value);
}
