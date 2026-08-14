import { Injectable } from '@angular/core';
import { Observable, Subscription } from 'rxjs';

import {
  LINKING_POLL_FALLBACK_MS,
  LINKING_POLL_MAX_MS,
  LINKING_POLL_MIN_MS,
} from '../linking.policy';

interface PollRegistration<T> {
  generation: number;
  load: () => Observable<T>;
  isTerminal: (value: T) => boolean;
  pollAfterMs: (value: T) => number | null;
  next: (value: T) => void;
  error: (error: unknown) => void;
  subscription: Subscription | null;
  timer: ReturnType<typeof setTimeout> | null;
}

@Injectable({ providedIn: 'root' })
export class LinkingStatusPollRunner {
  private readonly registrations = new Map<string, PollRegistration<unknown>>();

  start<T>(
    key: string,
    generation: number,
    load: () => Observable<T>,
    isTerminal: (value: T) => boolean,
    pollAfterMs: (value: T) => number | null,
    next: (value: T) => void,
    error: (error: unknown) => void,
  ): void {
    this.cancel(key);
    const registration: PollRegistration<T> = {
      generation,
      load,
      isTerminal,
      pollAfterMs,
      next,
      error,
      subscription: null,
      timer: null,
    };
    this.registrations.set(key, registration as PollRegistration<unknown>);
    this.poll(key, registration);
  }

  cancel(key: string): void {
    const registration = this.registrations.get(key);
    if (registration === undefined) {
      return;
    }
    registration.subscription?.unsubscribe();
    if (registration.timer !== null) {
      clearTimeout(registration.timer);
    }
    this.registrations.delete(key);
  }

  cancelAll(): void {
    [...this.registrations.keys()].forEach((key) => this.cancel(key));
  }

  private poll<T>(key: string, registration: PollRegistration<T>): void {
    if (this.registrations.get(key) !== registration) {
      return;
    }
    registration.subscription = registration.load().subscribe({
      next: (value) => {
        if (this.registrations.get(key) !== registration) {
          return;
        }
        registration.next(value);
        if (this.registrations.get(key) !== registration) {
          return;
        }
        if (registration.isTerminal(value)) {
          this.cancel(key);
          return;
        }
        const requestedDelay = registration.pollAfterMs(value) ?? LINKING_POLL_FALLBACK_MS;
        const delay = Math.min(LINKING_POLL_MAX_MS, Math.max(LINKING_POLL_MIN_MS, requestedDelay));
        registration.timer = setTimeout(() => this.poll(key, registration), delay);
      },
      error: (error: unknown) => {
        if (this.registrations.get(key) === registration) {
          registration.error(error);
          this.cancel(key);
        }
      },
    });
  }
}
