import { afterEach, describe, expect, it, vi } from 'vitest';

import { syncTableScrollbarGutter } from './table-scrollbar-gutter-sync';

describe('syncTableScrollbarGutter', () => {
  afterEach(() => {
    document.body.replaceChildren();
  });

  it('synchronizes the measured body gutter and cleans up both observers', () => {
    const observer = {
      observe: vi.fn(),
      unobserve: vi.fn(),
      disconnect: vi.fn(),
    };
    const mutation = {
      observe: vi.fn(),
      disconnect: vi.fn(),
    };
    vi.stubGlobal('ResizeObserver', vi.fn(() => observer));
    vi.stubGlobal('MutationObserver', vi.fn(() => mutation));
    vi.stubGlobal('requestAnimationFrame', vi.fn(() => 1));
    vi.stubGlobal('cancelAnimationFrame', vi.fn());
    const host = document.createElement('div');
    host.innerHTML = '<section class="scope"><div class="body"></div></section>';
    const body = host.querySelector('.body') as HTMLElement;
    Object.defineProperties(body, { offsetWidth: { value: 120 }, clientWidth: { value: 100 } });
    document.body.append(host);

    const disconnect = syncTableScrollbarGutter(host, '--gutter', '.body', '.scope');
    disconnect();

    expect((host.querySelector('.scope') as HTMLElement).style.getPropertyValue('--gutter')).toBe('20px');
    expect(observer.observe).toHaveBeenCalledWith(body);
    expect(observer.disconnect).toHaveBeenCalledOnce();
    expect(mutation.disconnect).toHaveBeenCalledOnce();
  });

  it('measures immediately without ResizeObserver and preserves requestAnimationFrame guards', () => {
    vi.stubGlobal('ResizeObserver', undefined);
    vi.stubGlobal('requestAnimationFrame', undefined);
    const host = document.createElement('div');
    host.innerHTML = '<section class="scope"><div class="body"></div></section>';
    const body = host.querySelector('.body') as HTMLElement;
    Object.defineProperties(body, { offsetWidth: { value: 100 }, clientWidth: { value: 92 } });

    const disconnect = syncTableScrollbarGutter(host, '--gutter', '.body', '.scope');
    disconnect();

    expect((host.querySelector('.scope') as HTMLElement).style.getPropertyValue('--gutter')).toBe('8px');
  });
});
