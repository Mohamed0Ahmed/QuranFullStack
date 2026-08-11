import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  effect,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';

import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdControlDirective } from '../../../../shared/ui/form-field/control.directive';
import { QdFormFieldComponent } from '../../../../shared/ui/form-field/form-field.component';
import { LinkingAyah } from '../../models/linking-ayah.models';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingSelection } from '../../models/linking-workspace.models';
import { LinkingFocusCoordinator } from '../../state/linking-focus.coordinator';
import { isVerseSelected } from '../../utils/linking-selection';
import { LinkingAyahCardComponent } from '../linking-ayah-card/linking-ayah-card.component';

let nextSelectionId = 0;

@Component({
  selector: 'qd-linking-ayah-selection',
  standalone: true,
  imports: [
    PaginationComponent,
    QdActionDirective,
    QdControlDirective,
    QdFormFieldComponent,
    LinkingAyahCardComponent,
  ],
  templateUrl: './linking-ayah-selection.component.html',
  styleUrl: './linking-ayah-selection.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingAyahSelectionComponent {
  private readonly focus = inject(LinkingFocusCoordinator);
  private readonly searchInput = viewChild<ElementRef<HTMLInputElement>>('searchInput');
  private readonly instance = nextSelectionId++;

  readonly ayahs = input.required<readonly LinkingAyah[]>();
  readonly selection = input.required<LinkingSelection>();
  readonly highlightSourceWords = input(true);
  readonly selectedCount = input.required<number>();
  readonly query = input('');
  readonly page = input(1);
  readonly pageSize = input(12);
  readonly filteredCount = input<number | null>(null);
  readonly disabled = input(false);
  readonly focusOnEntry = input(false);
  readonly paginationLabel = input(LINKING_LABELS.sourceEditorPagination);

  readonly selectionToggled = output<string>();
  readonly selectAllRequested = output<void>();
  readonly clearAllRequested = output<void>();
  readonly queryChanged = output<string>();
  readonly pageChanged = output<number>();

  protected readonly labels = LINKING_LABELS;

  constructor() {
    effect(() => {
      if (this.focusOnEntry()) {
        this.focus.focusAfterRender(() => this.searchInput()?.nativeElement ?? null);
      }
    });
  }

  protected inputId(verseKey: string): string {
    return `linking-ayah-selection-${this.instance}-${verseKey.replace(':', '-')}`;
  }

  protected onSearch(event: Event): void {
    this.queryChanged.emit((event.target as HTMLInputElement).value);
  }

  protected isSelected(verseKey: string): boolean {
    return isVerseSelected(this.selection(), verseKey);
  }

  protected totalCount(): number {
    return this.filteredCount() ?? this.ayahs().length;
  }
}
