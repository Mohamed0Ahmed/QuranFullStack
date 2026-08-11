import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnDestroy,
  OnInit,
  computed,
  effect,
  inject,
  linkedSignal,
  signal,
  untracked,
} from '@angular/core';

import { AccessUserDetail } from '../../../../core/api/generated/models/access-user-detail';
import { QD_BP_WIDE_QUERY } from '../../../../shared/layout/breakpoints';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { ConfirmDialogComponent } from '../../../../shared/ui/confirm-dialog/confirm-dialog.component';
import { QdDetailsWorkspaceComponent } from '../../../../shared/ui/details-workspace/details-workspace.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { QdControlDirective } from '../../../../shared/ui/form-field/control.directive';
import { QdFormFieldComponent } from '../../../../shared/ui/form-field/form-field.component';
import { QdModalShellComponent } from '../../../../shared/ui/modal-shell/modal-shell.component';
import { QdNoticeComponent } from '../../../../shared/ui/notice/notice.component';
import { AccessAccountPermissionsComponent } from '../../components/access-account-permissions/access-account-permissions.component';
import { AccessChangeReviewComponent } from '../../components/access-change-review/access-change-review.component';
import { AccessLifecycleActionsComponent } from '../../components/access-lifecycle-actions/access-lifecycle-actions.component';
import { AccessUserListComponent } from '../../components/access-user-list/access-user-list.component';
import { AccessUserSummaryCardComponent } from '../../components/access-user-summary-card/access-user-summary-card.component';
import { ACCESS_ADMIN_LABELS } from '../../models/access-admin.labels';
import {
  AccessUserListFilters,
  AccessUserLifecycleAction,
  AccessUserWorkflowAction,
  acceptGrantsPermissions,
  accessLifecycleActionsApply,
  accessUserNameLabel,
  canReplaceUserPermissions,
} from '../../models/access-admin.models';
import { AccessAdminFacade, AccessAdminMutationOutcome } from '../../state/access-admin.facade';

@Component({
  selector: 'qd-access-admin-page',
  standalone: true,
  imports: [
    AccessAccountPermissionsComponent,
    AccessChangeReviewComponent,
    AccessLifecycleActionsComponent,
    AccessUserListComponent,
    AccessUserSummaryCardComponent,
    ConfirmDialogComponent,
    ExplorerPanelSkeletonComponent,
    NgTemplateOutlet,
    QdActionDirective,
    QdControlDirective,
    QdDetailsWorkspaceComponent,
    QdErrorStateComponent,
    QdFormFieldComponent,
    QdModalShellComponent,
    QdNoticeComponent,
  ],
  templateUrl: './access-admin-page.component.html',
  styleUrl: './access-admin-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AccessAdminFacade],
})
export class AccessAdminPageComponent implements OnInit, OnDestroy {
  protected readonly facade = inject(AccessAdminFacade);

  protected readonly userSwitchAwaitingDiscard = signal<number | null>(null);
  protected readonly routeLeaveAwaitingDecision = signal(false);
  protected readonly pendingAction = signal<AccessUserWorkflowAction | null>(null);
  protected readonly userListSheetOpen = signal(false);
  protected readonly isWide = signal(true);
  protected readonly contextSearch = linkedSignal(() => this.facade.userQuery().search ?? '');

  protected readonly lifecycleActionsApply = computed(() =>
    accessLifecycleActionsApply(this.facade.selectedUser()),
  );
  protected readonly selectedIdentity = computed(() => {
    const user = this.facade.selectedUser();
    return user === null ? '' : accessUserNameLabel(user);
  });
  protected readonly detailLayout = computed(() =>
    this.facade.selectedUser() !== null ||
    this.facade.selectedUserLoading() ||
    this.facade.selectedUserError() !== null
      ? 'selection'
      : 'no-selection',
  );

  private wideQuery?: MediaQueryList;
  private readonly onWideChange = (event: MediaQueryListEvent): void => this.isWide.set(event.matches);
  private routeLeaveDecision: Promise<boolean> | null = null;
  private routeLeaveResolver: ((allowed: boolean) => void) | null = null;

  constructor() {
    effect(() => {
      if (this.facade.canAccess()) {
        untracked(() => void this.facade.load());
      }
    });

    inject(DestroyRef).onDestroy(() => this.settleRouteLeave(false));
  }

