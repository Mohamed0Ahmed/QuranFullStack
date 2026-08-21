import { Signal, WritableSignal, inject, signal } from '@angular/core';
import { Subject, Subscription, debounceTime } from 'rxjs';

import {
  WordsSearchDraftKey,
  WordsSearchDraftSessionStore,
} from './words-search-draft-session.store';

export class WordsDebouncedSearchDraftController {
  private readonly draftSignal: WritableSignal<string> = signal('');
  private readonly input = new Subject<string>();
  private subscription?: Subscription;
  private committedValue = '';
  private initialized = false;
  private readonly requestedCommits = new Set<string>();

  readonly draft: Signal<string> = this.draftSignal.asReadonly();

  static create(
    key: WordsSearchDraftKey,
    commit: (value: string) => void,
  ): WordsDebouncedSearchDraftController {
    return new WordsDebouncedSearchDraftController(
      key,
      inject(WordsSearchDraftSessionStore),
      commit,
    );
  }

  constructor(
    private readonly key: WordsSearchDraftKey,
    private readonly store: WordsSearchDraftSessionStore,
    private readonly commit: (value: string) => void,
  ) {}

  start(): void {
    this.subscription ??= this.input
      .pipe(debounceTime(300))
      .subscribe((value) => {
        this.requestedCommits.add(value);
        this.commit(value);
      });
  }

  syncCommitted(value: string): void {
    this.committedValue = value;
    const resolved = this.store.resolve(this.key, value, this.requestedCommits.delete(value));
    this.draftSignal.set(resolved);
    if (!this.initialized) {
      this.initialized = true;
      if (resolved !== value) {
        this.input.next(resolved);
      }
    }
  }

  update(value: string): void {
    this.draftSignal.set(value);
    this.store.stage(this.key, value, this.committedValue);
    if (value !== this.committedValue) {
      this.input.next(value);
    }
  }

  destroy(): void {
    this.subscription?.unsubscribe();
    this.subscription = undefined;
    this.requestedCommits.clear();
    this.store.flush();
  }
}
