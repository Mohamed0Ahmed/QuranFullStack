import { AsyncKeyValueStore } from './async-key-value-store';

export interface IndexedDbKeyValueStoreOptions {
  readonly databaseName: string;
  readonly storeName: string;
  // Injectable so tests (fake-indexeddb) and SSR can supply/withhold a factory; defaults to the browser's.
  readonly factory?: IDBFactory;
}

// Generic key/value store over a single IndexedDB object store. Values are keyed by string and persisted
// through the browser's structured-clone, so anything clonable survives a reload. All keys live in one
// store; namespacing is by database/store name, not by key prefix.
export class IndexedDbKeyValueStore<V> implements AsyncKeyValueStore<V> {
  private readonly factory: IDBFactory;
  private readonly databaseName: string;
  private readonly storeName: string;
  private connection: Promise<IDBDatabase> | null = null;

  constructor(options: IndexedDbKeyValueStoreOptions) {
    const factory = options.factory ?? (globalThis as { indexedDB?: IDBFactory }).indexedDB;
    if (!factory) {
      throw new Error('IndexedDB is not available in this environment.');
    }
    this.factory = factory;
    this.databaseName = options.databaseName;
    this.storeName = options.storeName;
  }

  async get(key: string): Promise<V | undefined> {
    const result = await this.request('readonly', (store) => store.get(key));
    return (result as V | undefined) ?? undefined;
  }

  async set(key: string, value: V): Promise<void> {
    await this.request('readwrite', (store) => store.put(value, key));
  }

  async delete(key: string): Promise<void> {
    await this.request('readwrite', (store) => store.delete(key));
  }

  async keys(): Promise<string[]> {
    const keys = await this.request('readonly', (store) => store.getAllKeys());
    return (keys as IDBValidKey[]).map((key) => String(key));
  }

  async clear(): Promise<void> {
    await this.request('readwrite', (store) => store.clear());
  }

  private open(): Promise<IDBDatabase> {
    return (this.connection ??= new Promise<IDBDatabase>((resolve, reject) => {
      const request = this.factory.open(this.databaseName, 1);
      request.onupgradeneeded = () => {
        const db = request.result;
        if (!db.objectStoreNames.contains(this.storeName)) {
          db.createObjectStore(this.storeName);
        }
      };
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error);
    }));
  }

  // Resolves on transaction completion (not merely request success) so a caller awaiting a write is
  // guaranteed the data is durable before continuing.
  private async request<R>(
    mode: IDBTransactionMode,
    body: (store: IDBObjectStore) => IDBRequest<R>,
  ): Promise<R> {
    const db = await this.open();
    return new Promise<R>((resolve, reject) => {
      const transaction = db.transaction(this.storeName, mode);
      const request = body(transaction.objectStore(this.storeName));
      transaction.oncomplete = () => resolve(request.result);
      transaction.onerror = () => reject(transaction.error ?? request.error);
      transaction.onabort = () => reject(transaction.error ?? request.error);
    });
  }
}
