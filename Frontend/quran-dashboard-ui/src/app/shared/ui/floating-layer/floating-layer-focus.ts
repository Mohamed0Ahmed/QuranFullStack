export class FloatingLayerFocus {
  private returnTarget: HTMLElement | null = null;
  private taken = false;

  constructor(private readonly layer: HTMLElement) {}

  capture(active: Element | null): void {
    this.returnTarget = active instanceof HTMLElement && active !== this.body ? active : null;
  }

  take(target: HTMLElement): void {
    this.taken = true;
    target.focus();
  }

  restore(anchor: HTMLElement | null): void {
    const target = anchor?.isConnected === true ? anchor : this.returnTarget;
    if (target?.isConnected === true) {
      target.focus();
    }
  }

  restoreWhenStillHeld(anchor: HTMLElement | null, active: Element | null): void {
    const heldInside =
      this.layer.contains(active) || (this.taken && (active === null || active === this.body));
    if (heldInside) {
      this.restore(anchor);
    }
  }

  private get body(): HTMLElement | null {
    return this.layer.ownerDocument.body;
  }
}

export function prepareCursorHostFocus(host: HTMLElement, layer: HTMLElement): HTMLElement {
  if (host === layer && !host.hasAttribute('tabindex')) {
    host.setAttribute('tabindex', '-1');
  }
  return host;
}

export function entryItem(items: readonly HTMLElement[]): HTMLElement | null {
  const selected = items.find(
    (item) =>
      item.getAttribute('aria-selected') === 'true' || item.getAttribute('aria-current') === 'true',
  );
  return selected ?? items[0] ?? null;
}

export function containsPointerTarget(
  target: Node,
  candidates: readonly (HTMLElement | null)[],
): boolean {
  return candidates.some((candidate) => candidate?.contains(target) === true);
}
