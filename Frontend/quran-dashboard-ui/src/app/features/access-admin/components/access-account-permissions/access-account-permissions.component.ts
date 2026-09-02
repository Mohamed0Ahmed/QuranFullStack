import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { AccessUserDetail } from '../../../../core/api/generated/models/access-user-detail';
import { PermissionCode } from '../../../../core/auth/permission-code';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { ACCESS_ADMIN_LABELS } from '../../models/access-admin.labels';
import { AccessPermissionDiff } from '../../models/access-admin.models';
import { AccessPermissionGroup } from '../../models/access-admin-permissions';
import { AccessPermissionEditorComponent } from '../access-permission-editor/access-permission-editor.component';

@Component({
  selector: 'qd-access-account-permissions',
  standalone: true,
  imports: [AccessPermissionEditorComponent, ExplorerPanelSkeletonComponent, QdErrorStateComponent],
  templateUrl: './access-account-permissions.component.html',
  styleUrl: './access-account-permissions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessAccountPermissionsComponent {
  readonly user = input.required<AccessUserDetail>();
  readonly mode = input.required<'owner' | 'permissions' | 'disabled' | 'unknown'>();
  readonly groups = input.required<readonly AccessPermissionGroup[]>();
  readonly selectedCodes = input.required<ReadonlySet<PermissionCode>>();
  readonly unknownPermissionCodes = input.required<readonly string[]>();
  readonly permissionDiff = input.required<AccessPermissionDiff>();
  readonly hasUnsavedPermissions = input(false);
  readonly catalogueLoading = input(false);
  readonly catalogueError = input<string | null>(null);
  readonly canAssignPermissions = input(false);
  readonly busyAction = input<string | null>(null);

  readonly selectionChange = output<PermissionCode[]>();
  readonly catalogueRetryRequested = output<void>();

  protected get labels(): typeof ACCESS_ADMIN_LABELS {
    return ACCESS_ADMIN_LABELS;
  }
}
