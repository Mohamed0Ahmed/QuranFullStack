import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';

import { AccessUserSummary } from '../../../../core/api/generated/models/access-user-summary';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdFloatingLayerDirective } from '../../../../shared/ui/floating-layer/floating-layer.directive';
import {
  AccessUserSearchState,
  EMPTY_ACCESS_USER_SEARCH,
  accessUserNameLabel,
} from '../../models/access-admin.models';

@Component({
  selector: 'qd-access-user-picker',
  standalone: true,
  imports: [
    QdActionDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QdFloatingLayerDirective,
  ],
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
  protected readonly open = signal(false);

  private readonly searchField = viewChild<ElementRef<HTMLInputElement>>('searchField');

  protected readonly anchor = computed(() => this.searchField()?.nativeElement ?? null);

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
    this.open.set(true);
    this.searchRequested.emit(term);
  }

  protected close(): void {
    this.open.set(false);
  }

  protected activateCursor(event: Event): void {
    const layer = event.currentTarget as HTMLElement;
    const cursorId = layer.getAttribute('aria-activedescendant');
    const option = Array.from(layer.querySelectorAll<HTMLElement>('[role="option"]')).find(
      (candidate) => candidate.id === cursorId,
    );
    if (!option) {
      return;
    }
    event.preventDefault();
    option.click();
  }

  protected select(candidate: AccessUserSummary): void {
    this.term.set('');
    this.open.set(false);
    this.selectionChange.emit(candidate);
  }

  protected clear(): void {
    this.selectionChange.emit(null);
  }

  protected nameLabel(candidate: AccessUserSummary): string {
    return accessUserNameLabel(candidate);
  }
}
