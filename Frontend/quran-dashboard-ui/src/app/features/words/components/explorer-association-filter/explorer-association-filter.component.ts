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

export type { AssociationOption } from '../../state/words-association-filters';

const PANEL_VIEWPORT_PADDING_PX = 8;
// Preferred room below the field before we flip the panel above it — not a forced minimum height.
const PANEL_FLIP_THRESHOLD_PX = 120;
const PANEL_MAX_HEIGHT_PX = 320;
const PANEL_MAX_HEIGHT_VAR = '--assoc-filter-panel-max-height';
const PANEL_ABOVE_CLASS = 'association-filter__panel--above';

let nextPanelId = 0;

/**
 * Presentational association-filter picker (Feature 026 US7, Feature 027 popover refactor). A
 * labeled search field whose input doubles as the anchor for a popover panel; used for
 * the Unique Words primary type / primary root, Lemmas root, and Stems primary root/lemma filters.
 * It owns no data: the page supplies <c>options</c> (loaded via the existing roots/lemmas apis or the
 * word-types tree read) and reacts to <c>searchChange</c>; selecting an option emits
 * <c>selectionChange</c>.
 *
 * - <c>clientFilter</c> = true (small static lists, e.g. the type select): filters options locally.
 * - <c>clientFilter</c> = false (roots/lemmas): the page server-searches and passes the results in.
 *
 * Popover model: the panel opens on typing (see <c>onQueryInput</c>), on ArrowDown (see
 * <c>onFieldKeydown</c>), or on focus only when the field already carries a selection or a query (see
 * <c>onFieldFocus</c>) — a plain empty field stays closed. It closes on Escape, an outside click, focus
 * leaving the whole component, or a selection — never via a separate disclosure trigger. There is no
 * focus trap and no roving arrow-key/listbox model: ArrowDown only opens the panel and hands focus to
 * the first option, which stays a plain Tab-reachable button (see the template comment above the list).
 */
@Component({
  selector: 'qd-explorer-association-filter',
  standalone: true,
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
  readonly disabled = input<boolean>(false);
  readonly clientFilter = input<boolean>(false);
  readonly testid = input<string>('explorer-association-filter');

  readonly searchChange = output<string>();
  readonly selectionChange = output<AssociationOption | null>();

  protected readonly panelId = `association-filter-panel-${nextPanelId++}`;
  protected readonly panelOpen = signal(false);
  protected readonly query = signal('');

  // Guards against the panel instantly reopening when Escape or a selection restores focus to the
  // field programmatically. Cleared on the field's next real blur. A plain field is enough: it only
  // gates event handlers and never drives template rendering.
  private reopenSuppressed = false;

  // TDZ-safe getter (see words README): reading the labels const via a readonly field resolves to
  // undefined in the bundled test build.
  protected get labels() { return WORDS_ASSOCIATION_FILTER_LABELS; }

  protected readonly hasSelection = computed(() => this.selectedId() !== null);

  // The active selection's display text. On URL restore the option list may not carry the selected
  // entry yet (server-searched pickers start empty) — show the neutral active-filter badge rather
  // than leaking the raw id.
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

  // Focus leaving the whole component (not just the field) closes the panel — this is what makes
  // tabbing past the last option close it, and makes focusing a sibling association field close this
  // one (single-open invariant). No focus is moved here; the browser already decided where it's going.
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

  // Focus alone opens the panel only when there is something to show back: an active selection (so the
  // user can revise it) or a query already in the field. An empty, unselected field stays closed —
  // ArrowDown (see onFieldKeydown) is the keyboard route in.
  protected onFieldFocus(): void {
    if (this.reopenSuppressed) {
      return;
    }
    if (!this.hasSelection() && this.query().trim().length === 0) {
      return;
    }
    this.openPanel();
  }

  // ArrowDown (bare or Alt+ArrowDown, both key === 'ArrowDown') is the combobox escape hatch: it opens
  // the panel even from an empty field and hands focus to the first option, so keyboard users can browse
  // without typing.
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

    // Flip above only when there is too little room below AND more room above; otherwise stay below.
    const openAbove = spaceBelow < PANEL_FLIP_THRESHOLD_PX && spaceAbove > spaceBelow;
    // Cap the height to the chosen side's actual space so the panel never overflows the viewport.
    const available = Math.max(0, openAbove ? spaceAbove : spaceBelow);
    const maxHeight = Math.min(PANEL_MAX_HEIGHT_PX, available);

    panel.classList.toggle(PANEL_ABOVE_CLASS, openAbove);
    panel.style.setProperty(PANEL_MAX_HEIGHT_VAR, `${maxHeight}px`);
  }
}
