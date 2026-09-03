import { ChangeDetectionStrategy, Component, effect, input, output, signal } from '@angular/core';

import { PermissionCode } from '../../../../core/auth/permission-code';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdControlDirective } from '../../../../shared/ui/form-field/control.directive';
import { QdFormFieldComponent } from '../../../../shared/ui/form-field/form-field.component';
import { AccessPermissionDiff, AccessUserWorkflowAction } from '../../models/access-admin.models';
import { AccessPermissionGroup, permissionLabelFor } from '../../models/access-admin-permissions';

@Component({
  selector: 'qd-access-change-review',
  standalone: true,
  imports: [QdActionDirective, QdControlDirective, QdFormFieldComponent],
  templateUrl: './access-change-review.component.html',
  styleUrl: './access-change-review.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessChangeReviewComponent {
  readonly action = input.required<AccessUserWorkflowAction | null>();
  readonly groups = input.required<readonly AccessPermissionGroup[]>();
  readonly permissionDiff = input.required<AccessPermissionDiff>();
  readonly showsPermissionDiff = input(false);
  readonly canConfirm = input(false);
  readonly busyAction = input<string | null>(null);

  readonly confirmed = output<string>();
  readonly cancelled = output<void>();

  protected readonly reason = signal('');

  constructor() {
    effect(() => {
      this.action();
      this.reason.set('');
    });
  }

  protected permissionLabel(code: PermissionCode): string {
    return permissionLabelFor(this.groups(), code);
  }

  protected updateReason(event: Event): void {
    this.reason.set((event.target as HTMLTextAreaElement).value);
  }

  protected cancel(): void {
    if (this.busyAction()) {
      return;
    }
    this.cancelled.emit();
  }

  protected confirm(): void {
    if (!this.action() || this.busyAction() || !this.canConfirm()) {
      return;
    }
    this.confirmed.emit(this.reason().trim());
  }
}
