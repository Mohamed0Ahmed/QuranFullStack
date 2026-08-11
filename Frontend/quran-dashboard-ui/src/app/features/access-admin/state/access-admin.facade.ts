import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';

import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { WriteAuthFailureCoordinator } from '../../../core/auth/write-auth-failure.coordinator';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { AccessUserDetail } from '../../../core/api/generated/models/access-user-detail';
import { AccessUserSummaryPagedResult } from '../../../core/api/generated/models/access-user-summary-paged-result';
import { AccessAdminApi } from '../data-access/access-admin.api';
import {
  AccessUserListQuery,
  canReplaceUserPermissions,
  canSelectUserPermissions,
} from '../models/access-admin.models';
import { ACCESS_ADMIN_LOAD_ERROR, failureMessage } from './access-admin-request-failure';
import { AccessPermissionDraftStore } from './access-permission-draft.store';

export type AccessAdminMutationOutcome =
  | 'success'
  | 'invalid'
  | 'conflict'
  | 'forbidden'
  | 'unauthorized'
  | 'error';

export type AccessAdminMessageTone = 'success' | 'notice' | 'error';

export interface AccessAdminMessage {
  readonly text: string;
  readonly tone: AccessAdminMessageTone;
}

const defaultUserQuery: AccessUserListQuery = { page: 1, pageSize: 25 };

@Injectable()
export class AccessAdminFacade {
  private static readonly accessDeniedMessage = 'لا تملك صلاحية إدارة الوصول.';
  private static readonly writeErrorMessage = 'تعذر إتمام التغيير المطلوب.';
  private static readonly mutationSuccessMessage = 'تم حفظ التغيير.';
  private static readonly conflictMessage = 'تغيرت بيانات المستخدم. تم تحديث الحالة الحالية.';

  private readonly api = inject(AccessAdminApi);
  private readonly currentUserStore = inject(CurrentUserStore);
  private readonly writeAuthFailureCoordinator = inject(WriteAuthFailureCoordinator);
  private readonly draft = new AccessPermissionDraftStore();

  private readonly usersState = signal<AccessUserSummaryPagedResult | null>(null);
  private readonly userQueryState = signal<AccessUserListQuery>(defaultUserQuery);
  private readonly usersLoadingState = signal(false);
  private readonly usersErrorState = signal<string | null>(null);
  private readonly selectedUserState = signal<AccessUserDetail | null>(null);
  private readonly selectedUserLoadingState = signal(false);
  private readonly selectedUserErrorState = signal<string | null>(null);
  private readonly mutationMessageState = signal<AccessAdminMessage | null>(null);
  private readonly busyActionState = signal<string | null>(null);
  private usersRequestVersion = 0;
  private selectedUserRequestVersion = 0;

  readonly canAccess = computed(() => this.currentUserStore.isActive() && this.currentUserStore.isOwner());
  readonly accessStateKnown = computed(() => {
    const loadState = this.currentUserStore.loadState();
    if (loadState === 'ready' || loadState === 'error') {
      return true;
    }
    return loadState === 'idle' && this.currentUserStore.authStateKnown();
  });
  readonly users = computed(() => this.usersState()?.items ?? []);
  readonly userPage = computed(() => this.usersState()?.page ?? this.userQueryState().page);
  readonly userPageSize = computed(() => this.usersState()?.pageSize ?? this.userQueryState().pageSize);
  readonly userTotalCount = computed(() => this.usersState()?.totalCount ?? 0);
  readonly userQuery = this.userQueryState.asReadonly();
  readonly usersLoading = this.usersLoadingState.asReadonly();
  readonly usersError = this.usersErrorState.asReadonly();
  readonly permissionGroups = this.draft.groups;
  readonly assignmentReady = this.draft.assignmentReady;
  readonly catalogueLoading = this.draft.catalogueLoading;
  readonly catalogueError = this.draft.catalogueError;
  readonly canAssignPermissions = this.draft.canAssign;
  readonly selectedUser = this.selectedUserState.asReadonly();
  readonly selectedPermissions = this.draft.grantedPermissions;
  readonly selectedPermissionCodes = this.draft.codes;
  readonly selectedUserLoading = this.selectedUserLoadingState.asReadonly();
  readonly selectedUserError = this.selectedUserErrorState.asReadonly();
  readonly mutationMessage = this.mutationMessageState.asReadonly();
  readonly busyAction = this.busyActionState.asReadonly();
  readonly permissionDiff = this.draft.diff;
  readonly isDirty = this.draft.isDirty;

