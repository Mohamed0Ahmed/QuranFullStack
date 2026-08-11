import { isPlatformBrowser } from '@angular/common';
import {
  AfterViewInit,
  DestroyRef,
  Directive,
  ElementRef,
  PLATFORM_ID,
  computed,
  effect,
  inject,
  input,
  output,
} from '@angular/core';

import {
  activeDescendantIndex,
  clearActiveDescendant,
  ensureOptionId,
  focusedItemIndex,
  markCursorItem,
} from './floating-layer-cursor';
import {
  FloatingLayerFocus,
  containsPointerTarget,
  entryItem,
  prepareCursorHostFocus,
} from './floating-layer-focus';
import {
  FloatingTypeAheadState,
  floatingLayerItems,
  floatingTextEntryField,
  nextTypeAheadState,
  resolveFloatingKeyAction,
  stepIndex,
  typeAheadMatchIndex,
} from './floating-layer-keyboard';
import { FloatingAnchorPoint, repositionFloatingLayer } from './floating-layer-placement';

export type QdFloatingLayerVariant =
  | 'action-menu'
  | 'select-listbox'
  | 'searchable-picker'
  | 'disclosure-popover'
  | 'tooltip';

export type QdFloatingLayerDismissReason = 'escape' | 'tab' | 'outside';

const NAVIGABLE_VARIANTS: readonly QdFloatingLayerVariant[] = [
  'action-menu',
  'select-listbox',
  'searchable-picker',
];
const ACTIVE_DESCENDANT_VARIANTS: readonly QdFloatingLayerVariant[] = [
  'select-listbox',
  'searchable-picker',
];

@Directive({
  selector: '[qdFloatingLayer]',
  standalone: true,
  host: {
    class: 'qd-floating-layer',
    '[class.qd-floating-layer--action-menu]': "variant() === 'action-menu'",
    '[class.qd-floating-layer--select-listbox]': "variant() === 'select-listbox'",
    '[class.qd-floating-layer--searchable-picker]': "variant() === 'searchable-picker'",
    '[class.qd-floating-layer--disclosure-popover]': "variant() === 'disclosure-popover'",
    '[class.qd-floating-layer--tooltip]': "variant() === 'tooltip'",
    '[attr.data-qd-floating-variant]': 'variant()',
    '(keydown)': 'onKeydown($event)',
  },
})
export class QdFloatingLayerDirective implements AfterViewInit {
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly platformId = inject(PLATFORM_ID);

  readonly variant = input<QdFloatingLayerVariant>('action-menu', { alias: 'qdFloatingLayer' });
  readonly anchorElement = input<HTMLElement | null>(null);
  readonly anchorPoint = input<FloatingAnchorPoint | null>(null);
  readonly controlElement = input<HTMLElement | null>(null);
  readonly typeAhead = input<boolean | undefined>(undefined);

  readonly dismissed = output<QdFloatingLayerDismissReason>();

  private readonly navigable = computed(() => NAVIGABLE_VARIANTS.includes(this.variant()));
  private readonly activeDescendantModel = computed(() =>
    ACTIVE_DESCENDANT_VARIANTS.includes(this.variant()),
  );
  private readonly typeAheadEnabled = computed(() => this.typeAhead() ?? this.navigable());

  private readonly focus = new FloatingLayerFocus(this.elementRef.nativeElement);
  private typed: FloatingTypeAheadState | null = null;
  private viewReady = false;
  private cursorId: string | null = null;
  private appliedVariant: QdFloatingLayerVariant | null = null;
  private boundControl: HTMLElement | null = null;
  private readonly controlKeydown = (event: KeyboardEvent) => this.onKeydown(event);

  constructor() {
    effect(() => {
      this.controlElement();
      this.syncControlBinding();
    });

    effect(() => {
      const variant = this.variant();
      this.anchorElement();
      this.anchorPoint();
      if (this.appliedVariant !== null && this.appliedVariant !== variant) {
        this.dropCursor();
      }
      this.appliedVariant = variant;
      if (this.viewReady) {
        this.reposition();
      }
    });

    if (isPlatformBrowser(this.platformId)) {
      const onPointerDown = (event: Event) => this.onDocumentPointerDown(event);
      const onViewportChange = () => this.reposition();
      document.addEventListener('pointerdown', onPointerDown, true);
      window.addEventListener('resize', onViewportChange);
      window.addEventListener('scroll', onViewportChange, true);
      this.focus.capture(document.activeElement);
      this.destroyRef.onDestroy(() => {
        document.removeEventListener('pointerdown', onPointerDown, true);
        window.removeEventListener('resize', onViewportChange);
        window.removeEventListener('scroll', onViewportChange, true);
        this.focus.restoreWhenStillHeld(this.anchorElement(), document.activeElement);
      });
    }

    this.destroyRef.onDestroy(() => {
      this.boundControl?.removeEventListener('keydown', this.controlKeydown);
      this.boundControl = null;
    });
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.syncControlBinding();
    this.reposition();
    this.focusEntryItem();
  }

