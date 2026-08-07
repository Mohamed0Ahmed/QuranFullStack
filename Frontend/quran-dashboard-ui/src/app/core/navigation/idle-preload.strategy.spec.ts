import { describe, expect, it, vi } from 'vitest';
import { of } from 'rxjs';

import { IdlePreloadStrategy } from './idle-preload.strategy';

type IdleCallback = () => void;

const flushMicrotasks = () => new Promise<void>((resolve) => setTimeout(resolve, 0));

describe('IdlePreloadStrategy', () => {
  it('defers the chunk load until the idle callback fires', async () => {
    let idleCallback: IdleCallback | undefined;
    vi.stubGlobal('requestIdleCallback', (cb: IdleCallback) => {
      idleCallback = cb;
      return 1;
    });

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

  // jsdom never defines requestIdleCallback, and the test-setup safety net unstubs the one
  // the previous test installs even if that test throws — so this fallback branch is reachable.
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
