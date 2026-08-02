import { afterEach, describe, expect, it, vi } from 'vitest';
import { of } from 'rxjs';

import { IdlePreloadStrategy } from './idle-preload.strategy';

type IdleCallback = () => void;

const flushMicrotasks = () => new Promise<void>((resolve) => setTimeout(resolve, 0));

describe('IdlePreloadStrategy', () => {
  afterEach(() => {
    delete (globalThis as { requestIdleCallback?: unknown }).requestIdleCallback;
    vi.useRealTimers();
  });

  it('defers the chunk load until the idle callback fires', async () => {
    let idleCallback: IdleCallback | undefined;
    (globalThis as { requestIdleCallback?: unknown }).requestIdleCallback = (cb: IdleCallback) => {
      idleCallback = cb;
      return 1;
    };

    const load = vi.fn(() => of('chunk'));
    const emitted: unknown[] = [];
    new IdlePreloadStrategy().preload({}, load).subscribe((value) => emitted.push(value));

    await flushMicrotasks();
    expect(load).not.toHaveBeenCalled();

    idleCallback?.();
    await flushMicrotasks();
    expect(load).toHaveBeenCalledTimes(1);
    expect(emitted).toEqual(['chunk']);
  });

  it('falls back to a delayed load when requestIdleCallback is unavailable', async () => {
    vi.useFakeTimers();

    const load = vi.fn(() => of('chunk'));
    const emitted: unknown[] = [];
    new IdlePreloadStrategy().preload({}, load).subscribe((value) => emitted.push(value));

    await vi.advanceTimersByTimeAsync(1499);
    expect(load).not.toHaveBeenCalled();

    await vi.advanceTimersByTimeAsync(1);
    expect(load).toHaveBeenCalledTimes(1);
    expect(emitted).toEqual(['chunk']);
  });
});
