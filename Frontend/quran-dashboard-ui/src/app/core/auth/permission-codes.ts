// Mirror of the backend PermissionCatalogue; PermissionParityTests reads this file and fails on drift.
export const PERMISSION_CODES = [
  'attribution.view',
  'attribution.manage',
  'permission.administer',
  'audit.restore',
  'safetyPoint.manage',
] as const;

export type PermissionCode = (typeof PERMISSION_CODES)[number];

export const BASELINE_PERMISSION_CODE: PermissionCode = 'attribution.view';
