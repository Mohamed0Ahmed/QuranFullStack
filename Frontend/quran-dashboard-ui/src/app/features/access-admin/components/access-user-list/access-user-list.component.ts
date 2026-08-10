import { ChangeDetectionStrategy, Component, effect, input, output, signal } from '@angular/core';

import { AccessUserSummary } from '../../../../core/api/generated/models/access-user-summary';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdControlDirective } from '../../../../shared/ui/form-field/control.directive';
import { QdFormFieldComponent } from '../../../../shared/ui/form-field/form-field.component';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import {
  QdResultItemDirective,
  QdResultListDirective,
} from '../../../../shared/ui/result-list/result-list.directive';
import { ExplorerResultCountComponent } from '../../../../shared/ui/result-count/explorer-result-count.component';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { ACCESS_ADMIN_LABELS } from '../../models/access-admin.labels';
import {
  AccessUserListFilters,
  AccessUserListQuery,
  AccessUserStatus,
  accessLifecycleTone,
  accessUserNameLabel,
} from '../../models/access-admin.models';

type OwnerFilter = 'all' | 'owner' | 'non-owner';

@Component({
  selector: 'qd-access-user-list',
  standalone: true,
  imports: [
    ExplorerResultCountComponent,
    PaginationComponent,
    QdActionDirective,
    QdControlDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QdFormFieldComponent,
    QdResultItemDirective,
    QdResultListDirective,
    QdSkeletonRowsComponent,
  ],
  templateUrl: './access-user-list.component.html',
  styleUrl: './access-user-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessUserListComponent {
  readonly users = input.required<readonly AccessUserSummary[]>();
  readonly selectedUserId = input<number | null>(null);
  readonly query = input.required<AccessUserListQuery>();
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly loading = input(false);
  readonly error = input<string | null>(null);

  readonly filtersChange = output<AccessUserListFilters>();
  readonly userSelected = output<number>();
  readonly pageChange = output<number>();

  protected readonly search = signal('');
  protected readonly status = signal<AccessUserStatus | 'all'>('all');
  protected readonly owner = signal<OwnerFilter>('all');

  protected readonly skeletonRowCount = 6;
  protected readonly skeletonRowTemplate = 'minmax(0, 1fr) auto';

  constructor() {
    effect(() => {
      const query = this.query();
      this.search.set(query.search ?? '');
      this.status.set(query.status ?? 'all');
      this.owner.set(query.isOwner === undefined ? 'all' : query.isOwner ? 'owner' : 'non-owner');
    });
  }

  protected updateSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected updateStatus(event: Event): void {
    this.status.set((event.target as HTMLSelectElement).value as AccessUserStatus | 'all');
  }

  protected updateOwner(event: Event): void {
    this.owner.set((event.target as HTMLSelectElement).value as OwnerFilter);
  }

  protected applyFilters(event: Event): void {
    event.preventDefault();
    const search = this.search().trim();
    const status = this.status();
    const owner = this.owner();
    this.filtersChange.emit({
      status: status === 'all' ? undefined : status,
      isOwner: owner === 'all' ? undefined : owner === 'owner',
      search: search || undefined,
    });
  }

  protected nameLabel(user: AccessUserSummary): string {
    return accessUserNameLabel(user);
  }

  protected statusLabel(user: AccessUserSummary): string {
    return ACCESS_ADMIN_LABELS.userStatus(user.status);
  }

  protected lifecycleBadgeClass(user: AccessUserSummary): string {
    return `qd-badge qd-badge--status qd-badge--lifecycle-${accessLifecycleTone(user.status)}`;
  }

  protected isSelected(user: AccessUserSummary): boolean {
    return this.selectedUserId() === user.id;
  }
}
