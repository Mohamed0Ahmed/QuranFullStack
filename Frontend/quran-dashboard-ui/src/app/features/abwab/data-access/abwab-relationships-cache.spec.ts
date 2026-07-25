import { describe, expect, it } from 'vitest';

import { InMemoryKeyValueStore } from '../../../core/caching/async-key-value-store';
import { CacheEntry } from '../../../core/caching/persistent-cache';
import { CategoryRelationshipListDto } from '../../../core/api/generated/models';
import { AbwabRelationshipsCache, abwabRelationshipsCacheKey } from './abwab-relationships-cache';

function list(categoryId: string, relationshipCount: number): CategoryRelationshipListDto {
  return {
    schemaVersion: 1,
    categoryId,
    expectedTimelineGeneration: { generation: 1 },
    serverTimeUtc: new Date().toISOString(),
    relationships: Array.from({ length: relationshipCount }, (_, index) => ({
      categoryRelationshipId: `relationship-${index}`,
      relationshipType: 0 as const,
      isDirectional: false,
      from: { categoryId, name: 'باب أ', path: [], isDeleted: false },
      to: { categoryId: `other-${index}`, name: 'باب ب', path: [], isDeleted: false },
      isDeleted: false,
      version: 1,
    })),
  };
}

function createCache() {
  const store = new InMemoryKeyValueStore<CacheEntry<CategoryRelationshipListDto>>();
  return new AbwabRelationshipsCache({ store });
}

describe('AbwabRelationshipsCache', () => {
  it('addresses the projection per (category, includeDeleted) rather than under one global key', () => {
    expect(abwabRelationshipsCacheKey('cat-1', false)).not.toBe(abwabRelationshipsCacheKey('cat-1', true));
    expect(abwabRelationshipsCacheKey('cat-1', false)).not.toBe(abwabRelationshipsCacheKey('cat-2', false));
  });

  it('has no cached projection before the first store', async () => {
    const cache = createCache();
    expect(await cache.getCached('cat-1', false)).toBeUndefined();
  });

  it('returns the stored projection until invalidated', async () => {
    const cache = createCache();
    const stored = list('cat-1', 2);
    await cache.store('cat-1', false, stored);
    expect(await cache.getCached('cat-1', false)).toEqual(stored);
  });

  it('the active and with-deleted projections of one category are stored independently', async () => {
    const cache = createCache();
    await cache.store('cat-1', false, list('cat-1', 1));
    await cache.store('cat-1', true, list('cat-1', 3));

    expect((await cache.getCached('cat-1', false))!.relationships).toHaveLength(1);
    expect((await cache.getCached('cat-1', true))!.relationships).toHaveLength(3);
  });

  it('post-mutation: invalidate() clears BOTH projections of the category, because a delete moves a row between them', async () => {
    const cache = createCache();
    await cache.store('cat-1', false, list('cat-1', 1));
    await cache.store('cat-1', true, list('cat-1', 3));

    await cache.invalidate('cat-1');

    expect(await cache.getCached('cat-1', false)).toBeUndefined();
    expect(await cache.getCached('cat-1', true)).toBeUndefined();
  });

  it('invalidating one category leaves another category cached', async () => {
    const cache = createCache();
    await cache.store('cat-1', false, list('cat-1', 1));
    await cache.store('cat-2', false, list('cat-2', 1));

    await cache.invalidate('cat-1');

    expect(await cache.getCached('cat-1', false)).toBeUndefined();
    expect(await cache.getCached('cat-2', false)).toBeDefined();
  });
});
