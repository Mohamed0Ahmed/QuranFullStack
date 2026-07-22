// The narrow async key/value port every persistent cache is built on. IndexedDB is the production backing;
// the in-memory implementation is the SSR/test fallback. Keeping the port this small means cache logic can
// be tested against a faithful double while the IndexedDB adapter is tested against the real algorithm.
export interface AsyncKeyValueStore<V> {
  get(key: string): Promise<V | undefined>;
  set(key: string, value: V): Promise<void>;
  delete(key: string): Promise<void>;
  keys(): Promise<string[]>;
  clear(): Promise<void>;
}

export class InMemoryKeyValueStore<V> implements AsyncKeyValueStore<V> {
  private readonly entries = new Map<string, V>();

  async get(key: string): Promise<V | undefined> {
    return this.entries.get(key);
  }

  // Clone on write to mirror IndexedDB's structured-clone-on-put, so the double can't hide a
  // shared-reference bug the real backing would not have.
  async set(key: string, value: V): Promise<void> {
    this.entries.set(key, structuredClone(value));
  }

  async delete(key: string): Promise<void> {
    this.entries.delete(key);
  }

  async keys(): Promise<string[]> {
    return [...this.entries.keys()];
  }

  async clear(): Promise<void> {
    this.entries.clear();
  }
}
