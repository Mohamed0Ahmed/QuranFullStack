import { Injectable, computed, inject } from '@angular/core';

import { CurrentUserStore } from '../../../core/auth/current-user.store';

// T074: frontend composite-read visibility wiring from /me permissions. This is COSMETIC ONLY — the
// backend DTO projection (tree-read-contract.md §"Composite-read redaction") is the sole authority;
// a hidden action invoked directly is still rejected server-side. Tree/search visibility requires
// BOTH category.view and section.view (every result exposes section/path context); protection detail
// requires protection.view on top of that, mirroring the redaction table exactly.
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

  private has(code: Parameters<CurrentUserStore['hasPermission']>[0]): boolean {
    return this.currentUser.hasPermission(code);
  }
}
