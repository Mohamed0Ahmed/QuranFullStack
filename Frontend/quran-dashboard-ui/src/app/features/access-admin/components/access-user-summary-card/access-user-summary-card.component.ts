import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { AccessUserDetail } from '../../../../core/api/generated/models/access-user-detail';
import { ACCESS_ADMIN_LABELS } from '../../models/access-admin.labels';
import { accessUserNameLabel } from '../../models/access-admin.models';

@Component({
  selector: 'qd-access-user-summary-card',
  standalone: true,
  templateUrl: './access-user-summary-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'explorer-panel-header' },
})
export class AccessUserSummaryCardComponent {
  readonly user = input.required<AccessUserDetail | null>();

  protected nameLabel(user: AccessUserDetail): string {
    return accessUserNameLabel(user);
  }

  protected statusLabel(user: AccessUserDetail): string {
    return ACCESS_ADMIN_LABELS.userStatus(user.status);
  }
}
