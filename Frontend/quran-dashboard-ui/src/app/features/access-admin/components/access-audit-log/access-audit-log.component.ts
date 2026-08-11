import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';

import { AccessAuditEventItem } from '../../../../core/api/generated/models/access-audit-event-item';
import { AccessUserSummary } from '../../../../core/api/generated/models/access-user-summary';
import { PermissionCode, isPermissionCode } from '../../../../core/auth/permission-code';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdControlDirective } from '../../../../shared/ui/form-field/control.directive';
import { QdFormFieldComponent } from '../../../../shared/ui/form-field/form-field.component';
import {
  QdResultItemDirective,
  QdResultListDirective,
} from '../../../../shared/ui/result-list/result-list.directive';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { ACCESS_ADMIN_LABELS } from '../../models/access-admin.labels';
import { AccessPermissionGroup } from '../../models/access-admin-permissions';
import {
  ACCESS_AUDIT_ACTION_TYPES,
  AccessUserSearchState,
  EMPTY_ACCESS_USER_SEARCH,
  accessUserNameLabel,
  isAccessAuditActionType,
} from '../../models/access-admin.models';
import { AccessUserPickerComponent } from '../access-user-picker/access-user-picker.component';

export interface AccessAuditFilters {
  readonly targetUserId?: number;
  readonly actorUserId?: number;
  readonly actionType?: string;
  readonly permissionCode?: PermissionCode;
}

@Component({
  selector: 'qd-access-audit-log',
  standalone: true,
  imports: [
    AccessUserPickerComponent,
    DatePipe,
    QdActionDirective,
    QdControlDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QdFormFieldComponent,
    QdResultItemDirective,
    QdResultListDirective,
    QdSkeletonRowsComponent,
  ],
  templateUrl: './access-audit-log.component.html',
  styleUrl: './access-audit-log.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessAuditLogComponent {
  readonly events = input.required<readonly AccessAuditEventItem[]>();
  readonly permissionGroups = input.required<readonly AccessPermissionGroup[]>();
  readonly loading = input(false);
  readonly error = input<string | null>(null);
  readonly hasNextPage = input(false);
  readonly appending = input(false);
  readonly appendError = input<string | null>(null);
  readonly appendedCount = input(0);
  readonly targetSearch = input<AccessUserSearchState>(EMPTY_ACCESS_USER_SEARCH);
  readonly actorSearch = input<AccessUserSearchState>(EMPTY_ACCESS_USER_SEARCH);

  readonly filtersApplied = output<AccessAuditFilters>();
  readonly nextPageRequested = output<void>();
  readonly targetSearchRequested = output<string>();
  readonly actorSearchRequested = output<string>();

  protected readonly targetUser = signal<AccessUserSummary | null>(null);
  protected readonly actorUser = signal<AccessUserSummary | null>(null);
  protected readonly actionType = signal('');
  protected readonly permissionCode = signal('');

  protected readonly skeletonRowCount = 6;
  protected readonly skeletonRowTemplate = 'minmax(0, 1fr) auto';

  protected readonly appendAnnouncement = computed(() =>
    ACCESS_ADMIN_LABELS.auditAppendedAnnouncement(this.appendedCount()),
  );

  protected get actionTypes(): readonly string[] {
    return ACCESS_AUDIT_ACTION_TYPES;
  }

  protected selectTargetUser(candidate: AccessUserSummary | null): void {
    this.targetUser.set(candidate);
  }

  protected selectActorUser(candidate: AccessUserSummary | null): void {
    this.actorUser.set(candidate);
  }

  protected updateActionType(event: Event): void {
    this.actionType.set((event.target as HTMLSelectElement).value);
  }

  protected updatePermissionCode(event: Event): void {
    this.permissionCode.set((event.target as HTMLSelectElement).value);
  }

  protected applyFilters(event: Event): void {
    event.preventDefault();
    const permissionCode = this.permissionCode();
    const actionType = this.actionType();
    this.filtersApplied.emit({
      targetUserId: this.targetUser()?.id,
      actorUserId: this.actorUser()?.id,
      actionType: isAccessAuditActionType(actionType) ? actionType : undefined,
      permissionCode: isPermissionCode(permissionCode) ? permissionCode : undefined,
    });
  }

  protected actionLabel(actionType: string): string {
    return ACCESS_ADMIN_LABELS.auditActionType(actionType);
  }

  protected targetLabel(event: AccessAuditEventItem): string {
    return participantLabel(event.targetDisplayName, event.targetEmail);
  }

  protected actorLabel(event: AccessAuditEventItem): string {
    return event.actorType === 'System'
      ? ACCESS_ADMIN_LABELS.systemActor
      : participantLabel(event.actorDisplayName, event.actorEmail);
  }
}

function participantLabel(displayName: string | null, email: string | null): string {
  return email ? accessUserNameLabel({ displayName, email }) : ACCESS_ADMIN_LABELS.unnamedParticipant;
}
