import { describe, expect, it } from 'vitest';

import { InMemoryKeyValueStore } from '../../../core/caching/async-key-value-store';
import { CacheEntry } from '../../../core/caching/persistent-cache';
import { AbwabTreeSnapshotDto } from '../../../core/api/generated/models';
import { AbwabTreeCache } from './abwab-cache';

function snapshot(treeRevision: number, generation: number): AbwabTreeSnapshotDto {
  return {
    schemaVersion: 1,
    treeRevision,
    expectedTimelineGeneration: { generation },
    serverTimeUtc: new Date().toISOString(),
    sections: [],
    categories: [],
    allCategoriesProjection: [],
  };
}

function createCache() {
  const store = new InMemoryKeyValueStore<CacheEntry<AbwabTreeSnapshotDto>>();
  return new AbwabTreeCache({ store });
}

describe('AbwabTreeCache (T069 core cache rules)', () => {
  it('has no cached snapshot before the first store', async () => {
    const cache = createCache();
    expect(await cache.getCached()).toBeUndefined();
  });

  it('returns the stored snapshot until invalidated', async () => {
    const cache = createCache();
    const stored = snapshot(1, 1);
    await cache.store(stored);
    expect(await cache.getCached()).toEqual(stored);
  });

  it('post-mutation: invalidate() clears the cached snapshot so the next read refetches', async () => {
    const cache = createCache();
    await cache.store(snapshot(1, 1));
    await cache.invalidate();
    expect(await cache.getCached()).toBeUndefined();
  });

  it('stale-cache: a cached snapshot is stale once the server reports a newer TreeRevision', () => {
    const cache = createCache();
    expect(cache.isStaleAgainst(snapshot(1, 1), snapshot(2, 1))).toBe(true);
  });

  it('stale-cache: a cached snapshot is stale once the server reports a newer TimelineGeneration', () => {
    const cache = createCache();
    expect(cache.isStaleAgainst(snapshot(1, 1), snapshot(1, 2))).toBe(true);
  });

  it('a cached snapshot matching both the current TreeRevision and TimelineGeneration is fresh', () => {
    const cache = createCache();
    expect(cache.isStaleAgainst(snapshot(3, 5), snapshot(3, 5))).toBe(false);
  });
});
