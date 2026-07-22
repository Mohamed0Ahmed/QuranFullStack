import { describe, expect, it } from 'vitest';

import { SignalStore } from './signal-store';

class CounterStore extends SignalStore<number> {
  begin(): void {
    this.setLoading();
  }
  succeed(value: number): void {
    this.setValue(value);
  }
  fail(message: string): void {
    this.setError(message);
  }
}

describe('SignalStore', () => {
  it('starts idle with no value or error', () => {
    const store = new CounterStore();

    expect(store.status()).toBe('idle');
    expect(store.value()).toBeNull();
    expect(store.error()).toBeNull();
    expect(store.isLoading()).toBe(false);
  });

  it('moves through loading into a ready value', () => {
    const store = new CounterStore();

    store.begin();
    expect(store.status()).toBe('loading');
    expect(store.isLoading()).toBe(true);

    store.succeed(5);
    expect(store.status()).toBe('ready');
    expect(store.value()).toBe(5);
    expect(store.error()).toBeNull();
    expect(store.isLoading()).toBe(false);
  });

  it('records an error and clears the value', () => {
    const store = new CounterStore();
    store.succeed(9);

    store.fail('تعذر التحميل');

    expect(store.status()).toBe('error');
    expect(store.value()).toBeNull();
    expect(store.error()).toBe('تعذر التحميل');
  });

  it('reset returns to the idle baseline', () => {
    const store = new CounterStore();
    store.succeed(3);

    store.reset();

    expect(store.status()).toBe('idle');
    expect(store.value()).toBeNull();
    expect(store.error()).toBeNull();
  });
});
