import { AsyncKeyValueStore } from './async-key-value-store';
import { IndexedDbKeyValueStore } from './indexed-db-key-value-store';

export interface CacheClock {
  now(): number;
}

const SYSTEM_CLOCK: CacheClock = { now: () => Date.now() };

export interface CacheEntry<V> {
  readonly value: V;
  readonly storedAt: number;
  readonly lastAccess: number;
}

export interface PersistentCacheOptions<V> {
  readonly store: AsyncKeyValueStore<CacheEntry<V>>;
  // 0/undefined = entries never expire; a positive value is the max age in ms since `storedAt`.
  readonly ttlMs?: number;
  // 0/undefined = unbounded; a positive value caps the entry count, evicting the least-recently-accessed.
  readonly maxEntries?: number;
  readonly clock?: CacheClock;
}

// Generic persistent cache: TTL freshness + LRU capacity over any AsyncKeyValueStore, backed by IndexedDB
// by default. It is intentionally value-only (no HTTP, no domain shape) — features compose it with their
// own loaders. In-flight loads are deduped in memory so a burst of getOrLoad calls issues one load.
export class PersistentCache<V> {
  private readonly store: AsyncKeyValueStore<CacheEntry<V>>;
  private readonly ttlMs: number;
  private readonly maxEntries: number;
  private readonly clock: CacheClock;
  private readonly inFlight = new Map<string, Promise<V>>();

  constructor(options: PersistentCacheOptions<V>) {
    this.store = options.store;
    this.ttlMs = options.ttlMs ?? 0;
    this.maxEntries = options.maxEntries ?? 0;
    this.clock = options.clock ?? SYSTEM_CLOCK;
  }

  static backedByIndexedDb<V>(
    options: {
      databaseName: string;
      storeName: string;
      factory?: IDBFactory;
    } & Omit<PersistentCacheOptions<V>, 'store'>,
  ): PersistentCache<V> {
    const store = new IndexedDbKeyValueStore<CacheEntry<V>>({
      databaseName: options.databaseName,
      storeName: options.storeName,
      factory: options.factory,
    });
    return new PersistentCache<V>({ ...options, store });
  }

  async get(key: string): Promise<V | undefined> {
    const entry = await this.store.get(key);
    if (!entry) {
      return undefined;
    }
    if (this.isExpired(entry)) {
      await this.store.delete(key);
      return undefined;
    }
    await this.store.set(key, { ...entry, lastAccess: this.clock.now() });
    return entry.value;
  }

  async has(key: string): Promise<boolean> {
    return (await this.get(key)) !== undefined;
  }

  async set(key: string, value: V): Promise<void> {
    const now = this.clock.now();
    await this.store.set(key, { value, storedAt: now, lastAccess: now });
    await this.enforceCapacity(key);
  }

  async getOrLoad(key: string, loader: () => Promise<V>): Promise<V> {
    const cached = await this.get(key);
    if (cached !== undefined) {
      return cached;
    }

    const pending = this.inFlight.get(key);
    if (pending) {
      return pending;
    }

    const load = loader()
      .then(async (value) => {
        await this.set(key, value);
        return value;
      })
      .finally(() => this.inFlight.delete(key));

    this.inFlight.set(key, load);
    return load;
  }

  async invalidate(key: string): Promise<void> {
    await this.store.delete(key);
  }

  async clear(): Promise<void> {
    await this.store.clear();
  }

  private isExpired(entry: CacheEntry<V>): boolean {
    return this.ttlMs > 0 && this.clock.now() - entry.storedAt > this.ttlMs;
  }

  private async enforceCapacity(justWritten: string): Promise<void> {
    if (this.maxEntries <= 0) {
      return;
    }
    const keys = await this.store.keys();
    if (keys.length <= this.maxEntries) {
      return;
    }

    let oldestKey: string | null = null;
    let oldestAccess = Number.POSITIVE_INFINITY;
    for (const key of keys) {
      if (key === justWritten) {
        continue;
      }
      const entry = await this.store.get(key);
      if (entry && entry.lastAccess < oldestAccess) {
        oldestAccess = entry.lastAccess;
        oldestKey = key;
      }
    }

    if (oldestKey !== null) {
      await this.store.delete(oldestKey);
    }
  }
}
