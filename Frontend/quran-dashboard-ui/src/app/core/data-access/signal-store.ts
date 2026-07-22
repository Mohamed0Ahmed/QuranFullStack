import { computed, signal } from '@angular/core';

export type ResourceStatus = 'idle' | 'loading' | 'ready' | 'error';

export class SignalStore<T> {
  private readonly valueSignal = signal<T | null>(null);
  private readonly statusSignal = signal<ResourceStatus>('idle');
  private readonly errorSignal = signal<string | null>(null);

  readonly value = this.valueSignal.asReadonly();
  readonly status = this.statusSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly isLoading = computed(() => this.statusSignal() === 'loading');

  protected setLoading(): void {
    this.statusSignal.set('loading');
    this.errorSignal.set(null);
  }

  protected setValue(value: T): void {
    this.valueSignal.set(value);
    this.statusSignal.set('ready');
    this.errorSignal.set(null);
  }

  protected setError(message: string): void {
    this.valueSignal.set(null);
    this.statusSignal.set('error');
    this.errorSignal.set(message);
  }

  reset(): void {
    this.valueSignal.set(null);
    this.statusSignal.set('idle');
    this.errorSignal.set(null);
  }
}
