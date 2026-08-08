import { describe, expect, it } from 'vitest';

import { PermissionCatalogueItem } from '../../../core/api/generated/models/permission-catalogue-item';
import {
  buildPermissionGroups,
  permissionCodesForSubmission,
  setGroupSelection,
  setIndividualSelection,
} from './access-admin-permissions';

const CATALOGUE: PermissionCatalogueItem[] = [
  {
    code: 'abwab.doors.create',
    arabicLabel: 'إضافة باب',
    englishDescription: 'Create a door.',
    groupKey: 'doors',
    groupLabel: 'الأبواب',
    groupDisplayOrder: 1,
    displayOrder: 1,
  },
  {
    code: 'abwab.doors.edit',
    arabicLabel: 'تعديل باب',
    englishDescription: 'Edit a door.',
    groupKey: 'doors',
    groupLabel: 'الأبواب',
    groupDisplayOrder: 1,
    displayOrder: 2,
  },
  {
    code: 'abwab.sections.create',
    arabicLabel: 'إضافة قسم',
    englishDescription: 'Create a section.',
    groupKey: 'sections',
    groupLabel: 'الأقسام',
    groupDisplayOrder: 2,
    displayOrder: 1,
  },
];

describe('access-admin permission selection', () => {
  it('expands a group selection into only its individual permission codes for submission', () => {
    const doors = buildPermissionGroups(CATALOGUE).find((group) => group.key === 'doors');

    expect(doors).toBeDefined();
    expect(doors?.labels.get('abwab.doors.create')).toBe('إضافة باب');

    const selected = setGroupSelection(new Set(), doors!, true);

    expect(permissionCodesForSubmission(selected)).toEqual([
      'abwab.doors.create',
      'abwab.doors.edit',
    ]);
  });

  it('keeps every individual code uncheckable after selecting its entire group', () => {
    const doors = buildPermissionGroups(CATALOGUE).find((group) => group.key === 'doors');

    expect(doors).toBeDefined();

    const allDoors = setGroupSelection(new Set(), doors!, true);
    const withoutEdit = setIndividualSelection(allDoors, 'abwab.doors.edit', false);

    expect(permissionCodesForSubmission(withoutEdit)).toEqual(['abwab.doors.create']);
    expect(withoutEdit.has('abwab.doors.edit')).toBe(false);
  });

  it('drops group-like sentinels and values that are not permission codes from a request payload', () => {
    const selected = new Set(['abwab.doors.create', 'doors.manage-all', 'not-a-permission']);

    expect(permissionCodesForSubmission(selected)).toEqual(['abwab.doors.create']);
  });

  it('keeps a real permission code the served catalogue does not offer, in canonical order', () => {
    const cataloguedCodes = CATALOGUE.map((item) => item.code);
    const selected = new Set(['abwab.doors.create', 'abwab.template_nodes.delete']);

    expect(cataloguedCodes).not.toContain('abwab.template_nodes.delete');
    expect(permissionCodesForSubmission(selected)).toEqual([
      'abwab.doors.create',
      'abwab.template_nodes.delete',
    ]);
  });
});
