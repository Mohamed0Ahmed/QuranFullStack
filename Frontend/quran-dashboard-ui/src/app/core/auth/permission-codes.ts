// Mirror of the backend PermissionCatalogue; PermissionParityTests reads this file and fails on drift.
export const PERMISSION_CODES = [
  'attribution.view',
  'attribution.manage',
  'permission.administer',
  'audit.restore',
  'safetyPoint.manage',
  'section.view',
  'section.add',
  'section.edit',
  'section.reorder',
  'section.delete',
  'category.view',
  'category.add',
  'category.edit',
  'category.move',
  'category.reorder',
  'category.delete',
  'protection.view',
  'protection.apply',
  'protection.lift',
  'relationship.view',
  'relationship.add',
  'relationship.edit',
  'relationship.delete',
  'relationship.restore',
  'template.view',
  'template.add',
  'template.edit',
  'template.delete',
  'template.restore',
  'template.apply',
] as const;

export type PermissionCode = (typeof PERMISSION_CODES)[number];

export const BASELINE_PERMISSION_CODE: PermissionCode = 'attribution.view';
