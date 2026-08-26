import { Injectable, signal } from '@angular/core';

import { PhraseResolutionCandidateDto } from '../../../../core/api/generated/models/phrase-resolution-candidate-dto';
import { PhraseResolutionStatus } from '../models/phrase-query.models';
import { MappedPhraseResolution } from './phrase-resolution-state';

@Injectable()
export class PhraseSimilarityResolutionStore {
  readonly draft = signal('');
  readonly status = signal<PhraseResolutionStatus>('idle');
  readonly candidates = signal<readonly PhraseResolutionCandidateDto[]>([]);

  setDraft(query: string): boolean {
    if (query === this.draft()) {
      return false;
    }
    this.draft.set(query);
    this.reset('idle');
    return true;
  }

  restoreDraft(query: string): void {
    if (query) {
      this.draft.set(query);
    }
  }

  start(): void {
    this.status.set('loading');
    this.candidates.set([]);
  }

  accept(mapped: MappedPhraseResolution): void {
    this.status.set(mapped.state.status);
    this.candidates.set(mapped.state.candidates);
  }

  select(candidate: PhraseResolutionCandidateDto): void {
    this.status.set('resolved');
    this.candidates.set([candidate]);
  }

  fail(status: PhraseResolutionStatus): void {
    this.status.set(status);
  }

  reset(status: PhraseResolutionStatus = 'idle'): void {
    this.status.set(status);
    this.candidates.set([]);
  }
}
