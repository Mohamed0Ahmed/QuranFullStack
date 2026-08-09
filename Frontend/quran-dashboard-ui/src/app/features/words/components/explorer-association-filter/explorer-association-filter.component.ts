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

export type { AssociationOption } from '../../state/words-association-filters';

const PANEL_VIEWPORT_PADDING_PX = 8;
const PANEL_FLIP_THRESHOLD_PX = 120;
const PANEL_MAX_HEIGHT_PX = 320;
const PANEL_MAX_HEIGHT_VAR = '--assoc-filter-panel-max-height';
const PANEL_ABOVE_CLASS = 'association-filter__panel--above';

let nextPanelId = 0;

@Component({
  selector: 'qd-explorer-association-filter',
  standalone: true,
  imports: [QdActionDirective, QdControlDirective],
  templateUrl: './explorer-association-filter.component.html',
  styleUrl: './explorer-association-filter.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExplorerAssociationFilterComponent {
  private readonly elementRef = inject(ElementRef<HTMLElement>);

  readonly fieldInputRef = viewChild<ElementRef<HTMLInputElement>>('fieldInput');
  readonly panelRef = viewChild<ElementRef<HTMLElement>>('panel');

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

  protected readonly panelId = `association-filter-panel-${nextPanelId++}`;
  protected readonly panelOpen = signal(false);
  protected readonly query = signal('');

  private reopenSuppressed = false;

  protected get labels() { return WORDS_ASSOCIATION_FILTER_LABELS; }

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

  @HostListener('document:keydown', ['$event'])
  protected onDocumentKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Escape' || !this.panelOpen()) {
      return;
    }
    event.preventDefault();
    this.panelOpen.set(false);
    this.reopenSuppressed = true;
    this.fieldInputRef()?.nativeElement.focus();
  }

  @HostListener('document:click', ['$event'])
  protected onDocumentClick(event: MouseEvent): void {
    if (!this.panelOpen()) {
      return;
    }
    const root = this.elementRef.nativeElement as HTMLElement;
    const target = event.target as Node | null;
    if (target && !root.contains(target)) {
      this.panelOpen.set(false);
    }
  }

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

  @HostListener('window:scroll')
  @HostListener('window:resize')
  protected onViewportChange(): void {
    if (this.panelOpen()) {
      requestAnimationFrame(() => this.applyPanelMaxHeight());
    }
  }

  protected onFieldFocus(): void {
    if (this.reopenSuppressed) {
      return;
    }
    if (!this.hasSelection() && this.query().trim().length === 0) {
      return;
    }
    this.openPanel();
  }

  protected onFieldKeydown(event: KeyboardEvent): void {
    if (event.key !== 'ArrowDown') {
      return;
    }
    event.preventDefault();
    this.openPanel();
    requestAnimationFrame(() => this.focusFirstOption());
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

  private openPanel(): void {
    this.panelOpen.set(true);
    requestAnimationFrame(() => this.applyPanelMaxHeight());
  }

  private focusFirstOption(): void {
    this.panelRef()
      ?.nativeElement.querySelector<HTMLButtonElement>('.association-filter__option')
      ?.focus();
  }

  private applyPanelMaxHeight(): void {
    const field = this.fieldInputRef()?.nativeElement;
    const panel = this.panelRef()?.nativeElement;
    if (!field || !panel) {
      return;
    }

    const fieldRect = field.getBoundingClientRect();
    const spaceBelow = window.innerHeight - fieldRect.bottom - PANEL_VIEWPORT_PADDING_PX;
    const spaceAbove = fieldRect.top - PANEL_VIEWPORT_PADDING_PX;

    const openAbove = spaceBelow < PANEL_FLIP_THRESHOLD_PX && spaceAbove > spaceBelow;
    const available = Math.max(0, openAbove ? spaceAbove : spaceBelow);
    const maxHeight = Math.min(PANEL_MAX_HEIGHT_PX, available);

    panel.classList.toggle(PANEL_ABOVE_CLASS, openAbove);
    panel.style.setProperty(PANEL_MAX_HEIGHT_VAR, `${maxHeight}px`);
  }
}