  async load(): Promise<void> {
    if (!this.canAccess()) {
      this.clearProtectedState();
      return;
    }

    await Promise.all([this.loadUsers(), this.loadPermissionCatalogue()]);
  }

  async updateUserQuery(query: Partial<AccessUserListQuery>): Promise<void> {
    this.userQueryState.set({ ...this.userQueryState(), ...query, page: query.page ?? 1 });
    await this.loadUsers();
  }

  async loadUsers(): Promise<void> {
    if (!this.canAccess()) {
      return;
    }

    const requestVersion = ++this.usersRequestVersion;
    this.usersLoadingState.set(true);
    this.usersErrorState.set(null);
    try {
      const response = await firstValueFrom(this.api.listUsers(this.userQueryState()));
      if (requestVersion !== this.usersRequestVersion) {
        return;
      }
      if (response.isSuccess && response.data) {
        this.usersState.set(response.data);
        return;
      }
      this.usersErrorState.set(response.message ?? ACCESS_ADMIN_LOAD_ERROR);
    } catch (error) {
      if (requestVersion === this.usersRequestVersion) {
        this.usersErrorState.set(failureMessage(error, ACCESS_ADMIN_LOAD_ERROR));
      }
    } finally {
      if (requestVersion === this.usersRequestVersion) {
        this.usersLoadingState.set(false);
      }
    }
  }

  async loadPermissionCatalogue(): Promise<void> {
    if (!this.canAccess()) {
      return;
    }

    this.draft.beginCatalogueLoad();
    try {
      const response = await firstValueFrom(this.api.getPermissionCatalogue());
      if (response.isSuccess && response.data) {
        this.draft.publishCatalogue(response.data.items, response.data.assignmentReady);
        return;
      }
      this.draft.failCatalogue(response.message ?? ACCESS_ADMIN_LOAD_ERROR);
    } catch (error) {
      this.draft.failCatalogue(failureMessage(error, ACCESS_ADMIN_LOAD_ERROR));
    } finally {
      this.draft.endCatalogueLoad();
    }
  }

  async selectUser(userId: number): Promise<void> {
    if (!this.canAccess()) {
      return;
    }

    const requestVersion = ++this.selectedUserRequestVersion;
    this.selectedUserLoadingState.set(true);
    this.selectedUserErrorState.set(null);
    this.mutationMessageState.set(null);
    try {
      const [detailResponse, permissionsResponse] = await Promise.all([
        firstValueFrom(this.api.getUser(userId)),
        firstValueFrom(this.api.getUserPermissions(userId)),
      ]);
      if (requestVersion !== this.selectedUserRequestVersion) {
        return;
      }
      if (!detailResponse.isSuccess || !detailResponse.data) {
        this.selectedUserErrorState.set(detailResponse.message ?? ACCESS_ADMIN_LOAD_ERROR);
        return;
      }
      if (!permissionsResponse.isSuccess || !permissionsResponse.data) {
        this.selectedUserErrorState.set(permissionsResponse.message ?? ACCESS_ADMIN_LOAD_ERROR);
        return;
      }

      this.selectedUserState.set(detailResponse.data);
      this.draft.adopt(permissionsResponse.data);
    } catch (error) {
      if (requestVersion === this.selectedUserRequestVersion) {
        this.selectedUserErrorState.set(failureMessage(error, ACCESS_ADMIN_LOAD_ERROR));
      }
    } finally {
      if (requestVersion === this.selectedUserRequestVersion) {
        this.selectedUserLoadingState.set(false);
      }
    }
  }

  setSelectedPermissionCodes(codes: ReadonlySet<string>): void {
    if (!this.canSelectPermissions()) {
      return;
    }
    this.draft.setCodes(codes);
  }

  discardDraft(): void {
    this.draft.discard();
  }

  async acceptSelectedUser(reason: string): Promise<AccessAdminMutationOutcome> {
    const user = this.selectedUserState();
    const normalizedReason = reason.trim();
    if (!user || user.isOwner || user.status !== 'pending') {
      return 'invalid';
    }

    return this.runMutation('accept', () =>
      this.api.acceptUser(user.id, {
        expectedVersion: user.version,
        permissionCodes: this.permissionCodesForAssignment(),
        reason: normalizedReason || null,
      }),
    );
  }

