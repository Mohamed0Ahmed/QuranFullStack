export const PERMISSION_CODES = [
  'abwab.doors.create',
  'abwab.doors.edit',
  'abwab.doors.move',
  'abwab.doors.reorder',
  'abwab.doors.archive',
  'abwab.doors.restore',
  'abwab.sections.create',
  'abwab.sections.edit',
  'abwab.sections.reorder',
  'abwab.sections.delete',
  'abwab.relations.create',
  'abwab.relations.delete',
  'abwab.templates.create',
  'abwab.templates.delete',
  'abwab.templates.apply',
  'abwab.template_nodes.create',
  'abwab.template_nodes.edit',
  'abwab.template_nodes.reorder',
  'abwab.template_nodes.delete',
] as const;

export type PermissionCode = (typeof PERMISSION_CODES)[number];

const permissionCodeSet = new Set<string>(PERMISSION_CODES);

export function isPermissionCode(value: string): value is PermissionCode {
  return permissionCodeSet.has(value);
}
