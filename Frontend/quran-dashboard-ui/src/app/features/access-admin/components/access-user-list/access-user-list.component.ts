import { ChangeDetectionStrategy, Component, effect, input, output, signal } from '@angular/core';

import { AccessUserSummary } from '../../../../core/api/generated/models/access-user-summary';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
import {
  AccessUserListFilters,
  AccessUserListQuery,
  AccessUserStatus,
  accessUserNameLabel,
} from '../../models/access-admin.models';

type OwnerFilter = 'all' | 'owner' | 'non-owner';

@Component({
  selector: 'qd-access-user-list',
  standalone: true,
  imports: [PaginationComponent, QdStateComponent],
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
    if (user.status === 'active') {
      return 'نشط';
    }
    if (user.status === 'pending') {
      return 'معلّق';
    }
    return 'معطّل';
  }
}
