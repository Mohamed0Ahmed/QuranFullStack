import { Injectable, computed, inject } from '@angular/core';

import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { ABWAB_PERMISSION_CODES, PermissionCode } from '../../../core/auth/permission-code';
import { AbwabModalKind } from '../models/abwab.models';

export const ABWAB_WRITE_PERMISSIONS = {
  createDoor: ABWAB_PERMISSION_CODES.doors.create,
  editDoor: ABWAB_PERMISSION_CODES.doors.edit,
  moveDoor: ABWAB_PERMISSION_CODES.doors.move,
  reorderDoor: ABWAB_PERMISSION_CODES.doors.reorder,
  archiveDoor: ABWAB_PERMISSION_CODES.doors.archive,
  restoreDoor: ABWAB_PERMISSION_CODES.doors.restore,
  createSection: ABWAB_PERMISSION_CODES.sections.create,
  editSection: ABWAB_PERMISSION_CODES.sections.edit,
  reorderSection: ABWAB_PERMISSION_CODES.sections.reorder,
  deleteSection: ABWAB_PERMISSION_CODES.sections.delete,
  createRelation: ABWAB_PERMISSION_CODES.relations.create,
  deleteRelation: ABWAB_PERMISSION_CODES.relations.delete,
  createTemplate: ABWAB_PERMISSION_CODES.templates.create,
  deleteTemplate: ABWAB_PERMISSION_CODES.templates.delete,
  applyTemplate: ABWAB_PERMISSION_CODES.templates.apply,
  createTemplateNode: ABWAB_PERMISSION_CODES.templateNodes.create,
  editTemplateNode: ABWAB_PERMISSION_CODES.templateNodes.edit,
  reorderTemplateNode: ABWAB_PERMISSION_CODES.templateNodes.reorder,
  deleteTemplateNode: ABWAB_PERMISSION_CODES.templateNodes.delete,
} as const satisfies Record<string, PermissionCode>;

const SECTION_PERMISSIONS = [
  ABWAB_WRITE_PERMISSIONS.createSection,
  ABWAB_WRITE_PERMISSIONS.editSection,
  ABWAB_WRITE_PERMISSIONS.reorderSection,
  ABWAB_WRITE_PERMISSIONS.deleteSection,
] as const;

const BULK_PERMISSIONS = [
  ABWAB_WRITE_PERMISSIONS.moveDoor,
  ABWAB_WRITE_PERMISSIONS.archiveDoor,
  ABWAB_WRITE_PERMISSIONS.createRelation,
] as const;

@Injectable()
export class AbwabPermissionsController {
  private readonly currentUserStore = inject(CurrentUserStore);

  readonly isResolved = computed(() => {
    if (!this.currentUserStore.authStateKnown()) {
      return false;
    }

    return !this.currentUserStore.isAuthenticated() || this.currentUserStore.loadState() !== 'loading';
  });
  readonly isOwner = computed(
    () => this.currentUserStore.isActive() && this.currentUserStore.isOwner(),
  );

  readonly canCreateDoor = computed(() => this.can(ABWAB_WRITE_PERMISSIONS.createDoor));
  readonly canEditDoor = computed(() => this.can(ABWAB_WRITE_PERMISSIONS.editDoor));
  readonly canMoveDoor = computed(() => this.can(ABWAB_WRITE_PERMISSIONS.moveDoor));
  readonly canReorderDoor = computed(() => this.can(ABWAB_WRITE_PERMISSIONS.reorderDoor));
  readonly canArchiveDoor = computed(() => this.can(ABWAB_WRITE_PERMISSIONS.archiveDoor));
  readonly canRestoreDoor = computed(() => this.can(ABWAB_WRITE_PERMISSIONS.restoreDoor));

  readonly canCreateSection = computed(() => this.can(ABWAB_WRITE_PERMISSIONS.createSection));
  readonly canEditSection = computed(() => this.can(ABWAB_WRITE_PERMISSIONS.editSection));
  readonly canReorderSection = computed(() => this.can(ABWAB_WRITE_PERMISSIONS.reorderSection));
  readonly canDeleteSection = computed(() => this.can(ABWAB_WRITE_PERMISSIONS.deleteSection));
  readonly canManageSections = computed(() => this.canAny(SECTION_PERMISSIONS));

  readonly canCreateRelation = computed(() => this.can(ABWAB_WRITE_PERMISSIONS.createRelation));
  readonly canDeleteRelation = computed(() => this.can(ABWAB_WRITE_PERMISSIONS.deleteRelation));
  readonly canUseBulkMode = computed(() => this.canAny(BULK_PERMISSIONS));

  readonly canCreateTemplate = computed(() => this.can(ABWAB_WRITE_PERMISSIONS.createTemplate));
  readonly canDeleteTemplate = computed(() => this.can(ABWAB_WRITE_PERMISSIONS.deleteTemplate));
  readonly canApplyTemplate = computed(() => this.can(ABWAB_WRITE_PERMISSIONS.applyTemplate));
  readonly canCreateTemplateNode = computed(() => this.can(ABWAB_WRITE_PERMISSIONS.createTemplateNode));
  readonly canEditTemplateNode = computed(() => this.can(ABWAB_WRITE_PERMISSIONS.editTemplateNode));
  readonly canReorderTemplateNode = computed(() => this.can(ABWAB_WRITE_PERMISSIONS.reorderTemplateNode));
  readonly canDeleteTemplateNode = computed(() => this.can(ABWAB_WRITE_PERMISSIONS.deleteTemplateNode));

  can(permission: PermissionCode): boolean {
    return this.currentUserStore.can(permission);
  }

  canAny(permissions: readonly PermissionCode[]): boolean {
    return this.currentUserStore.canAny(permissions);
  }

  canOpenModal(kind: AbwabModalKind): boolean {
    switch (kind) {
      case 'create':
      case 'child':
        return this.canCreateDoor();
      case 'edit':
        return this.canEditDoor();
      case 'move':
        return this.canMoveDoor();
      case 'sections':
        return this.canManageSections();
      case 'relations':
        return true;
    }
  }

  canOpenTemplateContextMenu(isRoot: boolean): boolean {
    return isRoot
      ? this.canEditTemplateNode() || this.canCreateTemplateNode() || this.canDeleteTemplate()
      : this.canEditTemplateNode() || this.canCreateTemplateNode() || this.canDeleteTemplateNode();
  }
}
