import { describe, expect, it } from 'vitest';

import {
  FLOATING_TYPE_AHEAD_WINDOW_MS,
  FloatingLayerKeyAction,
  nextTypeAheadState,
  resolveFloatingKeyAction,
  stepIndex,
  typeAheadMatchIndex,
} from './floating-layer-keyboard';

function keydown(key: string, init: KeyboardEventInit = {}): KeyboardEvent {
  return new KeyboardEvent('keydown', { key, ...init });
}

function itemsWithText(...labels: string[]): HTMLElement[] {
  return labels.map((label) => {
    const item = document.createElement('div');
    item.textContent = label;
    return item;
  });
}

describe('resolveFloatingKeyAction', () => {
  it.each([
    ['Escape', { kind: 'dismiss', reason: 'escape' }],
    ['Tab', { kind: 'dismiss', reason: 'tab' }],
    ['ArrowDown', { kind: 'step', step: 1 }],
    ['ArrowUp', { kind: 'step', step: -1 }],
    ['Home', { kind: 'edge', edge: 'first' }],
    ['End', { kind: 'edge', edge: 'last' }],
    ['b', { kind: 'type-ahead' }],
    ['F2', { kind: 'none' }],
  ] as [string, FloatingLayerKeyAction][])('routes %s in a navigable layer', (key, expected) => {
    expect(resolveFloatingKeyAction(keydown(key), true, 3)).toEqual(expected);
  });

  it('leaves every key but the two dismissals alone in a layer that does not navigate', () => {
    expect(resolveFloatingKeyAction(keydown('ArrowDown'), false, 3)).toEqual({ kind: 'none' });
    expect(resolveFloatingKeyAction(keydown('Escape'), false, 3)).toEqual({
      kind: 'dismiss',
      reason: 'escape',
    });
  });

  it('still dismisses a navigable layer that has no item to walk', () => {
    expect(resolveFloatingKeyAction(keydown('ArrowDown'), true, 0)).toEqual({ kind: 'none' });
    expect(resolveFloatingKeyAction(keydown('Tab'), true, 0)).toEqual({
      kind: 'dismiss',
      reason: 'tab',
    });
  });

  it.each([
    ['ctrlKey' as const],
    ['altKey' as const],
    ['metaKey' as const],
  ])('never steals a %s shortcut for type-ahead', (modifier) => {
    expect(resolveFloatingKeyAction(keydown('k', { [modifier]: true }), true, 3)).toEqual({
      kind: 'none',
    });
  });
});

describe('nextTypeAheadState', () => {
  it('drops a bare Space so it stays the focused item’s own activation key', () => {
    expect(nextTypeAheadState(null, ' ', 1_000)).toBeNull();
  });

  it('extends a prefix typed inside the window and restarts after it lapses', () => {
    const first = nextTypeAheadState(null, 'a', 1_000);
    expect(first).toEqual({ typed: 'a', at: 1_000 });

    const inside = nextTypeAheadState(first!, 'b', 1_000 + FLOATING_TYPE_AHEAD_WINDOW_MS);
    expect(inside).toEqual({ typed: 'ab', at: 1_000 + FLOATING_TYPE_AHEAD_WINDOW_MS });

    const lapsed = nextTypeAheadState(first!, 'b', 1_001 + FLOATING_TYPE_AHEAD_WINDOW_MS);
    expect(lapsed).toEqual({ typed: 'b', at: 1_001 + FLOATING_TYPE_AHEAD_WINDOW_MS });
  });
});

describe('typeAheadMatchIndex', () => {
  const ITEMS = itemsWithText('ألف', 'باء', 'تاء');

  it('wraps past the end of the list to reach an earlier match', () => {
    expect(typeAheadMatchIndex(ITEMS, ITEMS.length, 'ألف')).toBe(0);
  });

  it('reports no match instead of moving the cursor somewhere arbitrary', () => {
    expect(typeAheadMatchIndex(ITEMS, 0, 'ز')).toBe(-1);
  });
});

describe('stepIndex', () => {
  it.each([
    [-1, 1, 0],
    [-1, -1, 2],
    [2, 1, 0],
    [0, -1, 2],
  ])('steps from %i by %i to %i, wrapping at both ends', (current, step, expected) => {
    expect(stepIndex(3, current, step)).toBe(expected);
  });
});
