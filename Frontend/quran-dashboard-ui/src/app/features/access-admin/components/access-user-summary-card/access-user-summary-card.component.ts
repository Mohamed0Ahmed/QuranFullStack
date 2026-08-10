import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { AccessUserDetail } from '../../../../core/api/generated/models/access-user-detail';
import { ACCESS_ADMIN_LABELS } from '../../models/access-admin.labels';
import { accessLifecycleTone } from '../../models/access-admin.models';

@Component({
  selector: 'qd-access-user-summary-card',
  standalone: true,
  templateUrl: './access-user-summary-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'qd-flex qd-flex-col qd-gap-2' },
})
export class AccessUserSummaryCardComponent {
  readonly user = input.required<AccessUserDetail | null>();

  protected statusLabel(user: AccessUserDetail): string {
    return ACCESS_ADMIN_LABELS.userStatus(user.status);
  }

  protected lifecycleBadgeClass(user: AccessUserDetail): string {
    return `qd-badge qd-badge--status qd-badge--lifecycle-${accessLifecycleTone(user.status)}`;
  }
}
