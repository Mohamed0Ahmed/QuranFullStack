export const FLOATING_VIEWPORT_MARGIN = 8;
export const FLOATING_ANCHOR_GAP = 4;
export const FLOATING_MAX_BLOCK_SIZE_REM = 24;
export const FLOATING_DEFAULT_ROOT_FONT_SIZE = 16;
export const FLOATING_MAX_BLOCK_SIZE_CAP = FLOATING_MAX_BLOCK_SIZE_REM * FLOATING_DEFAULT_ROOT_FONT_SIZE;
export const FLOATING_VIEWPORT_BLOCK_FRACTION = 0.6;

export interface FloatingAnchorRect {
  readonly left: number;
  readonly top: number;
  readonly right: number;
  readonly bottom: number;
}

export interface FloatingLayerSize {
  readonly width: number;
  readonly height: number;
}

export interface FloatingViewport {
  readonly width: number;
  readonly height: number;
}

export type FloatingBlockSide = 'block-end' | 'block-start';

export interface FloatingPlacement {
  readonly left: number;
  readonly top: number;
  readonly maxBlockSize: number;
  readonly blockSide: FloatingBlockSide;
  readonly flipped: boolean;
  readonly inlineClamped: boolean;
}

export function floatingMaxBlockSize(
  viewport: FloatingViewport,
  rootFontSize: number = FLOATING_DEFAULT_ROOT_FONT_SIZE,
): number {
  return Math.min(
    viewport.height * FLOATING_VIEWPORT_BLOCK_FRACTION,
    FLOATING_MAX_BLOCK_SIZE_REM * rootFontSize,
  );
}

export function placeFloatingLayer(
  anchor: FloatingAnchorRect,
  size: FloatingLayerSize,
  viewport: FloatingViewport,
  direction: 'ltr' | 'rtl',
  rootFontSize: number = FLOATING_DEFAULT_ROOT_FONT_SIZE,
): FloatingPlacement {
  const cap = floatingMaxBlockSize(viewport, rootFontSize);
  const spaceAfter = viewport.height - FLOATING_VIEWPORT_MARGIN - (anchor.bottom + FLOATING_ANCHOR_GAP);
  const spaceBefore = anchor.top - FLOATING_ANCHOR_GAP - FLOATING_VIEWPORT_MARGIN;
  const desired = Math.min(size.height, cap);

  const fitsAfter = desired <= spaceAfter;
  const flipped = !fitsAfter && spaceBefore > spaceAfter;
  const blockSide: FloatingBlockSide = flipped ? 'block-start' : 'block-end';

  const available = Math.max(0, flipped ? spaceBefore : spaceAfter);
  const maxBlockSize = Math.max(0, Math.min(cap, available));
  const blockSize = Math.min(size.height, maxBlockSize);

  const top = flipped
    ? anchor.top - FLOATING_ANCHOR_GAP - blockSize
    : anchor.bottom + FLOATING_ANCHOR_GAP;

  const preferredLeft = direction === 'rtl' ? anchor.right - size.width : anchor.left;
  const maxLeft = viewport.width - size.width - FLOATING_VIEWPORT_MARGIN;
  const left = clamp(preferredLeft, FLOATING_VIEWPORT_MARGIN, Math.max(FLOATING_VIEWPORT_MARGIN, maxLeft));

  return {
    left,
    top: clamp(top, FLOATING_VIEWPORT_MARGIN, Math.max(FLOATING_VIEWPORT_MARGIN, viewport.height - blockSize - FLOATING_VIEWPORT_MARGIN)),
    maxBlockSize,
    blockSide,
    flipped,
    inlineClamped: left !== preferredLeft,
  };
}

export function resolveFloatingDirection(host: Element): 'ltr' | 'rtl' {
  return host.closest('[dir]')?.getAttribute('dir') === 'rtl' ? 'rtl' : 'ltr';
}

export function resolveRootFontSize(host: Element): number {
  const ownerDocument = host.ownerDocument;
  const view = ownerDocument?.defaultView ?? null;
  if (view === null) {
    return FLOATING_DEFAULT_ROOT_FONT_SIZE;
  }
  const parsed = Number.parseFloat(view.getComputedStyle(ownerDocument.documentElement).fontSize);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : FLOATING_DEFAULT_ROOT_FONT_SIZE;
}

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(value, max));
}
