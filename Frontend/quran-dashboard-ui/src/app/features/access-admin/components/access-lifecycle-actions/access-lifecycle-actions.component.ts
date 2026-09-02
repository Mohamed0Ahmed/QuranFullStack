import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import {
  AccessUserLifecycleAction,
  AccessUserWorkflowAction,
} from '../../models/access-admin.models';

@Component({
  selector: 'qd-access-lifecycle-actions',
  standalone: true,
  imports: [QdActionDirective],
  templateUrl: './access-lifecycle-actions.component.html',
  styleUrl: './access-lifecycle-actions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessLifecycleActionsComponent {
  readonly actions = input.required<readonly AccessUserWorkflowAction[]>();
  readonly busyAction = input<string | null>(null);
  readonly acceptGrantsPermissions = input(false);

  readonly actionRequested = output<AccessUserLifecycleAction>();

  protected requestAction(kind: AccessUserLifecycleAction): void {
    if (this.busyAction() || !this.actions().includes(kind)) {
      return;
    }
    this.actionRequested.emit(kind);
  }
}
