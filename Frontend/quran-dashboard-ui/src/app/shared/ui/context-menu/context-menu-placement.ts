const VIEWPORT_MARGIN = 8;

export interface MenuPlacement {
  readonly left: number;
  readonly top: number;
}

export interface MenuAnchor {
  readonly x: number;
  readonly y: number;
}

export interface MenuSize {
  readonly width: number;
  readonly height: number;
}

export interface MenuViewport {
  readonly width: number;
  readonly height: number;
}

export function placeContextMenu(
  anchor: MenuAnchor,
  size: MenuSize,
  viewport: MenuViewport,
  direction: 'ltr' | 'rtl',
): MenuPlacement {
  const rtl = direction === 'rtl';

  let left = rtl ? anchor.x - size.width : anchor.x;
  if (rtl ? left < VIEWPORT_MARGIN : left + size.width > viewport.width - VIEWPORT_MARGIN) {
    left = rtl ? anchor.x : anchor.x - size.width;
  }

  let top = anchor.y;
  if (top + size.height > viewport.height - VIEWPORT_MARGIN) {
    top = anchor.y - size.height;
  }

  return {
    left: clamp(left, VIEWPORT_MARGIN, viewport.width - size.width - VIEWPORT_MARGIN),
    top: clamp(top, VIEWPORT_MARGIN, viewport.height - size.height - VIEWPORT_MARGIN),
  };
}

export function resolveMenuDirection(host: Element): 'ltr' | 'rtl' {
  return host.closest('[dir]')?.getAttribute('dir') === 'rtl' ? 'rtl' : 'ltr';
}

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(value, max));
}