  private syncControlBinding(): void {
    const control = this.controlElement();
    if (control === this.boundControl) {
      return;
    }
    this.boundControl?.removeEventListener('keydown', this.controlKeydown);
    this.boundControl = control;
    control?.addEventListener('keydown', this.controlKeydown);
  }

  reposition(): void {
    if (isPlatformBrowser(this.platformId)) {
      repositionFloatingLayer(
        this.elementRef.nativeElement,
        this.anchorElement(),
        this.anchorPoint(),
      );
    }
  }

  items(): HTMLElement[] {
    return floatingLayerItems(this.elementRef.nativeElement);
  }

  protected onKeydown(event: KeyboardEvent): void {
    const items = this.navigable() ? this.items() : [];
    const action = resolveFloatingKeyAction(event, this.navigable(), items.length);

    switch (action.kind) {
      case 'dismiss':
        if (action.reason === 'escape') {
          event.preventDefault();
          event.stopPropagation();
        }
        this.close(action.reason);
        return;
      case 'step':
        event.preventDefault();
        this.setCursor(items[stepIndex(items.length, this.activeIndex(items), action.step)]);
        return;
      case 'edge':
        event.preventDefault();
        this.setCursor(action.edge === 'first' ? items[0] : items[items.length - 1]);
        return;
      case 'type-ahead':
        this.onTypeAhead(event, items);
        return;
      default:
        return;
    }
  }

  private onTypeAhead(event: KeyboardEvent, items: HTMLElement[]): void {
    if (!this.typeAheadEnabled()) {
      return;
    }
    const typed = nextTypeAheadState(this.typed, event.key, Date.now());
    if (typed === null) {
      return;
    }
    this.typed = typed;

    const match = typeAheadMatchIndex(items, this.activeIndex(items) + 1, typed.typed);
    if (match < 0) {
      return;
    }
    event.preventDefault();
    this.setCursor(items[match]);
  }

  private activeIndex(items: HTMLElement[]): number {
    return this.activeDescendantModel()
      ? activeDescendantIndex(items, this.cursorId)
      : focusedItemIndex(items, document.activeElement);
  }

  private setCursor(item: HTMLElement): void {
    item.scrollIntoView?.({ block: 'nearest' });
    if (!this.activeDescendantModel()) {
      this.focus.take(item);
      return;
    }
    this.cursorId = ensureOptionId(item);
    markCursorItem(this.elementRef.nativeElement, item);
    this.cursorHost().setAttribute('aria-activedescendant', this.cursorId);
  }

  private cursorHost(): HTMLElement {
    return this.controlElement() ?? this.searchField() ?? this.elementRef.nativeElement;
  }

  private searchField(): HTMLElement | null {
    return this.variant() === 'searchable-picker'
      ? floatingTextEntryField(this.elementRef.nativeElement)
      : null;
  }

  private dropCursor(): void {
    this.cursorId = null;
    clearActiveDescendant(this.elementRef.nativeElement, this.controlElement());
  }

  private focusEntryItem(): void {
    if (!isPlatformBrowser(this.platformId) || !this.navigable()) {
      return;
    }

    if (this.activeDescendantModel() && this.controlElement() === null) {
      this.focus.take(prepareCursorHostFocus(this.cursorHost(), this.elementRef.nativeElement));
    }

    const entry = entryItem(this.items());
    if (entry !== null) {
      this.setCursor(entry);
    }
  }

  private onDocumentPointerDown(event: Event): void {
    const target = event.target;
    if (!(target instanceof Node)) {
      return;
    }
    const anchors = [this.elementRef.nativeElement, this.anchorElement(), this.controlElement()];
    if (!containsPointerTarget(target, anchors)) {
      this.close('outside');
    }
  }

  private close(reason: QdFloatingLayerDismissReason): void {
    this.dropCursor();
    if (reason !== 'outside') {
      this.focus.restore(this.anchorElement());
    }
    this.dismissed.emit(reason);
  }
}
