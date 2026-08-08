import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';

import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { WriteAuthFailureCoordinator } from '../../../core/auth/write-auth-failure.coordinator';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { AccessUserDetail } from '../../../core/api/generated/models/access-user-detail';
import { AccessUserSummaryPagedResult } from '../../../core/api/generated/models/access-user-summary-paged-result';
import { LogtoSubjectRelinkPreview } from '../../../core/api/generated/models/logto-subject-relink-preview';
import { OwnerReconciliationStatus } from '../../../core/api/generated/models/owner-reconciliation-status';
import { AccessAdminApi } from '../data-access/access-admin.api';
import {
  AccessAuditQuery,
  AccessRelinkPreviewRequest,
  AccessUserListQuery,
  AccessUserSearchState,
  canReplaceUserPermissions,
  canSelectUserPermissions,
} from '../models/access-admin.models';
import { ACCESS_ADMIN_LOAD_ERROR, failureMessage } from './access-admin-request-failure';
import { AccessAuditStore } from './access-audit.store';
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
  private readonly audit = new AccessAuditStore(this.api);

  private readonly usersState = signal<AccessUserSummaryPagedResult | null>(null);
  private readonly userQueryState = signal<AccessUserListQuery>(defaultUserQuery);
  private readonly usersLoadingState = signal(false);
  private readonly usersErrorState = signal<string | null>(null);
  private readonly selectedUserState = signal<AccessUserDetail | null>(null);
  private readonly selectedUserLoadingState = signal(false);
  private readonly selectedUserErrorState = signal<string | null>(null);
  private readonly reconciliationState = signal<OwnerReconciliationStatus | null>(null);
  private readonly reconciliationLoadingState = signal(false);
  private readonly reconciliationErrorState = signal<string | null>(null);
  private readonly relinkPreviewState = signal<LogtoSubjectRelinkPreview | null>(null);
  private readonly relinkEvidenceTokenState = signal<string | null>(null);
  private readonly mutationMessageState = signal<AccessAdminMessage | null>(null);
  private readonly busyActionState = signal<string | null>(null);
  private usersRequestVersion = 0;
  private selectedUserRequestVersion = 0;
  private relinkPreviewRequestVersion = 0;

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
  readonly auditEvents = this.audit.events;
  readonly auditNextCursor = this.audit.nextCursor;
  readonly auditQuery = this.audit.query;
  readonly auditLoading = this.audit.loading;
  readonly auditError = this.audit.error;
  readonly reconciliationStatus = this.reconciliationState.asReadonly();
  readonly reconciliationLoading = this.reconciliationLoadingState.asReadonly();
  readonly reconciliationError = this.reconciliationErrorState.asReadonly();
  readonly relinkPreview = this.relinkPreviewState.asReadonly();
  readonly mutationMessage = this.mutationMessageState.asReadonly();
  readonly busyAction = this.busyActionState.asReadonly();
  readonly permissionDiff = this.draft.diff;
  readonly isDirty = this.draft.isDirty;

  async load(): Promise<void> {
    if (!this.canAccess()) {
      this.clearProtectedState();
      return;
    }

    await Promise.all([
      this.loadUsers(),
      this.loadPermissionCatalogue(),
      this.loadAuditEvents(),
      this.loadReconciliationStatus(),
    ]);
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

  async findUsers(search: string): Promise<AccessUserSearchState> {
    if (!this.canAccess()) {
      return { users: [], error: AccessAdminFacade.accessDeniedMessage, loading: false };
    }

    return this.audit.findUsers(search);
  }

  async selectUser(userId: number): Promise<void> {
    if (!this.canAccess()) {
      return;
    }

    const requestVersion = ++this.selectedUserRequestVersion;
    this.selectedUserLoadingState.set(true);
    this.selectedUserErrorState.set(null);
    this.mutationMessageState.set(null);
    this.invalidateRelinkPreviewRequest();
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

  clearMutationMessage(): void {
    this.mutationMessageState.set(null);
  }

  async acceptSelectedUser(reason: string): Promise<AccessAdminMutationOutcome> {
    const user = this.selectedUserState();
    const normalizedReason = reason.trim();
    if (!user || user.isOwner || user.status !== 'pending' || !normalizedReason) {
      return 'invalid';
    }

    return this.runMutation('accept', () =>
      this.api.acceptUser(user.id, {
        expectedVersion: user.version,
        permissionCodes: this.permissionCodesForAssignment(),
        reason: normalizedReason,
      }),
    );
  }

  async disableSelectedUser(reason: string): Promise<AccessAdminMutationOutcome> {
    const user = this.selectedUserState();
    const normalizedReason = reason.trim();
    if (!user || user.isOwner || user.status !== 'active' || !normalizedReason) {
      return 'invalid';
    }

    return this.runMutation('disable', () =>
      this.api.disableUser(user.id, { expectedVersion: user.version, reason: normalizedReason }),
    );
  }

  async reactivateSelectedUser(reason: string): Promise<AccessAdminMutationOutcome> {
    const user = this.selectedUserState();
    const normalizedReason = reason.trim();
    if (!user || user.isOwner || user.status !== 'disabled' || !normalizedReason) {
      return 'invalid';
    }

    return this.runMutation('reactivate', () =>
      this.api.reactivateUser(user.id, { expectedVersion: user.version, reason: normalizedReason }),
    );
  }

  async replaceSelectedPermissions(reason: string): Promise<AccessAdminMutationOutcome> {
    const user = this.selectedUserState();
    const permissions = this.draft.grantedPermissions();
    const normalizedReason = reason.trim();
    if (!user || !permissions || !normalizedReason || !this.canReplaceSelectedPermissions()) {
      return 'invalid';
    }

    return this.runMutation('permissions', () =>
      this.api.replacePermissions(user.id, {
        expectedVersion: permissions.version,
        permissionCodes: this.permissionCodesForAssignment(),
        reason: normalizedReason,
      }),
    );
  }

  async previewSelectedUserRelink(
    request: AccessRelinkPreviewRequest,
  ): Promise<AccessAdminMutationOutcome> {
    const user = this.selectedUserState();
    if (!this.canAccess() || !user || !request.newSub.trim() || !request.evidenceToken.trim()) {
      return 'invalid';
    }

    const requestVersion = ++this.relinkPreviewRequestVersion;
    const targetUserId = user.id;
    this.busyActionState.set('relink-preview');
    this.mutationMessageState.set(null);
    this.relinkPreviewState.set(null);
    this.relinkEvidenceTokenState.set(null);
    try {
      const response = await firstValueFrom(
        this.api.previewRelink(user.id, {
          newSub: request.newSub.trim(),
          evidenceToken: request.evidenceToken.trim(),
        }),
      );
      if (!this.isCurrentRelinkPreviewRequest(requestVersion, targetUserId)) {
        return 'invalid';
      }
      if (!response.isSuccess || !response.data) {
        this.mutationMessageState.set({
          text: response.message ?? AccessAdminFacade.writeErrorMessage,
          tone: 'error',
        });
        return 'invalid';
      }
      this.relinkPreviewState.set(response.data);
      this.relinkEvidenceTokenState.set(request.evidenceToken.trim());
      return 'success';
    } catch (error) {
      if (!this.isCurrentRelinkPreviewRequest(requestVersion, targetUserId)) {
        return 'invalid';
      }
      return this.handleMutationError(error);
    } finally {
      if (requestVersion === this.relinkPreviewRequestVersion && this.busyActionState() === 'relink-preview') {
        this.busyActionState.set(null);
      }
    }
  }

  async confirmSelectedUserRelink(reason: string): Promise<AccessAdminMutationOutcome> {
    const user = this.selectedUserState();
    const preview = this.relinkPreviewState();
    const evidenceToken = this.relinkEvidenceTokenState();
    const normalizedReason = reason.trim();
    if (!user || !preview || !evidenceToken || preview.userId !== user.id || !normalizedReason) {
      return 'invalid';
    }

    return this.runMutation('relink-confirm', () =>
      this.api.confirmRelink(user.id, {
        expectedVersion: preview.version,
        oldSub: preview.oldSub,
        newSub: preview.newSub,
        evidenceToken,
        reason: normalizedReason,
        confirmed: true,
      }),
    );
  }

  cancelSelectedUserRelink(): void {
    if (this.busyActionState()) {
      return;
    }
    this.invalidateRelinkPreviewRequest();
  }

  async updateAuditQuery(query: Partial<AccessAuditQuery>): Promise<void> {
    this.audit.applyQuery(query);
    await this.loadAuditEvents();
  }

  async loadNextAuditPage(): Promise<void> {
    if (!this.canAccess()) {
      return;
    }

    await this.audit.loadNextPage();
  }

  async loadAuditEvents(): Promise<void> {
    if (!this.canAccess()) {
      return;
    }

    await this.audit.load();
  }

  async loadReconciliationStatus(): Promise<void> {
    if (!this.canAccess()) {
      return;
    }

    this.reconciliationLoadingState.set(true);
    this.reconciliationErrorState.set(null);
    try {
      const response = await firstValueFrom(this.api.getOwnerReconciliationStatus());
      if (response.isSuccess && response.data) {
        this.reconciliationState.set(response.data);
        return;
      }
      this.reconciliationErrorState.set(response.message ?? ACCESS_ADMIN_LOAD_ERROR);
    } catch (error) {
      this.reconciliationErrorState.set(failureMessage(error, ACCESS_ADMIN_LOAD_ERROR));
    } finally {
      this.reconciliationLoadingState.set(false);
    }
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
    this.relinkPreviewState.set(null);
    this.relinkEvidenceTokenState.set(null);
    await Promise.all([
      this.selectUser(userId),
      this.loadUsers(),
      this.loadPermissionCatalogue(),
      this.loadAuditEvents(),
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

  private isCurrentRelinkPreviewRequest(requestVersion: number, targetUserId: number): boolean {
    return (
      requestVersion === this.relinkPreviewRequestVersion &&
      this.selectedUserState()?.id === targetUserId
    );
  }

  private invalidateRelinkPreviewRequest(): void {
    this.relinkPreviewRequestVersion += 1;
    this.relinkPreviewState.set(null);
    this.relinkEvidenceTokenState.set(null);
    if (this.busyActionState() === 'relink-preview') {
      this.busyActionState.set(null);
    }
  }

  private clearProtectedState(): void {
    this.usersState.set(null);
    this.selectedUserState.set(null);
    this.draft.clear();
    this.audit.clear();
    this.reconciliationState.set(null);
    this.invalidateRelinkPreviewRequest();
  }
}
