import { describe, expect, it, vi } from 'vitest';

import { InMemoryKeyValueStore } from './async-key-value-store';
import { CacheClock, CacheEntry, PersistentCache } from './persistent-cache';

function mutableClock(start = 0): CacheClock & { advance(ms: number): void } {
  let current = start;
  return {
    now: () => current,
    advance: (ms: number) => {
      current += ms;
    },
  };
}

function newCache<V>(options: { maxEntries?: number; ttlMs?: number } = {}) {
  const clock = mutableClock();
  const store = new InMemoryKeyValueStore<CacheEntry<V>>();
  const cache = new PersistentCache<V>({ store, clock, ...options });
  return { cache, clock, store };
}

describe('PersistentCache', () => {
  it('stores a value and reads it back', async () => {
    const { cache } = newCache<{ n: number }>();

    await cache.set('a', { n: 1 });

    expect(await cache.get('a')).toEqual({ n: 1 });
    expect(await cache.has('a')).toBe(true);
    expect(await cache.get('missing')).toBeUndefined();
  });

  it('getOrLoad loads once, dedupes concurrent reads, then serves from the store', async () => {
    const { cache } = newCache<number>();
    const loader = vi.fn(async () => 42);

    const [first, second] = await Promise.all([
      cache.getOrLoad('k', loader),
      cache.getOrLoad('k', loader),
    ]);
    const third = await cache.getOrLoad('k', loader);

    expect(first).toBe(42);
    expect(second).toBe(42);
    expect(third).toBe(42);
    expect(loader).toHaveBeenCalledTimes(1);
  });

  it('getOrLoad does not cache a rejected load and retries next time', async () => {
    const { cache } = newCache<number>();
    const loader = vi
      .fn()
      .mockRejectedValueOnce(new Error('boom'))
      .mockResolvedValueOnce(7);

    await expect(cache.getOrLoad('k', loader)).rejects.toThrow('boom');
    expect(await cache.getOrLoad('k', loader)).toBe(7);
    expect(loader).toHaveBeenCalledTimes(2);
  });

  it('treats an entry older than ttlMs as absent and evicts it', async () => {
    const { cache, clock, store } = newCache<string>({ ttlMs: 1000 });

    await cache.set('k', 'v');
    clock.advance(1001);

    expect(await cache.get('k')).toBeUndefined();
    expect(await store.keys()).not.toContain('k');
  });

  it('evicts the least-recently-accessed entry when it grows past maxEntries', async () => {
    const { cache, clock } = newCache<number>({ maxEntries: 2 });

    await cache.set('a', 1);
    clock.advance(1);
    await cache.set('b', 2);
    clock.advance(1);
    // Touch 'a' so 'b' becomes the least-recently-accessed entry.
    await cache.get('a');
    clock.advance(1);
    await cache.set('c', 3);

    expect(await cache.get('b')).toBeUndefined();
    expect(await cache.get('a')).toBe(1);
    expect(await cache.get('c')).toBe(3);
  });

  it('invalidate removes one key and clear empties the cache', async () => {
    const { cache } = newCache<number>();

    await cache.set('a', 1);
    await cache.set('b', 2);
    await cache.invalidate('a');

    expect(await cache.get('a')).toBeUndefined();
    expect(await cache.get('b')).toBe(2);

    await cache.clear();
    expect(await cache.get('b')).toBeUndefined();
  });
});
