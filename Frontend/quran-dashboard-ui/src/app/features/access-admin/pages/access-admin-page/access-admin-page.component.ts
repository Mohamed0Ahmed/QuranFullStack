import { ChangeDetectionStrategy, Component, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';

import { AccessUserDetail } from '../../../../core/api/generated/models/access-user-detail';
import { ConfirmDialogComponent } from '../../../../shared/ui/confirm-dialog/confirm-dialog.component';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { AccessAdvancedSecurityComponent } from '../../components/access-advanced-security/access-advanced-security.component';
import {
  AccessAuditFilters,
  AccessAuditLogComponent,
} from '../../components/access-audit-log/access-audit-log.component';
import { AccessChangeReviewComponent } from '../../components/access-change-review/access-change-review.component';
import { AccessLifecycleActionsComponent } from '../../components/access-lifecycle-actions/access-lifecycle-actions.component';
import { AccessPermissionEditorComponent } from '../../components/access-permission-editor/access-permission-editor.component';
import { AccessUserListComponent } from '../../components/access-user-list/access-user-list.component';
import { AccessUserSummaryCardComponent } from '../../components/access-user-summary-card/access-user-summary-card.component';
import { ACCESS_ADMIN_LABELS } from '../../models/access-admin.labels';
import {
  AccessUserListFilters,
  AccessUserLifecycleAction,
  AccessUserWorkflowAction,
  acceptGrantsPermissions,
  canReplaceUserPermissions,
  canSelectUserPermissions,
} from '../../models/access-admin.models';
import {
  ACCESS_ADMIN_TAB_KEYS,
  AccessAdminTab,
  DEFAULT_ACCESS_ADMIN_TAB,
  parseAccessAdminTab,
} from '../../models/access-admin-tabs';
import { AccessAdminFacade, AccessAdminMutationOutcome } from '../../state/access-admin.facade';

@Component({
  selector: 'qd-access-admin-page',
  standalone: true,
  imports: [
    AccessAdvancedSecurityComponent,
    AccessAuditLogComponent,
    AccessChangeReviewComponent,
    AccessLifecycleActionsComponent,
    AccessPermissionEditorComponent,
    AccessUserListComponent,
    AccessUserSummaryCardComponent,
    ConfirmDialogComponent,
    QdStateComponent,
    QdTabDirective,
    QdTabsComponent,
  ],
  templateUrl: './access-admin-page.component.html',
  styleUrl: './access-admin-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AccessAdminFacade],
})
export class AccessAdminPageComponent {
  protected readonly facade = inject(AccessAdminFacade);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly activeTab = signal<AccessAdminTab>(DEFAULT_ACCESS_ADMIN_TAB);
  protected readonly workflowResetToken = signal(0);
  protected readonly userSwitchAwaitingDiscard = signal<number | null>(null);
  protected readonly pendingAction = signal<AccessUserWorkflowAction | null>(null);

  constructor() {
    this.route.queryParamMap
      .pipe(takeUntilDestroyed())
      .subscribe((params) => this.showTab(parseAccessAdminTab(params.get('tab'))));

    effect(() => {
      if (this.facade.canAccess()) {
        untracked(() => void this.facade.load());
      }
    });
  }

  protected get labels(): typeof ACCESS_ADMIN_LABELS {
    return ACCESS_ADMIN_LABELS;
  }

  protected get tabKeys(): readonly AccessAdminTab[] {
    return ACCESS_ADMIN_TAB_KEYS;
  }

  hasUnsavedChanges(): boolean {
    return this.facade.isDirty();
  }

  protected selectTab(tab: AccessAdminTab): void {
    if (tab === this.activeTab()) {
      return;
    }
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { tab: tab === DEFAULT_ACCESS_ADMIN_TAB ? null : tab },
      queryParamsHandling: 'merge',
    });
  }

  private showTab(tab: AccessAdminTab): void {
    if (tab === this.activeTab()) {
      return;
    }
    this.activeTab.set(tab);
    this.facade.clearMutationMessage();
  }

  protected canSelectPermissions(user: AccessUserDetail): boolean {
    return canSelectUserPermissions(user);
  }

  protected canReplacePermissions(user: AccessUserDetail): boolean {
    return canReplaceUserPermissions(user, this.facade.canAssignPermissions());
  }

  protected acceptWouldGrantPermissions(): boolean {
    return acceptGrantsPermissions(this.facade.canAssignPermissions(), this.facade.permissionDiff());
  }

  protected selectUser(userId: number): void {
    if (this.facade.isDirty()) {
      this.userSwitchAwaitingDiscard.set(userId);
      return;
    }
    this.pendingAction.set(null);
    void this.facade.selectUser(userId);
  }

  protected discardDraftAndSwitchUser(): void {
    const userId = this.userSwitchAwaitingDiscard();
    this.userSwitchAwaitingDiscard.set(null);
    if (userId === null) {
      return;
    }
    this.pendingAction.set(null);
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

  protected previewRelink(request: { newSub: string; evidenceToken: string }): void {
    void this.facade.previewSelectedUserRelink(request);
  }

  protected confirmRelink(reason: string): void {
    void this.runRelinkConfirmation(reason);
  }

  protected cancelRelink(): void {
    this.facade.cancelSelectedUserRelink();
    this.workflowResetToken.update((value) => value + 1);
  }

  protected applyAuditFilters(filters: AccessAuditFilters): void {
    void this.facade.updateAuditQuery(filters);
  }

  protected loadNextAuditPage(): void {
    void this.facade.loadNextAuditPage();
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

  private async runRelinkConfirmation(reason: string): Promise<void> {
    this.resetWorkflowAfter(await this.facade.confirmSelectedUserRelink(reason));
  }

  private resetWorkflowAfter(outcome: AccessAdminMutationOutcome): void {
    if (outcome === 'success' || outcome === 'conflict' || outcome === 'forbidden' || outcome === 'unauthorized') {
      this.pendingAction.set(null);
      this.workflowResetToken.update((value) => value + 1);
    }
  }
}
