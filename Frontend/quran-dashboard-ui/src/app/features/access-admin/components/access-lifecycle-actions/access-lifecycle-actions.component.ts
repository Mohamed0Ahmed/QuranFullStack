import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { AccessUserDetail } from '../../../../core/api/generated/models/access-user-detail';
import { AccessUserLifecycleAction } from '../../models/access-admin.models';

@Component({
  selector: 'qd-access-lifecycle-actions',
  standalone: true,
  templateUrl: './access-lifecycle-actions.component.html',
  styleUrl: './access-lifecycle-actions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessLifecycleActionsComponent {
  readonly user = input.required<AccessUserDetail>();
  readonly busyAction = input<string | null>(null);
  readonly acceptGrantsPermissions = input(false);

  readonly actionRequested = output<AccessUserLifecycleAction>();

  protected requestAction(kind: AccessUserLifecycleAction): void {
    if (this.busyAction() || !this.isAvailable(kind)) {
      return;
    }
    this.actionRequested.emit(kind);
  }

  private isAvailable(kind: AccessUserLifecycleAction): boolean {
    const user = this.user();
    if (user.isOwner) {
      return false;
    }
    return (
      (kind === 'accept' && user.status === 'pending') ||
      (kind === 'disable' && user.status === 'active') ||
      (kind === 'reactivate' && user.status === 'disabled')
    );
  }
}
