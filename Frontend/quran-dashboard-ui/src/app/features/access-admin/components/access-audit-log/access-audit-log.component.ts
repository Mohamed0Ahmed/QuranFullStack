import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';

import { AccessAuditEventItem } from '../../../../core/api/generated/models/access-audit-event-item';
import { AccessUserSummary } from '../../../../core/api/generated/models/access-user-summary';
import { PermissionCode, isPermissionCode } from '../../../../core/auth/permission-code';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
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
  imports: [AccessUserPickerComponent, DatePipe, QdStateComponent],
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
