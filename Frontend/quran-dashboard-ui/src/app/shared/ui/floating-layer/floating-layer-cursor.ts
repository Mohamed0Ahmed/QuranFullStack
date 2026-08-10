export const FLOATING_CURSOR_ATTRIBUTE = 'data-qd-floating-cursor';

let nextOptionId = 0;

export function focusedItemIndex(items: readonly HTMLElement[], active: Element | null): number {
  return active instanceof HTMLElement ? items.indexOf(active) : -1;
}

export function activeDescendantIndex(
  items: readonly HTMLElement[],
  cursorId: string | null,
): number {
  return cursorId === null ? -1 : items.findIndex((item) => item.id === cursorId);
}

export function ensureOptionId(item: HTMLElement): string {
  if (item.id === '') {
    item.id = `qd-floating-option-${nextOptionId++}`;
  }
  return item.id;
}

export function markCursorItem(layer: HTMLElement, item: HTMLElement | null): void {
  for (const marked of Array.from(
    layer.querySelectorAll<HTMLElement>(`[${FLOATING_CURSOR_ATTRIBUTE}]`),
  )) {
    if (marked !== item) {
      marked.removeAttribute(FLOATING_CURSOR_ATTRIBUTE);
    }
  }
  item?.setAttribute(FLOATING_CURSOR_ATTRIBUTE, 'true');
}

export function clearActiveDescendant(layer: HTMLElement, control: HTMLElement | null): void {
  markCursorItem(layer, null);
  layer.removeAttribute('aria-activedescendant');
  layer.querySelector('[aria-activedescendant]')?.removeAttribute('aria-activedescendant');
  control?.removeAttribute('aria-activedescendant');
}