  ngOnInit(): void {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
      return;
    }
    this.wideQuery = window.matchMedia(QD_BP_WIDE_QUERY);
    this.isWide.set(this.wideQuery.matches);
    this.wideQuery.addEventListener('change', this.onWideChange);
  }

  ngOnDestroy(): void {
    this.wideQuery?.removeEventListener('change', this.onWideChange);
  }

  protected get labels(): typeof ACCESS_ADMIN_LABELS {
    return ACCESS_ADMIN_LABELS;
  }

  hasUnsavedChanges(): boolean {
    return this.facade.isDirty();
  }

  confirmRouteLeave(): Promise<boolean> {
    if (!this.hasUnsavedChanges()) {
      return Promise.resolve(true);
    }
    if (this.routeLeaveDecision !== null) {
      return this.routeLeaveDecision;
    }
    this.routeLeaveDecision = new Promise<boolean>((resolve) => {
      this.routeLeaveResolver = resolve;
    });
    this.routeLeaveAwaitingDecision.set(true);
    return this.routeLeaveDecision;
  }

  protected allowRouteLeave(): void {
    this.settleRouteLeave(true);
  }

  protected cancelRouteLeave(): void {
    this.settleRouteLeave(false);
  }

  private settleRouteLeave(allowed: boolean): void {
    const resolve = this.routeLeaveResolver;
    this.routeLeaveResolver = null;
    this.routeLeaveDecision = null;
    this.routeLeaveAwaitingDecision.set(false);
    resolve?.(allowed);
  }

  protected canReplacePermissions(user: AccessUserDetail | null): boolean {
    return canReplaceUserPermissions(user, this.facade.canAssignPermissions());
  }

  protected acceptWouldGrantPermissions(): boolean {
    return acceptGrantsPermissions(this.facade.canAssignPermissions(), this.facade.permissionDiff());
  }

  protected openUserListSheet(): void {
    this.userListSheetOpen.set(true);
  }

  protected updateContextSearch(event: Event): void {
    this.contextSearch.set((event.target as HTMLInputElement).value);
  }

  protected applyContextSearch(event: Event): void {
    event.preventDefault();
    const search = this.contextSearch().trim();
    void this.facade.updateUserQuery({ search: search || undefined });
    this.openUserListSheet();
  }

  protected closeUserListSheet(): void {
    this.userListSheetOpen.set(false);
  }

  protected selectUser(userId: number): void {
    if (this.facade.isDirty()) {
      this.userSwitchAwaitingDiscard.set(userId);
      return;
    }
    this.pendingAction.set(null);
    this.closeUserListSheet();
    void this.facade.selectUser(userId);
  }

  protected discardDraftAndSwitchUser(): void {
    const userId = this.userSwitchAwaitingDiscard();
    this.userSwitchAwaitingDiscard.set(null);
    if (userId === null) {
      return;
    }
    this.pendingAction.set(null);
    this.closeUserListSheet();
    this.facade.discardDraft();
    void this.facade.selectUser(userId);
  }

  protected keepEditingDraft(): void {
    this.userSwitchAwaitingDiscard.set(null);
  }

  protected discardDraft(): void {
    this.facade.discardDraft();
  }

  protected updateUsers(filters: AccessUserListFilters): void {
    void this.facade.updateUserQuery(filters);
  }

  protected updateUserPage(page: number): void {
    void this.facade.updateUserQuery({ page });
  }

  protected updatePermissionCodes(codes: string[]): void {
    this.facade.setSelectedPermissionCodes(new Set(codes));
  }

  protected reloadPermissionCatalogue(): void {
    void this.facade.loadPermissionCatalogue();
  }

  protected requestLifecycleAction(kind: AccessUserLifecycleAction): void {
    this.pendingAction.set(kind);
  }

  protected requestPermissionSave(): void {
    if (this.facade.busyAction()) {
      return;
    }
    this.pendingAction.set('permissions');
  }

  protected cancelPendingAction(): void {
    this.pendingAction.set(null);
  }

  protected confirmPendingAction(reason: string): void {
    void this.runAction(reason);
  }

  private async runAction(reason: string): Promise<void> {
    const kind = this.pendingAction();
    if (!kind) {
      return;
    }
    const outcome =
      kind === 'accept'
        ? await this.facade.acceptSelectedUser(reason)
        : kind === 'disable'
          ? await this.facade.disableSelectedUser(reason)
          : kind === 'reactivate'
            ? await this.facade.reactivateSelectedUser(reason)
            : await this.facade.replaceSelectedPermissions(reason);
    this.resetWorkflowAfter(outcome);
  }

  private resetWorkflowAfter(outcome: AccessAdminMutationOutcome): void {
    if (outcome === 'success' || outcome === 'conflict' || outcome === 'forbidden' || outcome === 'unauthorized') {
      this.pendingAction.set(null);
    }
  }
}