  async disableSelectedUser(reason: string): Promise<AccessAdminMutationOutcome> {
    const user = this.selectedUserState();
    const normalizedReason = reason.trim();
    if (!user || user.isOwner || user.status !== 'active') {
      return 'invalid';
    }

    return this.runMutation('disable', () =>
      this.api.disableUser(user.id, {
        expectedVersion: user.version,
        reason: normalizedReason || null,
      }),
    );
  }

  async reactivateSelectedUser(reason: string): Promise<AccessAdminMutationOutcome> {
    const user = this.selectedUserState();
    const normalizedReason = reason.trim();
    if (!user || user.isOwner || user.status !== 'disabled') {
      return 'invalid';
    }

    return this.runMutation('reactivate', () =>
      this.api.reactivateUser(user.id, {
        expectedVersion: user.version,
        reason: normalizedReason || null,
      }),
    );
  }

  async replaceSelectedPermissions(reason: string): Promise<AccessAdminMutationOutcome> {
    const user = this.selectedUserState();
    const permissions = this.draft.grantedPermissions();
    const normalizedReason = reason.trim();
    if (!user || !permissions || !this.canReplaceSelectedPermissions()) {
      return 'invalid';
    }

    return this.runMutation('permissions', () =>
      this.api.replacePermissions(user.id, {
        expectedVersion: permissions.version,
        permissionCodes: this.permissionCodesForAssignment(),
        reason: normalizedReason || null,
      }),
    );
  }

  private async runMutation<T>(
    action: string,
    request: () => Observable<ApiResponse<T>>,
  ): Promise<AccessAdminMutationOutcome> {
    const user = this.selectedUserState();
    if (!user || !this.canAccess()) {
      return 'invalid';
    }

    this.busyActionState.set(action);
    this.mutationMessageState.set(null);
    try {
      const response = await firstValueFrom(request());
      if (!response.isSuccess || !response.data) {
        this.mutationMessageState.set({
          text: response.message ?? AccessAdminFacade.writeErrorMessage,
          tone: 'error',
        });
        return 'invalid';
      }
      await this.refreshAfterMutation(user.id);
      this.mutationMessageState.set({
        text: AccessAdminFacade.mutationSuccessMessage,
        tone: 'success',
      });
      return 'success';
    } catch (error) {
      return this.handleMutationError(error);
    } finally {
      this.busyActionState.set(null);
    }
  }

  private async handleMutationError(error: unknown): Promise<AccessAdminMutationOutcome> {
    if (error instanceof HttpErrorResponse && error.status === 409) {
      const conflictMessage = failureMessage(error, AccessAdminFacade.conflictMessage);
      await this.refreshSelectedUserAfterConflict();
      this.mutationMessageState.set({ text: conflictMessage, tone: 'notice' });
      return 'conflict';
    }

    const authFailure = await this.writeAuthFailureCoordinator.handle(error);
    if (authFailure) {
      if (authFailure.kind === 'forbidden' && !this.canAccess()) {
        this.clearProtectedState();
      }
      this.mutationMessageState.set({
        text: authFailure.message ?? AccessAdminFacade.accessDeniedMessage,
        tone: 'error',
      });
      return authFailure.kind;
    }

    this.mutationMessageState.set({
      text: failureMessage(error, AccessAdminFacade.writeErrorMessage),
      tone: 'error',
    });
    return error instanceof HttpErrorResponse && (error.status === 400 || error.status === 404)
      ? 'invalid'
      : 'error';
  }

  private async refreshAfterMutation(userId: number): Promise<void> {
    await Promise.all([
      this.selectUser(userId),
      this.loadUsers(),
      this.loadPermissionCatalogue(),
    ]);
  }

  private async refreshSelectedUserAfterConflict(): Promise<void> {
    const userId = this.selectedUserState()?.id;
    if (userId !== undefined) {
      await this.selectUser(userId);
    }
  }

  private permissionCodesForAssignment(): string[] {
    return this.canAssignPermissions() ? this.draft.codesForSubmission() : [];
  }

  private canSelectPermissions(): boolean {
    return (
      this.canAccess() &&
      this.canAssignPermissions() &&
      canSelectUserPermissions(this.selectedUserState())
    );
  }

  private canReplaceSelectedPermissions(): boolean {
    return (
      this.canAccess() &&
      canReplaceUserPermissions(this.selectedUserState(), this.canAssignPermissions())
    );
  }

  private clearProtectedState(): void {
    this.usersState.set(null);
    this.selectedUserState.set(null);
    this.draft.clear();
  }
}
