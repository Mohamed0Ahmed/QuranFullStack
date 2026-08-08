import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';

import { AccessUserSummary } from '../../../../core/api/generated/models/access-user-summary';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
import {
  AccessUserSearchState,
  EMPTY_ACCESS_USER_SEARCH,
  accessUserNameLabel,
} from '../../models/access-admin.models';

@Component({
  selector: 'qd-access-user-picker',
  standalone: true,
  imports: [QdStateComponent],
  templateUrl: './access-user-picker.component.html',
  styleUrl: './access-user-picker.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessUserPickerComponent {
  readonly label = input.required<string>();
  readonly controlId = input.required<string>();
  readonly testIdPrefix = input.required<string>();
  readonly state = input<AccessUserSearchState>(EMPTY_ACCESS_USER_SEARCH);
  readonly selected = input<AccessUserSummary | null>(null);

  readonly searchRequested = output<string>();
  readonly selectionChange = output<AccessUserSummary | null>();

  protected readonly term = signal('');
  protected readonly searched = signal(false);

  protected updateTerm(event: Event): void {
    this.term.set((event.target as HTMLInputElement).value);
  }

  protected searchOnEnter(event: Event): void {
    event.preventDefault();
    this.search();
  }

  protected search(): void {
    const term = this.term().trim();
    if (!term || this.state().loading) {
      return;
    }
    this.searched.set(true);
    this.searchRequested.emit(term);
  }

  protected select(candidate: AccessUserSummary): void {
    this.term.set('');
    this.searched.set(false);
    this.selectionChange.emit(candidate);
  }

  protected clear(): void {
    this.selectionChange.emit(null);
  }

  protected nameLabel(candidate: AccessUserSummary): string {
    return accessUserNameLabel(candidate);
  }
}
