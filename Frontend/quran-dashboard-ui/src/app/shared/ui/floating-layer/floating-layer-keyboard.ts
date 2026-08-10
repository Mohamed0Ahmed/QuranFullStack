export const FLOATING_ITEM_SELECTOR =
  '[role="menuitem"], [role="menuitemradio"], [role="menuitemcheckbox"], [role="option"]';
export const FLOATING_TEXT_ENTRY_SELECTOR =
  'input:not([type="button"]):not([type="checkbox"]):not([type="hidden"]):not([type="radio"]), textarea, [contenteditable=""], [contenteditable="true"]';
export const FLOATING_TYPE_AHEAD_WINDOW_MS = 600;

export type FloatingLayerKeyAction =
  | { readonly kind: 'dismiss'; readonly reason: 'escape' | 'tab' }
  | { readonly kind: 'step'; readonly step: 1 | -1 }
  | { readonly kind: 'edge'; readonly edge: 'first' | 'last' }
  | { readonly kind: 'type-ahead' }
  | { readonly kind: 'none' };

export interface FloatingTypeAheadState {
  readonly typed: string;
  readonly at: number;
}

export function floatingLayerItems(layer: HTMLElement): HTMLElement[] {
  return Array.from(layer.querySelectorAll<HTMLElement>(FLOATING_ITEM_SELECTOR)).filter(
    (item) => item.getAttribute('aria-disabled') !== 'true' && !item.hasAttribute('disabled'),
  );
}

export function floatingTextEntryField(layer: HTMLElement): HTMLElement | null {
  return layer.querySelector<HTMLElement>(FLOATING_TEXT_ENTRY_SELECTOR);
}

export function isFloatingTextEntry(target: EventTarget | null): boolean {
  return target instanceof HTMLElement && target.matches(FLOATING_TEXT_ENTRY_SELECTOR);
}

export function resolveFloatingKeyAction(
  event: KeyboardEvent,
  navigable: boolean,
  itemCount: number,
): FloatingLayerKeyAction {
  if (event.key === 'Escape') {
    return { kind: 'dismiss', reason: 'escape' };
  }
  if (event.key === 'Tab') {
    return { kind: 'dismiss', reason: 'tab' };
  }
  if (!navigable || itemCount === 0) {
    return { kind: 'none' };
  }
  if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
    return { kind: 'step', step: event.key === 'ArrowDown' ? 1 : -1 };
  }
  if (isFloatingTextEntry(event.target)) {
    return { kind: 'none' };
  }
  if (event.key === 'Home' || event.key === 'End') {
    return { kind: 'edge', edge: event.key === 'Home' ? 'first' : 'last' };
  }
  if (event.key.length !== 1 || event.altKey || event.ctrlKey || event.metaKey) {
    return { kind: 'none' };
  }
  return { kind: 'type-ahead' };
}

export function nextTypeAheadState(
  current: FloatingTypeAheadState | null,
  key: string,
  now: number,
): FloatingTypeAheadState | null {
  const continuing = current !== null && now - current.at <= FLOATING_TYPE_AHEAD_WINDOW_MS;
  if (key === ' ' && !continuing) {
    return null;
  }
  const typed = continuing ? current.typed + key : key;
  return normalize(typed) === '' ? null : { typed, at: now };
}

export function typeAheadMatchIndex(
  items: readonly HTMLElement[],
  from: number,
  typed: string,
): number {
  const prefix = normalize(typed);
  for (let offset = 0; offset < items.length; offset += 1) {
    const index = (from + offset) % items.length;
    if (normalize(items[index].textContent ?? '').startsWith(prefix)) {
      return index;
    }
  }
  return -1;
}

export function stepIndex(count: number, current: number, step: number): number {
  const base = current >= 0 ? current : step > 0 ? -1 : 0;
  return (base + step + count) % count;
}

function normalize(value: string): string {
  return value.trim().toLocaleLowerCase();
}
