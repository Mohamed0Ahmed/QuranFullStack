import { Provider } from '@angular/core';

import { AsyncKeyValueStore, InMemoryKeyValueStore } from '../../../core/caching/async-key-value-store';
import { IndexedDbKeyValueStore } from '../../../core/caching/indexed-db-key-value-store';
import { CacheClock, CacheEntry, PersistentCache } from '../../../core/caching/persistent-cache';
import { DoorTemplateDetailDto, DoorTemplateListDto } from '../../../core/api/generated/models';

export const ABWAB_TEMPLATES_CACHE_DB_NAME = 'abwab-templates-cache';
export const ABWAB_TEMPLATES_CACHE_STORE_NAME = 'templates';

type CachedTemplateProjection = DoorTemplateListDto | DoorTemplateDetailDto;

export function abwabTemplateListCacheKey(includeDeleted: boolean): string {
  return `templates:list:${includeDeleted ? 'with-deleted' : 'active'}`;
}

export function abwabTemplateDetailCacheKey(doorTemplateId: string, includeDeleted: boolean): string {
  return `templates:detail:${doorTemplateId}:${includeDeleted ? 'with-deleted' : 'active'}`;
}

export interface AbwabTemplatesCacheOptions {
  readonly store?: AsyncKeyValueStore<CacheEntry<CachedTemplateProjection>>;
  readonly clock?: CacheClock;
}

export class AbwabTemplatesCache {
  private readonly cache: PersistentCache<CachedTemplateProjection>;

  constructor(options: AbwabTemplatesCacheOptions = {}) {
    this.cache = new PersistentCache<CachedTemplateProjection>({
      store: options.store ?? AbwabTemplatesCache.createDefaultStore(),
      clock: options.clock,
    });
  }

  private static createDefaultStore(): AsyncKeyValueStore<CacheEntry<CachedTemplateProjection>> {
    return typeof indexedDB !== 'undefined'
      ? new IndexedDbKeyValueStore<CacheEntry<CachedTemplateProjection>>({
          databaseName: ABWAB_TEMPLATES_CACHE_DB_NAME,
          storeName: ABWAB_TEMPLATES_CACHE_STORE_NAME,
        })
      : new InMemoryKeyValueStore<CacheEntry<CachedTemplateProjection>>();
  }

  getCachedList(includeDeleted: boolean): Promise<DoorTemplateListDto | undefined> {
    return this.cache.get(abwabTemplateListCacheKey(includeDeleted)) as Promise<DoorTemplateListDto | undefined>;
  }

  storeList(includeDeleted: boolean, list: DoorTemplateListDto): Promise<void> {
    return this.cache.set(abwabTemplateListCacheKey(includeDeleted), list);
  }

  getCachedDetail(doorTemplateId: string, includeDeleted: boolean): Promise<DoorTemplateDetailDto | undefined> {
    return this.cache.get(abwabTemplateDetailCacheKey(doorTemplateId, includeDeleted)) as Promise<DoorTemplateDetailDto | undefined>;
  }

  storeDetail(doorTemplateId: string, includeDeleted: boolean, detail: DoorTemplateDetailDto): Promise<void> {
    return this.cache.set(abwabTemplateDetailCacheKey(doorTemplateId, includeDeleted), detail);
  }

  // Both projections invalidate together with the list: a node edit changes the detail, a delete
  // moves a template between the two list projections, and an apply changes neither predictably —
  // there is no client-side reconciliation, only invalidate + re-fetch.
  async invalidate(doorTemplateId: string | null): Promise<void> {
    await this.cache.invalidate(abwabTemplateListCacheKey(false));
    await this.cache.invalidate(abwabTemplateListCacheKey(true));

    if (doorTemplateId !== null) {
      await this.cache.invalidate(abwabTemplateDetailCacheKey(doorTemplateId, false));
      await this.cache.invalidate(abwabTemplateDetailCacheKey(doorTemplateId, true));
    }
  }
}

export const ABWAB_TEMPLATES_CACHE_PROVIDER: Provider = {
  provide: AbwabTemplatesCache,
  useFactory: () => new AbwabTemplatesCache(),
};
