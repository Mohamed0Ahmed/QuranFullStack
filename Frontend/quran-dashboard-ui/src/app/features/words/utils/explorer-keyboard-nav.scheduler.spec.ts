import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import {
  EXPLORER_KEYBOARD_NAV_DEBOUNCE_MS,
  ExplorerKeyboardNavScheduler,
} from './explorer-keyboard-nav.scheduler';

describe('ExplorerKeyboardNavScheduler', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('coalesces rapid key presses into one final callback', () => {
    const callback = vi.fn();
    const scheduler = new ExplorerKeyboardNavScheduler(callback);

    scheduler.schedule('first');
    scheduler.schedule('final');
    vi.advanceTimersByTime(EXPLORER_KEYBOARD_NAV_DEBOUNCE_MS - 1);

    expect(callback).not.toHaveBeenCalled();

    vi.advanceTimersByTime(1);

    expect(callback).toHaveBeenCalledTimes(1);
    expect(callback).toHaveBeenCalledWith('final');
  });

  it('cancels a pending callback', () => {
    const callback = vi.fn();
    const scheduler = new ExplorerKeyboardNavScheduler(callback);

    scheduler.schedule('target');
    scheduler.cancel();
    vi.advanceTimersByTime(EXPLORER_KEYBOARD_NAV_DEBOUNCE_MS);

    expect(callback).not.toHaveBeenCalled();
    expect(scheduler.hasPending()).toBe(false);
  });
});
