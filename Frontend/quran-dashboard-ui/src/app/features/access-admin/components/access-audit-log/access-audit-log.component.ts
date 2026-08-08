import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';

import { AccessAuditEventItem } from '../../../../core/api/generated/models/access-audit-event-item';
import { PermissionCode, isPermissionCode } from '../../../../core/auth/permission-code';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
import { AccessPermissionGroup } from '../../models/access-admin-permissions';

export interface AccessAuditFilters {
  readonly targetUserId?: number;
  readonly actorUserId?: number;
  readonly actionType?: string;
  readonly permissionCode?: PermissionCode;
}

@Component({
  selector: 'qd-access-audit-log',
  standalone: true,
  imports: [QdStateComponent],
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

  readonly filtersApplied = output<AccessAuditFilters>();
  readonly nextPageRequested = output<void>();

  protected readonly targetUserId = signal('');
  protected readonly actorUserId = signal('');
  protected readonly actionType = signal('');
  protected readonly permissionCode = signal('');

  protected updateTargetUserId(event: Event): void {
    this.targetUserId.set((event.target as HTMLInputElement).value);
  }

  protected updateActorUserId(event: Event): void {
    this.actorUserId.set((event.target as HTMLInputElement).value);
  }

  protected updateActionType(event: Event): void {
    this.actionType.set((event.target as HTMLInputElement).value);
  }

  protected updatePermissionCode(event: Event): void {
    this.permissionCode.set((event.target as HTMLSelectElement).value);
  }

  protected applyFilters(event: Event): void {
    event.preventDefault();
    const permissionCode = this.permissionCode();
    this.filtersApplied.emit({
      targetUserId: positiveUserId(this.targetUserId()),
      actorUserId: positiveUserId(this.actorUserId()),
      actionType: this.actionType().trim() || undefined,
      permissionCode: isPermissionCode(permissionCode) ? permissionCode : undefined,
    });
  }

  protected actorTypeLabel(actorType: string): string {
    return actorType === 'User' ? 'مستخدم' : actorType === 'System' ? 'النظام' : actorType;
  }
}

function positiveUserId(value: string): number | undefined {
  const userId = Number.parseInt(value.trim(), 10);
  return Number.isSafeInteger(userId) && userId > 0 ? userId : undefined;
}
