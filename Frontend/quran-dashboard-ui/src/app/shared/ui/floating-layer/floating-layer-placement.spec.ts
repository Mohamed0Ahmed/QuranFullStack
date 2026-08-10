import { describe, expect, it } from 'vitest';

import {
  FLOATING_ANCHOR_GAP,
  FLOATING_DEFAULT_ROOT_FONT_SIZE,
  FLOATING_VIEWPORT_MARGIN,
  floatingMaxBlockSize,
  placeFloatingLayer,
  pointerAnchorRect,
  resolveRootFontSize,
} from './floating-layer-placement';

const VIEWPORT = { width: 1080, height: 800 };
const SIZE = { width: 240, height: 200 };

function anchorAt(left: number, top: number, width = 120, height = 40) {
  return { left, top, right: left + width, bottom: top + height };
}

describe('placeFloatingLayer', () => {
  it('opens on the block-end side of the anchor when the layer fits below it', () => {
    const anchor = anchorAt(400, 200);

    const placement = placeFloatingLayer(anchor, SIZE, VIEWPORT, 'ltr');

    expect(placement.blockSide).toBe('block-end');
    expect(placement.flipped).toBe(false);
    expect(placement.top).toBe(anchor.bottom + FLOATING_ANCHOR_GAP);
  });

  // D34: a menu opened near the bottom edge must flip above its trigger rather than be clipped or
  // push the document taller.
  it('flips to the block-start side when the block-end side cannot hold it and the other side can', () => {
    const anchor = anchorAt(400, 700);

    const placement = placeFloatingLayer(anchor, SIZE, VIEWPORT, 'ltr');

    expect(placement.flipped).toBe(true);
    expect(placement.blockSide).toBe('block-start');
    expect(placement.top).toBe(anchor.top - FLOATING_ANCHOR_GAP - SIZE.height);
  });

  it('keeps the roomier side and shrinks its own scroller when neither side can hold the layer', () => {
    const anchor = anchorAt(400, 120, 120, 40);
    const shortViewport = { width: 1080, height: 300 };

    const placement = placeFloatingLayer(anchor, { width: 240, height: 900 }, shortViewport, 'ltr');

    expect(placement.flipped).toBe(false);
    expect(placement.maxBlockSize).toBe(
      shortViewport.height - FLOATING_VIEWPORT_MARGIN - (anchor.bottom + FLOATING_ANCHOR_GAP),
    );
    expect(placement.top + placement.maxBlockSize).toBeLessThanOrEqual(
      shortViewport.height - FLOATING_VIEWPORT_MARGIN,
    );
  });

  it.each([
    ['ltr', 400, 400],
    ['rtl', 400, 400 + 120 - SIZE.width],
  ] as const)('anchors the layer to the %s reading-start edge of the trigger', (direction, left, expected) => {
    const placement = placeFloatingLayer(anchorAt(left, 200), SIZE, VIEWPORT, direction);

    expect(placement.left).toBe(expected);
    expect(placement.inlineClamped).toBe(false);
  });

  it.each([
    ['ltr', 1000],
    ['rtl', 20],
  ] as const)('clamps the layer inside the viewport on the inline axis in %s', (direction, left) => {
    const placement = placeFloatingLayer(anchorAt(left, 200), SIZE, VIEWPORT, direction);

    expect(placement.inlineClamped).toBe(true);
    expect(placement.left).toBeGreaterThanOrEqual(FLOATING_VIEWPORT_MARGIN);
    expect(placement.left + SIZE.width).toBeLessThanOrEqual(VIEWPORT.width - FLOATING_VIEWPORT_MARGIN);
  });

  it('never reports a position that would put the layer outside the viewport on the block axis', () => {
    const placement = placeFloatingLayer(anchorAt(400, 780), SIZE, VIEWPORT, 'rtl');

    expect(placement.top).toBeGreaterThanOrEqual(FLOATING_VIEWPORT_MARGIN);
    expect(placement.top).toBeLessThanOrEqual(VIEWPORT.height - FLOATING_VIEWPORT_MARGIN);
  });

  it('caps the layer at min(60vh, 24rem) regardless of how much room the side has', () => {
    const roomy = placeFloatingLayer(anchorAt(400, 20), { width: 240, height: 5000 }, VIEWPORT, 'ltr');

    expect(roomy.maxBlockSize).toBe(floatingMaxBlockSize(VIEWPORT));
    expect(floatingMaxBlockSize({ width: 390, height: 500 })).toBe(300);
    expect(floatingMaxBlockSize({ width: 1440, height: 1200 })).toBe(384);
  });

  // The rem half of `min(60vh, 24rem)` is a rem, not 384 hard pixels: at a 20px root font the token
  // resolves to 480, and a JS cap frozen at 384 would clip the layer the stylesheet still allows.
  it('resolves the rem half of the cap against the given root font size', () => {
    expect(floatingMaxBlockSize({ width: 1440, height: 1200 }, 20)).toBe(480);
    expect(floatingMaxBlockSize({ width: 1440, height: 1200 }, 12)).toBe(288);

    const placement = placeFloatingLayer(
      anchorAt(400, 20),
      { width: 240, height: 5000 },
      { width: 1440, height: 1200 },
      'ltr',
      20,
    );
    expect(placement.maxBlockSize).toBe(480);
  });

  // Phase 7 folded the context-menu placement module into this helper: a pointer-invoked menu is
  // the same arithmetic against a zero-size anchor with no gap, so there is one collision truth.
  describe('pointer anchor', () => {
    const MENU = { width: 200, height: 150 };
    const MENU_VIEWPORT = { width: 1000, height: 800 };

    const atPoint = (x: number, y: number, direction: 'ltr' | 'rtl') =>
      placeFloatingLayer(pointerAnchorRect({ x, y }), MENU, MENU_VIEWPORT, direction, 16, 0);

    it.each([
      ['rtl', 500, 300],
      ['ltr', 500, 500],
    ] as const)('extends from the pointer toward the %s reading direction', (direction, x, expected) => {
      const placement = atPoint(x, 100, direction);

      expect(placement.left).toBe(expected);
      expect(placement.top).toBe(100);
    });

    it('flips above the pointer when the menu would not fit below it', () => {
      const placement = atPoint(500, 700, 'rtl');

      expect(placement.flipped).toBe(true);
      expect(placement.top).toBe(550);
    });

    it('clamps a pointer near either viewport corner back inside the margin', () => {
      expect(atPoint(4, 4, 'ltr').left).toBe(FLOATING_VIEWPORT_MARGIN);
      expect(atPoint(4, 4, 'ltr').top).toBe(FLOATING_VIEWPORT_MARGIN);
      expect(atPoint(996, 796, 'rtl').left).toBe(792);
      expect(atPoint(996, 796, 'rtl').inlineClamped).toBe(true);
    });
  });

  it('resolves the root font size from the host document, falling back to 16px', () => {
    document.documentElement.style.fontSize = '20px';
    expect(resolveRootFontSize(document.body)).toBe(20);

    document.documentElement.style.fontSize = '';
    expect(resolveRootFontSize(document.body)).toBe(FLOATING_DEFAULT_ROOT_FONT_SIZE);
  });
});
