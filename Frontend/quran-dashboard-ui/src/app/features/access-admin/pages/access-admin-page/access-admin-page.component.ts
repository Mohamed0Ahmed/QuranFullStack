import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';

import { isPermissionCode } from '../../../../core/auth/permission-code';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
import { AccessUserListComponent } from '../../components/access-user-list/access-user-list.component';
import {
  AccessUserWorkflowConfirmation,
  AccessUserWorkflowsComponent,
} from '../../components/access-user-workflows/access-user-workflows.component';
import { AccessUserListFilters } from '../../models/access-admin.models';
import { AccessAdminFacade, AccessAdminMutationOutcome } from '../../state/access-admin.facade';

@Component({
  selector: 'qd-access-admin-page',
  standalone: true,
  imports: [AccessUserListComponent, AccessUserWorkflowsComponent, QdStateComponent],
  templateUrl: './access-admin-page.component.html',
  styleUrl: './access-admin-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AccessAdminFacade],
})
export class AccessAdminPageComponent implements OnInit {
  protected readonly facade = inject(AccessAdminFacade);
  protected readonly workflowResetToken = signal(0);
  protected readonly auditTargetUserId = signal('');
  protected readonly auditActorUserId = signal('');
  protected readonly auditActionType = signal('');
  protected readonly auditPermissionCode = signal('');

  ngOnInit(): void {
    void this.facade.load();
  }

  protected selectUser(userId: number): void {
    void this.facade.selectUser(userId);
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

  protected confirmAction(action: AccessUserWorkflowConfirmation): void {
    void this.runAction(action);
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

  protected updateAuditTargetUserId(event: Event): void {
    this.auditTargetUserId.set((event.target as HTMLInputElement).value);
  }

  protected updateAuditActorUserId(event: Event): void {
    this.auditActorUserId.set((event.target as HTMLInputElement).value);
  }

  protected updateAuditActionType(event: Event): void {
    this.auditActionType.set((event.target as HTMLInputElement).value);
  }

  protected updateAuditPermissionCode(event: Event): void {
    this.auditPermissionCode.set((event.target as HTMLSelectElement).value);
  }

  protected applyAuditFilters(event: Event): void {
    event.preventDefault();
    const permissionCode = this.auditPermissionCode();
    void this.facade.updateAuditQuery({
      targetUserId: positiveUserId(this.auditTargetUserId()),
      actorUserId: positiveUserId(this.auditActorUserId()),
      actionType: this.auditActionType().trim() || undefined,
      permissionCode: isPermissionCode(permissionCode) ? permissionCode : undefined,
    });
  }

  protected auditActorTypeLabel(actorType: string): string {
    return actorType === 'User' ? 'مستخدم' : actorType === 'System' ? 'النظام' : actorType;
  }

  protected loadNextAuditPage(): void {
    void this.facade.loadNextAuditPage();
  }

  private async runAction(action: AccessUserWorkflowConfirmation): Promise<void> {
    const outcome =
      action.kind === 'accept'
        ? await this.facade.acceptSelectedUser(action.reason)
        : action.kind === 'disable'
          ? await this.facade.disableSelectedUser(action.reason)
          : action.kind === 'reactivate'
            ? await this.facade.reactivateSelectedUser(action.reason)
            : await this.facade.replaceSelectedPermissions(action.reason);
    this.resetWorkflowAfter(outcome);
  }

  private async runRelinkConfirmation(reason: string): Promise<void> {
    this.resetWorkflowAfter(await this.facade.confirmSelectedUserRelink(reason));
  }

  private resetWorkflowAfter(outcome: AccessAdminMutationOutcome): void {
    if (outcome === 'success' || outcome === 'conflict' || outcome === 'forbidden' || outcome === 'unauthorized') {
      this.workflowResetToken.update((value) => value + 1);
    }
  }
}

function positiveUserId(value: string): number | undefined {
  const userId = Number.parseInt(value.trim(), 10);
  return Number.isSafeInteger(userId) && userId > 0 ? userId : undefined;
}
