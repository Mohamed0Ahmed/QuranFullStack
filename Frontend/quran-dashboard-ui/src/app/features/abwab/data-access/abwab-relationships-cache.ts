import { Provider } from '@angular/core';

import { AsyncKeyValueStore, InMemoryKeyValueStore } from '../../../core/caching/async-key-value-store';
import { IndexedDbKeyValueStore } from '../../../core/caching/indexed-db-key-value-store';
import { CacheClock, CacheEntry, PersistentCache } from '../../../core/caching/persistent-cache';
import { CategoryRelationshipListDto } from '../../../core/api/generated/models';

export const ABWAB_RELATIONSHIPS_CACHE_DB_NAME = 'abwab-relationships-cache';
export const ABWAB_RELATIONSHIPS_CACHE_STORE_NAME = 'lists';

export function abwabRelationshipsCacheKey(categoryId: string, includeDeleted: boolean): string {
  return `relationships:${categoryId}:${includeDeleted ? 'with-deleted' : 'active'}`;
}

export interface AbwabRelationshipsCacheOptions {
  readonly store?: AsyncKeyValueStore<CacheEntry<CategoryRelationshipListDto>>;
  readonly clock?: CacheClock;
}

export class AbwabRelationshipsCache {
  private readonly cache: PersistentCache<CategoryRelationshipListDto>;

  constructor(options: AbwabRelationshipsCacheOptions = {}) {
    this.cache = new PersistentCache<CategoryRelationshipListDto>({
      store: options.store ?? AbwabRelationshipsCache.createDefaultStore(),
      clock: options.clock,
    });
  }

  // IndexedDB when the runtime provides it, an in-memory fallback otherwise (SSR, non-browser test
  // environments), so constructing the cache never throws.
  private static createDefaultStore(): AsyncKeyValueStore<CacheEntry<CategoryRelationshipListDto>> {
    return typeof indexedDB !== 'undefined'
      ? new IndexedDbKeyValueStore<CacheEntry<CategoryRelationshipListDto>>({
          databaseName: ABWAB_RELATIONSHIPS_CACHE_DB_NAME,
          storeName: ABWAB_RELATIONSHIPS_CACHE_STORE_NAME,
        })
      : new InMemoryKeyValueStore<CacheEntry<CategoryRelationshipListDto>>();
  }

  getCached(categoryId: string, includeDeleted: boolean): Promise<CategoryRelationshipListDto | undefined> {
    return this.cache.get(abwabRelationshipsCacheKey(categoryId, includeDeleted));
  }

  store(categoryId: string, includeDeleted: boolean, list: CategoryRelationshipListDto): Promise<void> {
    return this.cache.set(abwabRelationshipsCacheKey(categoryId, includeDeleted), list);
  }

  // Both projections of a category are invalidated together: a delete moves a row from one to the
  // other, so keeping either stale would show a relationship that no longer belongs there.
  async invalidate(categoryId: string): Promise<void> {
    await this.cache.invalidate(abwabRelationshipsCacheKey(categoryId, false));
    await this.cache.invalidate(abwabRelationshipsCacheKey(categoryId, true));
  }
}

// Provided rather than constructed inside the facade so a test can swap in a cache with its own
// store/clock without reaching into facade internals.
export const ABWAB_RELATIONSHIPS_CACHE_PROVIDER: Provider = {
  provide: AbwabRelationshipsCache,
  useFactory: () => new AbwabRelationshipsCache(),
};
