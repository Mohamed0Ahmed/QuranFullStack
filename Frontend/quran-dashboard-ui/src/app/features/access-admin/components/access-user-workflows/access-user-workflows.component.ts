import { ChangeDetectionStrategy, Component, effect, input, output, signal } from '@angular/core';

import { AccessUserDetail } from '../../../../core/api/generated/models/access-user-detail';
import { PermissionCode } from '../../../../core/auth/permission-code';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
import { ACCESS_ADMIN_LABELS } from '../../models/access-admin.labels';
import {
  AccessPermissionDiff,
  accessUserNameLabel,
  hasPermissionChanges,
} from '../../models/access-admin.models';
import { AccessPermissionGroup, permissionLabelFor } from '../../models/access-admin-permissions';
import { AccessPermissionEditorComponent } from '../access-permission-editor/access-permission-editor.component';

export type AccessUserWorkflowAction = 'accept' | 'disable' | 'reactivate' | 'permissions';

export interface AccessUserWorkflowConfirmation {
  readonly kind: AccessUserWorkflowAction;
  readonly reason: string;
}

@Component({
  selector: 'qd-access-user-workflows',
  standalone: true,
  imports: [AccessPermissionEditorComponent, QdStateComponent],
  templateUrl: './access-user-workflows.component.html',
  styleUrl: './access-user-workflows.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessUserWorkflowsComponent {
  readonly user = input.required<AccessUserDetail | null>();
  readonly groups = input.required<readonly AccessPermissionGroup[]>();
  readonly selectedCodes = input.required<ReadonlySet<PermissionCode>>();
  readonly permissionDiff = input.required<AccessPermissionDiff>();
  readonly busyAction = input<string | null>(null);
  readonly resetToken = input(0);
  readonly catalogueLoading = input(false);
  readonly catalogueError = input<string | null>(null);
  readonly canAssignPermissions = input(false);

  readonly permissionCodesChange = output<PermissionCode[]>();
  readonly draftDiscarded = output<void>();
  readonly actionConfirmed = output<AccessUserWorkflowConfirmation>();
  readonly catalogueRetryRequested = output<void>();

  protected readonly pendingAction = signal<AccessUserWorkflowAction | null>(null);
  protected readonly actionReason = signal('');

  constructor() {
    effect(() => {
      this.user()?.id;
      this.resetToken();
      this.resetWorkflow();
    });
  }

  protected canSelectPermissions(): boolean {
    const user = this.user();
    return user !== null && !user.isOwner && (user.status === 'pending' || user.status === 'active');
  }

  protected canDisable(): boolean {
    return this.isActiveNonOwner();
  }

  protected canReplacePermissions(): boolean {
    return this.isActiveNonOwner() && this.canAssignPermissions();
  }

  protected hasUnsavedPermissions(): boolean {
    return this.canAssignPermissions() && hasPermissionChanges(this.permissionDiff());
  }

  protected acceptGrantsPermissions(): boolean {
    return this.canAssignPermissions() && this.permissionDiff().granted.length > 0;
  }

  protected showsPermissionDiff(): boolean {
    const action = this.pendingAction();
    if (action === 'permissions') {
      return this.canAssignPermissions();
    }
    return action === 'accept' && this.acceptGrantsPermissions();
  }

  protected diffSummaryLabel(): string {
    const diff = this.permissionDiff();
    return ACCESS_ADMIN_LABELS.permissionDiffSummary(diff.granted.length, diff.revoked.length);
  }

  protected isNoOpPermissionSave(): boolean {
    return this.pendingAction() === 'permissions' && !this.hasUnsavedPermissions();
  }

  protected discardDraft(): void {
    if (this.busyAction()) {
      return;
    }
    this.draftDiscarded.emit();
  }

  protected requestAction(kind: AccessUserWorkflowAction): void {
    if (this.busyAction() || !this.isActionAvailable(kind)) {
      return;
    }
    this.pendingAction.set(kind);
    this.actionReason.set('');
  }

  protected cancelAction(): void {
    if (!this.busyAction()) {
      this.pendingAction.set(null);
      this.actionReason.set('');
    }
  }

  protected updateActionReason(event: Event): void {
    this.actionReason.set((event.target as HTMLTextAreaElement).value);
  }

  protected confirmAction(): void {
    const kind = this.pendingAction();
    const reason = this.actionReason().trim();
    if (!kind || !reason || this.busyAction() || this.isNoOpPermissionSave()) {
      return;
    }
    this.actionConfirmed.emit({ kind, reason });
  }

  protected permissionLabel(code: PermissionCode): string {
    return permissionLabelFor(this.groups(), code);
  }

  protected nameLabel(user: AccessUserDetail): string {
    return accessUserNameLabel(user);
  }

  protected statusLabel(status: string): string {
    if (status === 'active') {
      return 'نشط';
    }
    if (status === 'pending') {
      return 'معلّق';
    }
    return 'معطّل';
  }

  private isActiveNonOwner(): boolean {
    const user = this.user();
    return user !== null && !user.isOwner && user.status === 'active';
  }

  private isActionAvailable(kind: AccessUserWorkflowAction): boolean {
    const user = this.user();
    if (!user || user.isOwner) {
      return false;
    }
    return (
      (kind === 'accept' && user.status === 'pending') ||
      (kind === 'disable' && user.status === 'active') ||
      (kind === 'reactivate' && user.status === 'disabled') ||
      (kind === 'permissions' && this.canReplacePermissions())
    );
  }

  private resetWorkflow(): void {
    this.pendingAction.set(null);
    this.actionReason.set('');
  }
}
