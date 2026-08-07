export const ABWAB_PERMISSION_CODES = {
  doors: {
    create: 'abwab.doors.create',
    edit: 'abwab.doors.edit',
    move: 'abwab.doors.move',
    reorder: 'abwab.doors.reorder',
    archive: 'abwab.doors.archive',
    restore: 'abwab.doors.restore',
  },
  sections: {
    create: 'abwab.sections.create',
    edit: 'abwab.sections.edit',
    reorder: 'abwab.sections.reorder',
    delete: 'abwab.sections.delete',
  },
  relations: {
    create: 'abwab.relations.create',
    delete: 'abwab.relations.delete',
  },
  templates: {
    create: 'abwab.templates.create',
    delete: 'abwab.templates.delete',
    apply: 'abwab.templates.apply',
  },
  templateNodes: {
    create: 'abwab.template_nodes.create',
    edit: 'abwab.template_nodes.edit',
    reorder: 'abwab.template_nodes.reorder',
    delete: 'abwab.template_nodes.delete',
  },
} as const;

export const PERMISSION_CODES = [
  ABWAB_PERMISSION_CODES.doors.create,
  ABWAB_PERMISSION_CODES.doors.edit,
  ABWAB_PERMISSION_CODES.doors.move,
  ABWAB_PERMISSION_CODES.doors.reorder,
  ABWAB_PERMISSION_CODES.doors.archive,
  ABWAB_PERMISSION_CODES.doors.restore,
  ABWAB_PERMISSION_CODES.sections.create,
  ABWAB_PERMISSION_CODES.sections.edit,
  ABWAB_PERMISSION_CODES.sections.reorder,
  ABWAB_PERMISSION_CODES.sections.delete,
  ABWAB_PERMISSION_CODES.relations.create,
  ABWAB_PERMISSION_CODES.relations.delete,
  ABWAB_PERMISSION_CODES.templates.create,
  ABWAB_PERMISSION_CODES.templates.delete,
  ABWAB_PERMISSION_CODES.templates.apply,
  ABWAB_PERMISSION_CODES.templateNodes.create,
  ABWAB_PERMISSION_CODES.templateNodes.edit,
  ABWAB_PERMISSION_CODES.templateNodes.reorder,
  ABWAB_PERMISSION_CODES.templateNodes.delete,
] as const;

export type PermissionCode = (typeof PERMISSION_CODES)[number];

const permissionCodeSet = new Set<string>(PERMISSION_CODES);

export function isPermissionCode(value: string): value is PermissionCode {
  return permissionCodeSet.has(value);
}
