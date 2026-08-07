import { signal } from '@angular/core';
import { getTestBed, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { PERMISSION_CODES, PermissionCode } from '../../../core/auth/permission-code';
import { AbwabPermissionsController, ABWAB_WRITE_PERMISSIONS } from './abwab-permissions.controller';

type Capability =
  | 'canCreateDoor'
  | 'canEditDoor'
  | 'canMoveDoor'
  | 'canReorderDoor'
  | 'canArchiveDoor'
  | 'canRestoreDoor'
  | 'canCreateSection'
  | 'canEditSection'
  | 'canReorderSection'
  | 'canDeleteSection'
  | 'canCreateRelation'
  | 'canDeleteRelation'
  | 'canCreateTemplate'
  | 'canDeleteTemplate'
  | 'canApplyTemplate'
  | 'canCreateTemplateNode'
  | 'canEditTemplateNode'
  | 'canReorderTemplateNode'
  | 'canDeleteTemplateNode';

const CAPABILITIES: readonly { readonly capability: Capability; readonly permission: PermissionCode }[] = [
  { capability: 'canCreateDoor', permission: ABWAB_WRITE_PERMISSIONS.createDoor },
  { capability: 'canEditDoor', permission: ABWAB_WRITE_PERMISSIONS.editDoor },
  { capability: 'canMoveDoor', permission: ABWAB_WRITE_PERMISSIONS.moveDoor },
  { capability: 'canReorderDoor', permission: ABWAB_WRITE_PERMISSIONS.reorderDoor },
  { capability: 'canArchiveDoor', permission: ABWAB_WRITE_PERMISSIONS.archiveDoor },
  { capability: 'canRestoreDoor', permission: ABWAB_WRITE_PERMISSIONS.restoreDoor },
  { capability: 'canCreateSection', permission: ABWAB_WRITE_PERMISSIONS.createSection },
  { capability: 'canEditSection', permission: ABWAB_WRITE_PERMISSIONS.editSection },
  { capability: 'canReorderSection', permission: ABWAB_WRITE_PERMISSIONS.reorderSection },
  { capability: 'canDeleteSection', permission: ABWAB_WRITE_PERMISSIONS.deleteSection },
  { capability: 'canCreateRelation', permission: ABWAB_WRITE_PERMISSIONS.createRelation },
  { capability: 'canDeleteRelation', permission: ABWAB_WRITE_PERMISSIONS.deleteRelation },
  { capability: 'canCreateTemplate', permission: ABWAB_WRITE_PERMISSIONS.createTemplate },
  { capability: 'canDeleteTemplate', permission: ABWAB_WRITE_PERMISSIONS.deleteTemplate },
  { capability: 'canApplyTemplate', permission: ABWAB_WRITE_PERMISSIONS.applyTemplate },
  { capability: 'canCreateTemplateNode', permission: ABWAB_WRITE_PERMISSIONS.createTemplateNode },
  { capability: 'canEditTemplateNode', permission: ABWAB_WRITE_PERMISSIONS.editTemplateNode },
  { capability: 'canReorderTemplateNode', permission: ABWAB_WRITE_PERMISSIONS.reorderTemplateNode },
  { capability: 'canDeleteTemplateNode', permission: ABWAB_WRITE_PERMISSIONS.deleteTemplateNode },
];

function setup(permissions: readonly PermissionCode[] = []) {
  const granted = signal<ReadonlySet<PermissionCode>>(new Set(permissions));
  const authStateKnown = signal(true);
  const isAuthenticated = signal(true);
  const loadState = signal<'idle' | 'loading' | 'ready' | 'error'>('ready');
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      AbwabPermissionsController,
      {
        provide: CurrentUserStore,
        useValue: {
          can: (permission: PermissionCode) => granted().has(permission),
          canAny: (codes: readonly PermissionCode[]) => codes.some((permission) => granted().has(permission)),
          authStateKnown,
          isAuthenticated,
          loadState,
        },
      },
    ],
  });
  return {
    controller: TestBed.inject(AbwabPermissionsController),
    granted,
    authStateKnown,
    isAuthenticated,
    loadState,
  };
}

describe('AbwabPermissionsController', () => {
  it('maps every Abwab write capability to the existing typed permission catalogue exactly once', () => {
    expect(Object.values(ABWAB_WRITE_PERMISSIONS)).toEqual(PERMISSION_CODES);
  });

  it.each(CAPABILITIES)('enables $capability only for $permission', ({ capability, permission }) => {
    const { controller, granted } = setup([permission]);

    expect((controller[capability] as () => boolean)()).toBe(true);

    granted.set(new Set());
    expect((controller[capability] as () => boolean)()).toBe(false);
  });

  it('keeps sections and bulk mode capability-based rather than role-based', () => {
    const { controller, granted } = setup([ABWAB_WRITE_PERMISSIONS.editSection]);

    expect(controller.canManageSections()).toBe(true);
    expect(controller.canUseBulkMode()).toBe(false);

    granted.set(new Set([ABWAB_WRITE_PERMISSIONS.createRelation]));
    expect(controller.canManageSections()).toBe(false);
    expect(controller.canUseBulkMode()).toBe(true);
  });

  it('holds URL write overlays until access state resolves, while relations remains public', () => {
    const { controller, authStateKnown, loadState, granted } = setup([ABWAB_WRITE_PERMISSIONS.createDoor]);

    authStateKnown.set(false);
    expect(controller.isResolved()).toBe(false);

    authStateKnown.set(true);
    loadState.set('loading');
    expect(controller.isResolved()).toBe(false);

    loadState.set('ready');
    expect(controller.canOpenModal('create')).toBe(true);
    expect(controller.canOpenModal('relations')).toBe(true);

    granted.set(new Set());
    expect(controller.canOpenModal('create')).toBe(false);
    expect(controller.canOpenModal('relations')).toBe(true);
  });

  it('limits root and child template context menus to their exact delete capability', () => {
    const { controller, granted } = setup([ABWAB_WRITE_PERMISSIONS.deleteTemplate]);

    expect(controller.canOpenTemplateContextMenu(true)).toBe(true);
    expect(controller.canOpenTemplateContextMenu(false)).toBe(false);

    granted.set(new Set([ABWAB_WRITE_PERMISSIONS.deleteTemplateNode]));
    expect(controller.canOpenTemplateContextMenu(true)).toBe(false);
    expect(controller.canOpenTemplateContextMenu(false)).toBe(true);
  });
});
