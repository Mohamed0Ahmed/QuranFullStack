import { Signal, computed, signal } from '@angular/core';

export type ActionStatus = 'idle' | 'pending' | 'success' | 'error' | 'conflict';

export interface ActionState<TResult> {
  readonly status: ActionStatus;
  readonly result: TResult | null;
  readonly error: unknown;
}

const IDLE_STATE: ActionState<never> = { status: 'idle', result: null, error: null };

// Recognizes the shared ConcurrencyConflictError by its stable `name` marker rather than importing the
// class, so this core primitive stays decoupled from the shared concurrency module.
function isConflict(error: unknown): boolean {
  return error instanceof Error && error.name === 'ConcurrencyConflictError';
}

// Generic Signals wrapper around a one-shot async operation. It exposes a status signal a template can bind
// to and separates a concurrency conflict from a generic failure, so a form/command can surface the two
// differently (e.g. reload-and-retry vs. show an error). Domain-free: the handler is caller-supplied.
export class AsyncAction<TInput, TResult> {
  private readonly stateSignal = signal<ActionState<TResult>>(IDLE_STATE);

  readonly state: Signal<ActionState<TResult>> = this.stateSignal.asReadonly();
  readonly status = computed(() => this.stateSignal().status);
  readonly isPending = computed(() => this.stateSignal().status === 'pending');

  constructor(private readonly handler: (input: TInput) => Promise<TResult>) {}

  async run(input: TInput): Promise<ActionState<TResult>> {
    this.stateSignal.set({ status: 'pending', result: null, error: null });
    try {
      const result = await this.handler(input);
      this.stateSignal.set({ status: 'success', result, error: null });
    } catch (error) {
      this.stateSignal.set({
        status: isConflict(error) ? 'conflict' : 'error',
        result: null,
        error,
      });
    }
    return this.stateSignal();
  }

  reset(): void {
    this.stateSignal.set(IDLE_STATE);
  }
}
