import { Injectable, computed, inject } from '@angular/core';

import { CurrentUserStore } from '../../../core/auth/current-user.store';

// COSMETIC ONLY — the backend DTO projection (tree-read-contract.md §"Composite-read redaction") is
// the sole authority; a hidden action invoked directly is still rejected server-side.
@Injectable()
export class AbwabPermissions {
  private readonly currentUser = inject(CurrentUserStore);

  readonly canViewTree = computed(() => this.has('category.view') && this.has('section.view'));
  readonly canViewProtectionDetail = computed(() => this.canViewTree() && this.has('protection.view'));

  readonly canAddCategory = computed(() => this.has('category.add'));
  readonly canEditCategory = computed(() => this.has('category.edit'));
  readonly canMoveCategory = computed(() => this.has('category.move'));
  readonly canReorderCategory = computed(() => this.has('category.reorder'));
  readonly canDeleteCategory = computed(() => this.has('category.delete'));

  readonly canAddSection = computed(() => this.has('section.add'));
  readonly canEditSection = computed(() => this.has('section.edit'));
  readonly canReorderSection = computed(() => this.has('section.reorder'));
  readonly canDeleteSection = computed(() => this.has('section.delete'));

  readonly canApplyProtection = computed(() => this.has('protection.apply'));
  readonly canLiftProtection = computed(() => this.has('protection.lift'));

  readonly canViewRelationship = computed(() => this.has('relationship.view'));
  readonly canAddRelationship = computed(() => this.has('relationship.add'));
  readonly canEditRelationship = computed(() => this.has('relationship.edit'));
  readonly canDeleteRelationship = computed(() => this.has('relationship.delete'));
  readonly canRestoreRelationship = computed(() => this.has('relationship.restore'));

  private has(code: Parameters<CurrentUserStore['hasPermission']>[0]): boolean {
    return this.currentUser.hasPermission(code);
  }
}
