import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';

import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { PermissionCode, isPermissionCode } from '../../../core/auth/permission-code';
import { WriteAuthFailureCoordinator } from '../../../core/auth/write-auth-failure.coordinator';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { AccessAuditEventPage } from '../../../core/api/generated/models/access-audit-event-page';
import { AccessUserDetail } from '../../../core/api/generated/models/access-user-detail';
import { AccessUserPermissions } from '../../../core/api/generated/models/access-user-permissions';
import { AccessUserSummaryPagedResult } from '../../../core/api/generated/models/access-user-summary-paged-result';
import { LogtoSubjectRelinkPreview } from '../../../core/api/generated/models/logto-subject-relink-preview';
import { OwnerReconciliationStatus } from '../../../core/api/generated/models/owner-reconciliation-status';
import { PermissionCatalogueItem } from '../../../core/api/generated/models/permission-catalogue-item';
import { AccessAdminApi } from '../data-access/access-admin.api';
import {
  AccessAuditQuery,
  AccessPermissionDiff,
  AccessRelinkPreviewRequest,
  AccessUserListQuery,
} from '../models/access-admin.models';
import {
  AccessPermissionGroup,
  buildPermissionGroups,
  permissionCodesForSubmission,
} from '../models/access-admin-permissions';

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
const defaultAuditQuery: AccessAuditQuery = { pageSize: 25 };

@Injectable()
export class AccessAdminFacade {
  private static readonly accessDeniedMessage = 'لا تملك صلاحية إدارة الوصول.';
  private static readonly loadErrorMessage = 'تعذر تحميل بيانات إدارة الوصول.';
  private static readonly writeErrorMessage = 'تعذر إتمام التغيير المطلوب.';
  private static readonly mutationSuccessMessage = 'تم حفظ التغيير.';
  private static readonly conflictMessage = 'تغيرت بيانات المستخدم. تم تحديث الحالة الحالية.';

  private readonly api = inject(AccessAdminApi);
  private readonly currentUserStore = inject(CurrentUserStore);
  private readonly writeAuthFailureCoordinator = inject(WriteAuthFailureCoordinator);

