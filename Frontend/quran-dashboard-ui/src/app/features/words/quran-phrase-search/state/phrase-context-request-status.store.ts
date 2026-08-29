import { Injectable, signal } from '@angular/core';

import { PhraseLoadStatus } from '../models/phrase-repetitions.models';

export type PhraseContextRequestTarget =
  | 'capabilities'
  | 'workspace'
  | 'branches'
  | 'results';

@Injectable()
export class PhraseContextRequestStatusStore {
  readonly capabilities = signal<PhraseLoadStatus>('idle');
  readonly branches = signal<PhraseLoadStatus>('idle');
  readonly results = signal<PhraseLoadStatus>('idle');
  readonly errorMessage = signal('');

  set(target: PhraseContextRequestTarget, status: PhraseLoadStatus): void {
    if (target === 'capabilities') {
      this.capabilities.set(status);
      return;
    }
    if (target === 'results') {
      this.results.set(status);
      return;
    }
    this.branches.set(status);
    if (target === 'workspace') {
      this.results.set(status);
    }
  }

  fail(
    target: PhraseContextRequestTarget,
    status: Extract<
      PhraseLoadStatus,
      'invalid' | 'error' | 'rate-limited' | 'stale' | 'unavailable'
    >,
    message: string,
  ): void {
    this.set(target, status);
    this.errorMessage.set(message);
  }
}
