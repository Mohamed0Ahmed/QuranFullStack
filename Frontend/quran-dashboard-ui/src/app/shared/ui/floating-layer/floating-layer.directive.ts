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
  FLOATING_ANCHOR_GAP,
  FloatingAnchorPoint,
  placeFloatingLayer,
  pointerAnchorRect,
  resolveFloatingDirection,
  resolveRootFontSize,
} from './floating-layer-placement';

export type QdFloatingLayerVariant =
  | 'action-menu'
  | 'select-listbox'
  | 'searchable-picker'
  | 'disclosure-popover'
  | 'tooltip';

export type QdFloatingLayerDismissReason = 'escape' | 'tab' | 'outside';

const ITEM_SELECTOR = '[role="menuitem"], [role="menuitemradio"], [role="menuitemcheckbox"], [role="option"]';
const TEXT_ENTRY_SELECTOR =
  'input:not([type="button"]):not([type="checkbox"]):not([type="hidden"]):not([type="radio"]), textarea, [contenteditable=""], [contenteditable="true"]';
const TYPE_AHEAD_WINDOW_MS = 600;
const CURSOR_ATTRIBUTE = 'data-qd-floating-cursor';
const NAVIGABLE_VARIANTS: readonly QdFloatingLayerVariant[] = [
  'action-menu',
  'select-listbox',
  'searchable-picker',
];
const ACTIVE_DESCENDANT_VARIANTS: readonly QdFloatingLayerVariant[] = [
  'select-listbox',
  'searchable-picker',
];

