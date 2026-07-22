import { IDBFactory } from 'fake-indexeddb';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { IndexedDbKeyValueStore } from './indexed-db-key-value-store';

describe('IndexedDbKeyValueStore', () => {
  let factory: IDBFactory;

  beforeEach(() => {
    factory = new IDBFactory();
  });

  afterEach(() => {
    factory = new IDBFactory();
  });

  function newStore<V>() {
    return new IndexedDbKeyValueStore<V>({ databaseName: 'qd-test', storeName: 'entries', factory });
  }

  it('round-trips a structured value', async () => {
    const store = newStore<{ label: string; count: number }>();

    await store.set('k', { label: 'x', count: 3 });

    expect(await store.get('k')).toEqual({ label: 'x', count: 3 });
  });

  it('returns undefined for a missing key', async () => {
    expect(await newStore<number>().get('nope')).toBeUndefined();
  });

  it('lists keys and deletes them', async () => {
    const store = newStore<number>();

    await store.set('a', 1);
    await store.set('b', 2);
    expect((await store.keys()).sort()).toEqual(['a', 'b']);

    await store.delete('a');
    expect(await store.keys()).toEqual(['b']);
  });

  it('clears every entry', async () => {
    const store = newStore<number>();
    await store.set('a', 1);
    await store.set('b', 2);

    await store.clear();

    expect(await store.keys()).toEqual([]);
  });

  it('persists across a reopen of the same database', async () => {
    await newStore<string>().set('k', 'kept');

    expect(await newStore<string>().get('k')).toBe('kept');
  });

  it('stores a structured-clone snapshot, not a live reference', async () => {
    const store = newStore<{ n: number }>();
    const source = { n: 1 };

    await store.set('k', source);
    source.n = 999;

    expect(await store.get('k')).toEqual({ n: 1 });
  });
});
