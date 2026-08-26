import { Injectable, signal } from '@angular/core';

import { PhraseLoadStatus } from '../models/phrase-repetitions.models';

export type PhraseContextRequestTarget =
  | 'capabilities'
  | 'workspace'
  | 'branches'
  | 'groups'
  | 'occurrences';

@Injectable()
export class PhraseContextRequestStatusStore {
  readonly capabilities = signal<PhraseLoadStatus>('idle');
  readonly branches = signal<PhraseLoadStatus>('idle');
  readonly groups = signal<PhraseLoadStatus>('idle');
  readonly occurrences = signal<PhraseLoadStatus>('idle');
  readonly errorMessage = signal('');

  set(target: PhraseContextRequestTarget, status: PhraseLoadStatus): void {
    if (target === 'capabilities') {
      this.capabilities.set(status);
      return;
    }
    if (target === 'groups') {
      this.groups.set(status);
      return;
    }
    if (target === 'occurrences') {
      this.occurrences.set(status);
      return;
    }
    this.branches.set(status);
    if (target === 'workspace') {
      this.groups.set(status);
    }
  }

  fail(
    target: PhraseContextRequestTarget,
    status: Extract<PhraseLoadStatus, 'invalid' | 'error' | 'stale' | 'unavailable'>,
    message: string,
  ): void {
    this.set(target, status);
    this.errorMessage.set(message);
  }
}
