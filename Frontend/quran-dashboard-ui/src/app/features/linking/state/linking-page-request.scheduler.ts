import { Injectable } from '@angular/core';
import { Observable, ReplaySubject, Subscription } from 'rxjs';

import { LINKING_MAX_PAGE_REQUESTS } from '../linking.policy';

interface ScheduledPageRequest<T> {
  identity: string;
  scope: string;
  generation: number;
  load: () => Observable<T>;
  subject: ReplaySubject<T>;
  subscription: Subscription | null;
  started: boolean;
}

@Injectable({ providedIn: 'root' })
export class LinkingPageRequestScheduler {
  private readonly requests = new Map<string, ScheduledPageRequest<unknown>>();
  private readonly queue: ScheduledPageRequest<unknown>[] = [];
  private activeCount = 0;

  schedule<T>(
    scope: string,
    key: string,
    generation: number,
    load: () => Observable<T>,
  ): Observable<T> {
    const identity = `${scope}:${generation}:${key}`;
    const existing = this.requests.get(identity) as ScheduledPageRequest<T> | undefined;
    if (existing !== undefined) {
      return existing.subject.asObservable();
    }
    const request: ScheduledPageRequest<T> = {
      identity,
      scope,
      generation,
      load,
      subject: new ReplaySubject<T>(1),
      subscription: null,
      started: false,
    };
    this.requests.set(identity, request as ScheduledPageRequest<unknown>);
    this.queue.push(request as ScheduledPageRequest<unknown>);
    this.drain();
    return request.subject.asObservable();
  }

  cancelOlder(scope: string, generation: number): void {
    for (const request of [...this.requests.values()]) {
      if (request.scope === scope && request.generation < generation) {
        this.cancel(request);
      }
    }
    this.drain();
  }

  cancelScope(scope: string): void {
    for (const request of [...this.requests.values()]) {
      if (request.scope === scope) {
        this.cancel(request);
      }
    }
    this.drain();
  }

  private drain(): void {
    while (this.activeCount < LINKING_MAX_PAGE_REQUESTS) {
      const request = this.queue.shift();
      if (request === undefined) {
        return;
      }
      if (!this.requests.has(request.identity)) {
        continue;
      }
      this.start(request);
    }
  }

  private start(request: ScheduledPageRequest<unknown>): void {
    request.started = true;
    this.activeCount += 1;
    try {
      request.subscription = request.load().subscribe({
        next: (value) => request.subject.next(value),
        error: (error: unknown) => {
          request.subject.error(error);
          this.finish(request);
        },
        complete: () => {
          request.subject.complete();
          this.finish(request);
        },
      });
    } catch (error: unknown) {
      request.subject.error(error);
      this.finish(request);
    }
  }

  private cancel(request: ScheduledPageRequest<unknown>): void {
    request.subscription?.unsubscribe();
    request.subject.complete();
    this.requests.delete(request.identity);
    if (request.started) {
      request.started = false;
      this.activeCount -= 1;
    }
  }

  private finish(request: ScheduledPageRequest<unknown>): void {
    if (!this.requests.delete(request.identity)) {
      return;
    }
    if (request.started) {
      request.started = false;
      this.activeCount -= 1;
    }
    this.drain();
  }
}
