import { ChangeDetectionStrategy, Component, effect, input, output, signal } from '@angular/core';

import { AccessUserDetail } from '../../../../core/api/generated/models/access-user-detail';
import { LogtoSubjectRelinkPreview } from '../../../../core/api/generated/models/logto-subject-relink-preview';
import { PermissionCode } from '../../../../core/auth/permission-code';
import { AccessPermissionDiff, AccessRelinkPreviewRequest } from '../../models/access-admin.models';
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
  imports: [AccessPermissionEditorComponent],
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
  readonly relinkPreview = input<LogtoSubjectRelinkPreview | null>(null);
  readonly resetToken = input(0);

  readonly permissionCodesChange = output<PermissionCode[]>();
  readonly actionConfirmed = output<AccessUserWorkflowConfirmation>();
  readonly relinkPreviewRequested = output<AccessRelinkPreviewRequest>();
  readonly relinkConfirmed = output<string>();
  readonly relinkCancelled = output<void>();

  protected readonly pendingAction = signal<AccessUserWorkflowAction | null>(null);
  protected readonly actionReason = signal('');
  protected readonly newSub = signal('');
  protected readonly evidenceToken = signal('');
  protected readonly relinkReason = signal('');
  protected readonly relinkConfirmation = signal(false);

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

  protected canReplacePermissions(): boolean {
    return this.user()?.status === 'active' && !this.user()?.isOwner;
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
    if (!kind || !reason || this.busyAction()) {
      return;
    }
    this.actionConfirmed.emit({ kind, reason });
  }

  protected permissionLabel(code: PermissionCode): string {
    return permissionLabelFor(this.groups(), code);
  }

  protected updateNewSub(event: Event): void {
    this.newSub.set((event.target as HTMLInputElement).value);
  }

  protected updateEvidenceToken(event: Event): void {
    this.evidenceToken.set((event.target as HTMLInputElement).value);
  }

  protected requestRelinkPreview(): void {
    const newSub = this.newSub().trim();
    const evidenceToken = this.evidenceToken().trim();
    if (!newSub || !evidenceToken || this.busyAction()) {
      return;
    }
    this.relinkPreviewRequested.emit({ newSub, evidenceToken });
    this.evidenceToken.set('');
  }

  protected updateRelinkReason(event: Event): void {
    this.relinkReason.set((event.target as HTMLTextAreaElement).value);
  }

  protected updateRelinkConfirmation(event: Event): void {
    this.relinkConfirmation.set((event.target as HTMLInputElement).checked);
  }

  protected confirmRelink(): void {
    if (!this.relinkPreview() || !this.relinkReason().trim() || !this.relinkConfirmation() || this.busyAction()) {
      return;
    }
    this.relinkConfirmed.emit(this.relinkReason().trim());
  }

  protected cancelRelink(): void {
    if (this.busyAction()) {
      return;
    }
    this.resetRelinkForm();
    this.relinkCancelled.emit();
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
      (kind === 'permissions' && user.status === 'active')
    );
  }

  private resetWorkflow(): void {
    this.pendingAction.set(null);
    this.actionReason.set('');
    this.resetRelinkForm();
  }

  private resetRelinkForm(): void {
    this.newSub.set('');
    this.evidenceToken.set('');
    this.relinkReason.set('');
    this.relinkConfirmation.set(false);
  }
}