let nextOptionId = 0;

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

  private focusReturnTarget: HTMLElement | null = null;
  private typedPrefix = '';
  private typedAt = 0;
  private viewReady = false;
  private cursorId: string | null = null;
  private appliedVariant: QdFloatingLayerVariant | null = null;
  private boundControl: HTMLElement | null = null;
  private tookFocus = false;
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
        this.clearActiveDescendant();
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
      this.destroyRef.onDestroy(() => {
        document.removeEventListener('pointerdown', onPointerDown, true);
        window.removeEventListener('resize', onViewportChange);
        window.removeEventListener('scroll', onViewportChange, true);
      });
    }

    this.captureFocusReturnTarget();
    this.destroyRef.onDestroy(() => {
      this.boundControl?.removeEventListener('keydown', this.controlKeydown);
      this.boundControl = null;
      this.returnFocusWhenStillHeldInside();
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
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    const layer = this.elementRef.nativeElement;
    const anchor = this.anchorElement();
    const point = this.anchorPoint();
    const anchored = anchor !== null && anchor.isConnected;
    if (!anchored && point === null) {
      return;
    }

    const placement = placeFloatingLayer(
      anchored ? anchor.getBoundingClientRect() : pointerAnchorRect(point!),
      { width: layer.offsetWidth, height: layer.scrollHeight },
      { width: window.innerWidth, height: window.innerHeight },
      resolveFloatingDirection(layer),
      resolveRootFontSize(layer),
      anchored ? FLOATING_ANCHOR_GAP : 0,
    );

    layer.style.position = 'fixed';
    layer.style.insetInlineStart = 'auto';
    layer.style.insetInlineEnd = 'auto';
    layer.style.setProperty('inset-block-start', `${placement.top}px`);
    layer.style.setProperty('left', `${placement.left}px`);
    layer.style.maxBlockSize = `${placement.maxBlockSize}px`;
    layer.dataset['qdFloatingBlockSide'] = placement.blockSide;
    layer.dataset['qdFloatingClamped'] = placement.inlineClamped ? 'true' : 'false';
  }

  items(): HTMLElement[] {
    return Array.from(this.elementRef.nativeElement.querySelectorAll<HTMLElement>(ITEM_SELECTOR)).filter(
      (item) => item.getAttribute('aria-disabled') !== 'true' && !item.hasAttribute('disabled'),
    );
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      event.stopPropagation();
      this.close('escape');
      return;
    }

    if (event.key === 'Tab') {
      this.close('tab');
      return;
    }

    if (!this.navigable()) {
      return;
    }

    const items = this.items();
    if (items.length === 0) {
      return;
    }

    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault();
      this.moveBy(items, event.key === 'ArrowDown' ? 1 : -1);
      return;
    }

    if (isTextEntry(event.target)) {
      return;
    }

    switch (event.key) {
      case 'Home':
        event.preventDefault();
        this.setCursor(items[0]);
        return;
      case 'End':
        event.preventDefault();
        this.setCursor(items[items.length - 1]);
        return;
      default:
        this.onTypeAhead(event, items);
    }
  }

  private onTypeAhead(event: KeyboardEvent, items: HTMLElement[]): void {
    if (!this.typeAheadEnabled() || event.key.length !== 1 || event.altKey || event.ctrlKey || event.metaKey) {
      return;
    }

    const now = Date.now();
    const continuing = this.typedPrefix !== '' && now - this.typedAt <= TYPE_AHEAD_WINDOW_MS;
    if (event.key === ' ' && !continuing) {
      return;
    }

    const typed = continuing ? this.typedPrefix + event.key : event.key;
    const prefix = normalize(typed);
    if (prefix === '') {
      return;
    }

    this.typedPrefix = typed;
    this.typedAt = now;

    const startIndex = this.activeIndex(items) + 1;
    const ordered = items.slice(startIndex).concat(items.slice(0, startIndex));
    const match = ordered.find((item) => normalize(item.textContent ?? '').startsWith(prefix));
    if (match === undefined) {
      return;
    }
    event.preventDefault();
    this.setCursor(match);
  }

  private moveBy(items: HTMLElement[], step: number): void {
    const current = this.activeIndex(items);
    const base = current >= 0 ? current : step > 0 ? -1 : 0;
    const next = (base + step + items.length) % items.length;
    this.setCursor(items[next]);
  }

  private activeIndex(items: HTMLElement[]): number {
    if (this.activeDescendantModel()) {
      return this.cursorId === null ? -1 : items.findIndex((item) => item.id === this.cursorId);
    }
    const active = document.activeElement;
    return active instanceof HTMLElement ? items.indexOf(active) : -1;
  }

  private setCursor(item: HTMLElement): void {
    item.scrollIntoView?.({ block: 'nearest' });
    if (!this.activeDescendantModel()) {
      this.tookFocus = true;
      item.focus();
      return;
    }
    const id = ensureOptionId(item);
    this.cursorId = id;
    this.markCursorItem(item);
    this.cursorHost().setAttribute('aria-activedescendant', id);
  }

  private markCursorItem(item: HTMLElement | null): void {
    for (const marked of Array.from(
      this.elementRef.nativeElement.querySelectorAll<HTMLElement>(`[${CURSOR_ATTRIBUTE}]`),
    )) {
      if (marked !== item) {
        marked.removeAttribute(CURSOR_ATTRIBUTE);
      }
    }
    item?.setAttribute(CURSOR_ATTRIBUTE, 'true');
  }

  private cursorHost(): HTMLElement {
    return this.controlElement() ?? this.searchField() ?? this.elementRef.nativeElement;
  }

  private searchField(): HTMLElement | null {
    return this.variant() === 'searchable-picker'
      ? this.elementRef.nativeElement.querySelector<HTMLElement>(TEXT_ENTRY_SELECTOR)
      : null;
  }

  private clearActiveDescendant(): void {
    this.cursorId = null;
    this.markCursorItem(null);
    const layer = this.elementRef.nativeElement;
    layer.removeAttribute('aria-activedescendant');
    layer.querySelector('[aria-activedescendant]')?.removeAttribute('aria-activedescendant');
    this.controlElement()?.removeAttribute('aria-activedescendant');
  }

  private focusEntryItem(): void {
    if (!isPlatformBrowser(this.platformId) || !this.navigable()) {
      return;
    }

    // An external combobox control keeps DOM focus and drives the layer through
    // aria-activedescendant, so the layer must not pull focus off the field the user is typing in.
    if (this.activeDescendantModel() && this.controlElement() === null) {
      const host = this.cursorHost();
      if (host === this.elementRef.nativeElement && !host.hasAttribute('tabindex')) {
        host.setAttribute('tabindex', '-1');
      }
      this.tookFocus = true;
      host.focus();
    }

    const items = this.items();
    if (items.length === 0) {
      return;
    }
    const selected = items.find(
      (item) => item.getAttribute('aria-selected') === 'true' || item.getAttribute('aria-current') === 'true',
    );
    this.setCursor(selected ?? items[0]);
  }

  private onDocumentPointerDown(event: Event): void {
    const target = event.target;
    if (!(target instanceof Node)) {
      return;
    }
    if (
      this.elementRef.nativeElement.contains(target) ||
      this.anchorElement()?.contains(target) === true ||
      this.controlElement()?.contains(target) === true
    ) {
      return;
    }
    this.close('outside');
  }

  private close(reason: QdFloatingLayerDismissReason): void {
    this.clearActiveDescendant();
    if (reason !== 'outside') {
      this.returnFocus();
    }
    this.dismissed.emit(reason);
  }

  private captureFocusReturnTarget(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    const active = document.activeElement;
    this.focusReturnTarget = active instanceof HTMLElement && active !== document.body ? active : null;
  }

  private returnFocusWhenStillHeldInside(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    const active = document.activeElement;
    const heldInside =
      this.elementRef.nativeElement.contains(active) ||
      (this.tookFocus && (active === null || active === document.body));
    if (heldInside) {
      this.returnFocus();
    }
  }

  private returnFocus(): void {
    const anchor = this.anchorElement();
    const target = anchor?.isConnected === true ? anchor : this.focusReturnTarget;
    if (target?.isConnected === true) {
      target.focus();
    }
  }
}

function normalize(value: string): string {
  return value.trim().toLocaleLowerCase();
}

function isTextEntry(target: EventTarget | null): boolean {
  return target instanceof HTMLElement && target.matches(TEXT_ENTRY_SELECTOR);
}

function ensureOptionId(item: HTMLElement): string {
  if (item.id === '') {
    item.id = `qd-floating-option-${nextOptionId++}`;
  }
  return item.id;
}
