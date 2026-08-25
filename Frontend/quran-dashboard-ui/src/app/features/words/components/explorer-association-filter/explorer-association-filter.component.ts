import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  computed,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';

import { AssociationOption } from '../../state/words-association-filters';
import { WORDS_ASSOCIATION_FILTER_LABELS } from '../../models/words-shared.labels';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdControlDirective } from '../../../../shared/ui/form-field/control.directive';
import {
  QdFloatingLayerDirective,
  QdFloatingLayerDismissReason,
} from '../../../../shared/ui/floating-layer/floating-layer.directive';

export type { AssociationOption } from '../../state/words-association-filters';

let nextPanelId = 0;

@Component({
  selector: 'qd-explorer-association-filter',
  standalone: true,
  imports: [QdActionDirective, QdControlDirective, QdFloatingLayerDirective],
  templateUrl: './explorer-association-filter.component.html',
  styleUrl: './explorer-association-filter.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExplorerAssociationFilterComponent {
  private readonly elementRef = inject(ElementRef<HTMLElement>);

  readonly fieldInputRef = viewChild<ElementRef<HTMLInputElement>>('fieldInput');

  readonly label = input.required<string>();
  readonly placeholder = input<string>('');
  readonly options = input<readonly AssociationOption[]>([]);
  readonly selectedId = input<string | number | null>(null);
  readonly selectedLabel = input<string | null>(null);
  readonly loading = input<boolean>(false);
  readonly error = input<boolean>(false);
  readonly disabled = input<boolean>(false);
  readonly clientFilter = input<boolean>(false);
  readonly testid = input<string>('explorer-association-filter');

  readonly searchChange = output<string>();
  readonly selectionChange = output<AssociationOption | null>();

  private readonly instance = nextPanelId++;
  protected readonly panelId = `association-filter-panel-${this.instance}`;
  protected readonly panelOpen = signal(false);
  protected readonly query = signal('');

  private reopenSuppressed = false;

  protected get labels() { return WORDS_ASSOCIATION_FILTER_LABELS; }

  protected readonly fieldElement = computed(() => this.fieldInputRef()?.nativeElement ?? null);

  protected readonly hasSelection = computed(() => this.selectedId() !== null);

  protected readonly selectionText = computed(() => {
    const explicit = this.selectedLabel();
    if (explicit !== null && explicit.length > 0) {
      return explicit;
    }
    const id = this.selectedId();
    if (id === null) {
      return '';
    }
    const match = this.options().find((option) => option.id === id);
    return match?.label ?? this.labels.activeFilter;
  });

  protected readonly visibleOptions = computed<readonly AssociationOption[]>(() => {
    if (!this.clientFilter()) {
      return this.options();
    }
    const term = this.query().trim().toLowerCase();
    if (term.length === 0) {
      return this.options();
    }
    return this.options().filter((option) => option.label.toLowerCase().includes(term));
  });

  @HostListener('focusout', ['$event'])
  protected onComponentFocusOut(event: FocusEvent): void {
    if (!this.panelOpen()) {
      return;
    }
    const root = this.elementRef.nativeElement as HTMLElement;
    const related = event.relatedTarget as Node | null;
    if (!related || !root.contains(related)) {
      this.panelOpen.set(false);
    }
  }

  protected optionElementId(option: AssociationOption): string {
    return `${this.panelId}-option-${option.id}`;
  }

  protected onFieldFocus(): void {
    if (this.reopenSuppressed) {
      return;
    }
    if (!this.clientFilter() && !this.hasSelection() && this.query().trim().length === 0) {
      return;
    }
    this.openPanel();
  }

  protected onFieldKeydown(event: KeyboardEvent): void {
    if (event.key === 'ArrowDown' && !this.panelOpen()) {
      event.preventDefault();
      this.openPanel();
      return;
    }
    if (event.key === 'Enter' && this.panelOpen()) {
      this.activateCursor(event);
    }
  }

  protected onFieldBlur(): void {
    this.reopenSuppressed = false;
  }

  protected onQueryInput(value: string): void {
    this.query.set(value);
    this.reopenSuppressed = false;
    if (!this.panelOpen()) {
      this.openPanel();
    }
    if (!this.clientFilter()) {
      this.searchChange.emit(value.trim());
    }
  }

  protected onLayerDismissed(reason: QdFloatingLayerDismissReason): void {
    this.panelOpen.set(false);
    if (reason === 'escape') {
      this.reopenSuppressed = true;
    }
  }

  protected onSelect(option: AssociationOption): void {
    this.selectionChange.emit(option);
    this.query.set('');
    this.panelOpen.set(false);
    this.reopenSuppressed = true;
    this.fieldInputRef()?.nativeElement.focus();
  }

  protected onClear(): void {
    this.selectionChange.emit(null);
    this.query.set('');
    if (this.panelOpen()) {
      this.panelOpen.set(false);
    }
  }

  private activateCursor(event: KeyboardEvent): void {
    const cursorId = this.fieldInputRef()?.nativeElement.getAttribute('aria-activedescendant');
    if (!cursorId) {
      return;
    }
    const option = this.visibleOptions().find((candidate) => this.optionElementId(candidate) === cursorId);
    if (option === undefined) {
      return;
    }
    event.preventDefault();
    this.onSelect(option);
  }

  private openPanel(): void {
    this.panelOpen.set(true);
  }
}
