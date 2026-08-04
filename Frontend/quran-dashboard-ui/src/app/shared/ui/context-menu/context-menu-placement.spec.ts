import { describe, expect, it } from 'vitest';

import { placeContextMenu } from './context-menu-placement';

// F-69: the RTL placement / viewport-flip / clamp arithmetic is the whole correctness surface of
// qd-context-menu, and jsdom cannot measure boxes — so the math is pinned here on plain inputs.
describe('placeContextMenu', () => {
  const SIZE = { width: 200, height: 150 };
  const VIEWPORT = { width: 1000, height: 800 };

  it('rtl default: extends toward inline-start, left = anchor.x − width', () => {
    expect(placeContextMenu({ x: 500, y: 100 }, SIZE, VIEWPORT, 'rtl')).toEqual({ left: 300, top: 100 });
  });

  it('ltr default: extends toward inline-end, left = anchor.x', () => {
    expect(placeContextMenu({ x: 500, y: 100 }, SIZE, VIEWPORT, 'ltr')).toEqual({ left: 500, top: 100 });
  });

  it('viewport-edge flip: rtl flips forward at the start edge, ltr flips back at the end edge, and the block axis flips up at the bottom', () => {
    expect(placeContextMenu({ x: 100, y: 100 }, SIZE, VIEWPORT, 'rtl')).toEqual({ left: 100, top: 100 });
    expect(placeContextMenu({ x: 950, y: 100 }, SIZE, VIEWPORT, 'ltr')).toEqual({ left: 750, top: 100 });
    expect(placeContextMenu({ x: 500, y: 700 }, SIZE, VIEWPORT, 'rtl')).toEqual({ left: 300, top: 550 });
  });

  it('clamp: a result outside the 8px margin is pinned to it on both axes, min and max', () => {
    expect(placeContextMenu({ x: 4, y: 4 }, SIZE, VIEWPORT, 'ltr')).toEqual({ left: 8, top: 8 });
    expect(placeContextMenu({ x: 996, y: 796 }, SIZE, VIEWPORT, 'rtl')).toEqual({ left: 792, top: 642 });
  });
});
