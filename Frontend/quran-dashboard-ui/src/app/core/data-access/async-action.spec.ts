import { describe, expect, it } from 'vitest';

import { ConcurrencyConflictError } from '../../shared/concurrency/optimistic-concurrency';
import { AsyncAction } from './async-action';

describe('AsyncAction', () => {
  it('starts idle', () => {
    const action = new AsyncAction<void, number>(async () => 1);

    expect(action.state().status).toBe('idle');
    expect(action.state().result).toBeNull();
    expect(action.isPending()).toBe(false);
  });

  it('runs to success and exposes the result', async () => {
    const action = new AsyncAction<number, number>(async (n) => n * 2);

    const state = await action.run(3);

    expect(state.status).toBe('success');
    expect(state.result).toBe(6);
    expect(action.state().result).toBe(6);
    expect(action.state().error).toBeNull();
  });

  it('is pending while the handler is in flight', async () => {
    let release!: () => void;
    const gate = new Promise<void>((resolve) => {
      release = resolve;
    });
    const action = new AsyncAction<void, string>(async () => {
      await gate;
      return 'done';
    });

    const running = action.run();
    expect(action.isPending()).toBe(true);
    expect(action.state().status).toBe('pending');

    release();
    await running;
    expect(action.isPending()).toBe(false);
  });

  it('captures a thrown error as the error status', async () => {
    const failure = new Error('boom');
    const action = new AsyncAction<void, void>(async () => {
      throw failure;
    });

    const state = await action.run();

    expect(state.status).toBe('error');
    expect(state.error).toBe(failure);
    expect(state.result).toBeNull();
  });

  it('classifies a ConcurrencyConflictError as a conflict, not a generic error', async () => {
    const action = new AsyncAction<void, void>(async () => {
      throw new ConcurrencyConflictError(1, 2);
    });

    const state = await action.run();

    expect(state.status).toBe('conflict');
    expect(state.error).toBeInstanceOf(ConcurrencyConflictError);
  });

  it('reset returns to idle', async () => {
    const action = new AsyncAction<void, number>(async () => 1);
    await action.run();

    action.reset();

    expect(action.state().status).toBe('idle');
    expect(action.state().result).toBeNull();
  });
});
