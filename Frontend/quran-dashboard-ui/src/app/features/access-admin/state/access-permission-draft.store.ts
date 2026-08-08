import { computed, signal } from '@angular/core';

import { AccessUserPermissions } from '../../../core/api/generated/models/access-user-permissions';
import { PermissionCatalogueItem } from '../../../core/api/generated/models/permission-catalogue-item';
import { PermissionCode, isPermissionCode } from '../../../core/auth/permission-code';
import { AccessPermissionDiff, hasPermissionChanges } from '../models/access-admin.models';
import {
  AccessPermissionGroup,
  buildPermissionGroups,
  permissionCodesForSubmission,
} from '../models/access-admin-permissions';

export class AccessPermissionDraftStore {
  private readonly catalogueState = signal<readonly PermissionCatalogueItem[]>([]);
  private readonly assignmentReadyState = signal(false);
  private readonly catalogueLoadingState = signal(false);
  private readonly catalogueErrorState = signal<string | null>(null);
  private readonly grantedState = signal<AccessUserPermissions | null>(null);
  private readonly draftState = signal<ReadonlySet<PermissionCode>>(new Set());

  readonly groups = computed<readonly AccessPermissionGroup[]>(() =>
    buildPermissionGroups(this.catalogueState()),
  );
  readonly assignmentReady = this.assignmentReadyState.asReadonly();
  readonly catalogueLoading = this.catalogueLoadingState.asReadonly();
  readonly catalogueError = this.catalogueErrorState.asReadonly();
  readonly grantedPermissions = this.grantedState.asReadonly();
  readonly codes = this.draftState.asReadonly();
  readonly canAssign = computed(
    () => this.assignmentReady() && !this.catalogueError() && this.groups().length > 0,
  );
  readonly diff = computed<AccessPermissionDiff>(() => {
    const granted = this.grantedCodes();
    const draft = this.draftState();
    return {
      granted: permissionCodesForSubmission(draft).filter((code) => !granted.has(code)),
      revoked: permissionCodesForSubmission(granted).filter((code) => !draft.has(code)),
    };
  });
  readonly isDirty = computed(() => this.canAssign() && hasPermissionChanges(this.diff()));

  beginCatalogueLoad(): void {
    this.catalogueLoadingState.set(true);
    this.catalogueErrorState.set(null);
  }

  publishCatalogue(items: readonly PermissionCatalogueItem[], assignmentReady: boolean): void {
    this.catalogueState.set(items);
    this.assignmentReadyState.set(assignmentReady);
  }

  failCatalogue(message: string): void {
    this.assignmentReadyState.set(false);
    this.catalogueErrorState.set(message);
  }

  endCatalogueLoad(): void {
    this.catalogueLoadingState.set(false);
  }

  adopt(granted: AccessUserPermissions): void {
    this.grantedState.set(granted);
    this.draftState.set(knownCodes(granted.permissionCodes));
  }

  setCodes(codes: ReadonlySet<string>): void {
    this.draftState.set(knownCodes([...codes]));
  }

  discard(): void {
    this.draftState.set(knownCodes(this.grantedState()?.permissionCodes ?? []));
  }

  clear(): void {
    this.grantedState.set(null);
    this.draftState.set(new Set());
  }

  codesForSubmission(): string[] {
    return [...permissionCodesForSubmission(this.draftState()), ...this.unmodelledGrantedCodes()];
  }

  private grantedCodes(): ReadonlySet<PermissionCode> {
    return knownCodes(this.grantedState()?.permissionCodes ?? []);
  }

  private unmodelledGrantedCodes(): string[] {
    return (this.grantedState()?.permissionCodes ?? []).filter((code) => !isPermissionCode(code));
  }
}

function knownCodes(codes: readonly string[]): Set<PermissionCode> {
  return new Set(codes.filter(isPermissionCode));
}
