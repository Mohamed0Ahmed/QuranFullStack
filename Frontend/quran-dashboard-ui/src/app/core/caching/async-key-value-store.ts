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
