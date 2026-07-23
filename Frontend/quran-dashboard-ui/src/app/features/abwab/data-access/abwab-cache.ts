import { AsyncKeyValueStore, InMemoryKeyValueStore } from '../../../core/caching/async-key-value-store';
import { IndexedDbKeyValueStore } from '../../../core/caching/indexed-db-key-value-store';
import { CacheClock, CacheEntry, PersistentCache } from '../../../core/caching/persistent-cache';
import { AbwabTreeSnapshotDto } from '../../../core/api/generated/models';

export const ABWAB_TREE_CACHE_DB_NAME = 'abwab-core-cache';
export const ABWAB_TREE_CACHE_STORE_NAME = 'snapshots';
export const ABWAB_TREE_CACHE_KEY = 'tree';

export interface AbwabTreeCacheOptions {
  readonly store?: AsyncKeyValueStore<CacheEntry<AbwabTreeSnapshotDto>>;
  readonly clock?: CacheClock;
}

// T069: core cache rules for the Abwab tree snapshot, built on the reused 028 §14.1 primitive
// (PersistentCache over an IndexedDB-backed AsyncKeyValueStore). The tree snapshot is cached under
// ONE stable key (there is only one snapshot resource); every successful mutation and every
// generation/revision conflict invalidates it, forcing the next read to go back to the server —
// there is no client-side reconciliation of a stale snapshot.
export class AbwabTreeCache {
  private readonly cache: PersistentCache<AbwabTreeSnapshotDto>;

  constructor(options: AbwabTreeCacheOptions = {}) {
    this.cache = new PersistentCache<AbwabTreeSnapshotDto>({
      store: options.store ?? AbwabTreeCache.createDefaultStore(),
      clock: options.clock,
    });
  }

  // IndexedDB when the runtime provides it (every real browser); an in-memory fallback otherwise
  // (SSR, non-browser test environments) so constructing this cache never throws — worst case the
  // snapshot cache does not persist across reloads, and the app still works from the network.
  private static createDefaultStore(): AsyncKeyValueStore<CacheEntry<AbwabTreeSnapshotDto>> {
    const hasIndexedDb = typeof indexedDB !== 'undefined';
    return hasIndexedDb
      ? new IndexedDbKeyValueStore<CacheEntry<AbwabTreeSnapshotDto>>({
          databaseName: ABWAB_TREE_CACHE_DB_NAME,
          storeName: ABWAB_TREE_CACHE_STORE_NAME,
        })
      : new InMemoryKeyValueStore<CacheEntry<AbwabTreeSnapshotDto>>();
  }

  getCached(): Promise<AbwabTreeSnapshotDto | undefined> {
    return this.cache.get(ABWAB_TREE_CACHE_KEY);
  }

  store(snapshot: AbwabTreeSnapshotDto): Promise<void> {
    return this.cache.set(ABWAB_TREE_CACHE_KEY, snapshot);
  }

  // Called after every successful mutation (post-mutation cache key) and after every
  // abwab.tree_revision_stale / abwab.timeline_generation_stale / abwab.row_stale conflict
  // (stale-cache handling) — both cases mean the cached snapshot no longer reflects the server.
  invalidate(): Promise<void> {
    return this.cache.invalidate(ABWAB_TREE_CACHE_KEY);
  }

  isStaleAgainst(cached: AbwabTreeSnapshotDto, latest: Pick<AbwabTreeSnapshotDto, 'treeRevision' | 'expectedTimelineGeneration'>): boolean {
    return (
      cached.treeRevision !== latest.treeRevision ||
      cached.expectedTimelineGeneration.generation !== latest.expectedTimelineGeneration.generation
    );
  }
}