  private readonly usersState = signal<AccessUserSummaryPagedResult | null>(null);
  private readonly userQueryState = signal<AccessUserListQuery>(defaultUserQuery);
  private readonly usersLoadingState = signal(false);
  private readonly usersErrorState = signal<string | null>(null);
  private readonly catalogueState = signal<readonly PermissionCatalogueItem[]>([]);
  private readonly assignmentReadyState = signal(false);
  private readonly catalogueLoadingState = signal(false);
  private readonly catalogueErrorState = signal<string | null>(null);
  private readonly selectedUserState = signal<AccessUserDetail | null>(null);
  private readonly selectedPermissionsState = signal<AccessUserPermissions | null>(null);
  private readonly selectedPermissionCodesState = signal<ReadonlySet<PermissionCode>>(new Set());
  private readonly selectedUserLoadingState = signal(false);
  private readonly selectedUserErrorState = signal<string | null>(null);
  private readonly auditState = signal<AccessAuditEventPage | null>(null);
  private readonly auditQueryState = signal<AccessAuditQuery>(defaultAuditQuery);
  private readonly auditLoadingState = signal(false);
  private readonly auditErrorState = signal<string | null>(null);
  private readonly reconciliationState = signal<OwnerReconciliationStatus | null>(null);
  private readonly reconciliationLoadingState = signal(false);
  private readonly reconciliationErrorState = signal<string | null>(null);
  private readonly relinkPreviewState = signal<LogtoSubjectRelinkPreview | null>(null);
  private readonly relinkEvidenceTokenState = signal<string | null>(null);
  private readonly mutationMessageState = signal<AccessAdminMessage | null>(null);
  private readonly busyActionState = signal<string | null>(null);
  private usersRequestVersion = 0;
  private selectedUserRequestVersion = 0;
  private auditRequestVersion = 0;
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
  readonly permissionGroups = computed<readonly AccessPermissionGroup[]>(() =>
    buildPermissionGroups(this.catalogueState()),
  );
  readonly assignmentReady = this.assignmentReadyState.asReadonly();
  readonly catalogueLoading = this.catalogueLoadingState.asReadonly();
  readonly catalogueError = this.catalogueErrorState.asReadonly();
  readonly canAssignPermissions = computed(
    () => this.assignmentReady() && !this.catalogueError() && this.permissionGroups().length > 0,
  );
  readonly selectedUser = this.selectedUserState.asReadonly();
  readonly selectedPermissions = this.selectedPermissionsState.asReadonly();
  readonly selectedPermissionCodes = this.selectedPermissionCodesState.asReadonly();
  readonly selectedUserLoading = this.selectedUserLoadingState.asReadonly();
  readonly selectedUserError = this.selectedUserErrorState.asReadonly();
  readonly auditEvents = computed(() => this.auditState()?.items ?? []);
  readonly auditNextCursor = computed(() => this.auditState()?.nextCursor ?? null);
  readonly auditQuery = this.auditQueryState.asReadonly();
  readonly auditLoading = this.auditLoadingState.asReadonly();
  readonly auditError = this.auditErrorState.asReadonly();
  readonly reconciliationStatus = this.reconciliationState.asReadonly();
  readonly reconciliationLoading = this.reconciliationLoadingState.asReadonly();
  readonly reconciliationError = this.reconciliationErrorState.asReadonly();
  readonly relinkPreview = this.relinkPreviewState.asReadonly();
  readonly mutationMessage = this.mutationMessageState.asReadonly();
  readonly busyAction = this.busyActionState.asReadonly();
  readonly permissionDiff = computed<AccessPermissionDiff>(() => {
    const current = knownCodes(this.selectedPermissionsState()?.permissionCodes ?? []);
    const next = new Set(permissionCodesForSubmission(this.catalogueState(), this.selectedPermissionCodesState()));
    return {
      granted: [...next].filter((code) => !current.has(code)),
      revoked: [...current].filter((code) => !next.has(code)),
    };
  });

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
      this.usersErrorState.set(response.message ?? AccessAdminFacade.loadErrorMessage);
    } catch (error) {
      if (requestVersion === this.usersRequestVersion) {
        this.usersErrorState.set(messageFrom(error, AccessAdminFacade.loadErrorMessage));
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

    this.catalogueLoadingState.set(true);
    this.catalogueErrorState.set(null);
    try {
      const response = await firstValueFrom(this.api.getPermissionCatalogue());
      if (response.isSuccess && response.data) {
        this.catalogueState.set(response.data.items);
        this.assignmentReadyState.set(response.data.assignmentReady);
        return;
      }
      this.assignmentReadyState.set(false);
      this.catalogueErrorState.set(response.message ?? AccessAdminFacade.loadErrorMessage);
    } catch (error) {
      this.assignmentReadyState.set(false);
      this.catalogueErrorState.set(messageFrom(error, AccessAdminFacade.loadErrorMessage));
    } finally {
      this.catalogueLoadingState.set(false);
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
        this.selectedUserErrorState.set(detailResponse.message ?? AccessAdminFacade.loadErrorMessage);
        return;
      }
      if (!permissionsResponse.isSuccess || !permissionsResponse.data) {
        this.selectedUserErrorState.set(permissionsResponse.message ?? AccessAdminFacade.loadErrorMessage);
        return;
      }

      this.selectedUserState.set(detailResponse.data);
      this.selectedPermissionsState.set(permissionsResponse.data);
      this.selectedPermissionCodesState.set(knownCodes(permissionsResponse.data.permissionCodes));
    } catch (error) {
      if (requestVersion === this.selectedUserRequestVersion) {
        this.selectedUserErrorState.set(messageFrom(error, AccessAdminFacade.loadErrorMessage));
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
    this.selectedPermissionCodesState.set(knownCodes([...codes]));
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
    const permissions = this.selectedPermissionsState();
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
    this.auditQueryState.set({ ...this.auditQueryState(), ...query, cursor: undefined });
    await this.loadAuditEvents();
  }

  async loadNextAuditPage(): Promise<void> {
    const cursor = this.auditState()?.nextCursor;
    if (!cursor) {
      return;
    }
    await this.loadAuditEvents({ ...this.auditQueryState(), cursor }, true);
  }

  async loadAuditEvents(query = this.auditQueryState(), append = false): Promise<void> {
    if (!this.canAccess()) {
      return;
    }

    const requestVersion = ++this.auditRequestVersion;
    this.auditLoadingState.set(true);
    this.auditErrorState.set(null);
    try {
      const response = await firstValueFrom(this.api.listAuditEvents(query));
      if (requestVersion !== this.auditRequestVersion) {
        return;
      }
      if (!response.isSuccess || !response.data) {
        this.auditErrorState.set(response.message ?? AccessAdminFacade.loadErrorMessage);
        return;
      }
      const items = append ? [...this.auditEvents(), ...response.data.items] : response.data.items;
      this.auditState.set({ ...response.data, items });
    } catch (error) {
      if (requestVersion === this.auditRequestVersion) {
        this.auditErrorState.set(messageFrom(error, AccessAdminFacade.loadErrorMessage));
      }
    } finally {
      if (requestVersion === this.auditRequestVersion) {
        this.auditLoadingState.set(false);
      }
    }
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
      this.reconciliationErrorState.set(response.message ?? AccessAdminFacade.loadErrorMessage);
    } catch (error) {
      this.reconciliationErrorState.set(messageFrom(error, AccessAdminFacade.loadErrorMessage));
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
      const conflictMessage = messageFrom(error, AccessAdminFacade.conflictMessage);
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
      text: messageFrom(error, AccessAdminFacade.writeErrorMessage),
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

  private permissionCodesForAssignment(): PermissionCode[] {
    return this.canAssignPermissions()
      ? permissionCodesForSubmission(this.catalogueState(), this.selectedPermissionCodesState())
      : [];
  }

  private canSelectPermissions(): boolean {
    const user = this.selectedUserState();
    return (
      this.canAccess() &&
      this.canAssignPermissions() &&
      user !== null &&
      !user.isOwner &&
      (user.status === 'pending' || user.status === 'active')
    );
  }

  private canReplaceSelectedPermissions(): boolean {
    const user = this.selectedUserState();
    return this.canSelectPermissions() && user?.status === 'active';
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
    this.selectedPermissionsState.set(null);
    this.selectedPermissionCodesState.set(new Set());
    this.auditState.set(null);
    this.reconciliationState.set(null);
    this.invalidateRelinkPreviewRequest();
  }
}

function knownCodes(codes: readonly string[]): Set<PermissionCode> {
  return new Set(codes.filter(isPermissionCode));
}

function messageFrom(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse) || typeof error.error !== 'object' || error.error === null) {
    return fallback;
  }
  const response = error.error as Partial<ApiResponse<unknown>>;
  return typeof response.message === 'string' && response.message.trim() ? response.message : fallback;
}
