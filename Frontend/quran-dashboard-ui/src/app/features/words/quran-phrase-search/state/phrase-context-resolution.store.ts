import { Injectable, signal } from '@angular/core';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';

import { PhraseContextBranchesResponse } from '../../../../core/api/generated/models/phrase-context-branches-response';
import { PhraseResolutionApi } from '../data-access/phrase-resolution.api';
import { PhraseResolutionViewState } from '../models/phrase-query.models';
import { PhraseTextMode } from '../models/phrase-repetitions.models';
import { encodePhraseQuery, phraseQueryByteLength } from './phrase-query-encoding';
import {
  MappedPhraseResolution,
  mapPhraseResolution,
  phraseResolutionFromBranches,
} from './phrase-resolution-state';

const INVALID_QUERY_MESSAGE = 'اكتب عبارة قرآنية ثم أرسلها.';

@Injectable()
export class PhraseContextResolutionStore {
  readonly mode = signal<PhraseTextMode>('simple');
  readonly state = signal<PhraseResolutionViewState>({
    rawQuery: '', mode: 'simple', status: 'idle', candidates: [], selectedResolutionRef: null, message: '',
  });

  constructor(private readonly api: PhraseResolutionApi) {}

  setDraft(rawQuery: string): void {
    this.state.update((current) => ({
      ...current,
      rawQuery,
      status: 'idle',
      candidates: [],
      selectedResolutionRef: null,
      message: '',
    }));
  }

  setMode(mode: PhraseTextMode): boolean {
    if (mode === this.mode()) {
      return false;
    }
    this.mode.set(mode);
    this.state.set({
      rawQuery: this.state().rawQuery,
      mode,
      status: 'idle',
      candidates: [],
      selectedResolutionRef: null,
      message: '',
    });
    return true;
  }

  resolve(): Observable<MappedPhraseResolution | null> {
    const query = this.state().rawQuery.trim();
    if (!query || phraseQueryByteLength(query) > 4096) {
      this.state.update((current) => ({
        ...current,
        status: 'invalid',
        candidates: [],
        selectedResolutionRef: null,
        message: INVALID_QUERY_MESSAGE,
      }));
      return of(null);
    }
    this.state.update((current) => ({ ...current, rawQuery: query, status: 'loading', message: '' }));
    return this.api
      .resolve(this.mode(), encodePhraseQuery(query))
      .pipe(map((response) => mapPhraseResolution(query, this.mode(), response)));
  }

  accept(mapped: MappedPhraseResolution): void {
    this.state.set(mapped.state);
  }

  fail(status: PhraseResolutionViewState['status'], message: string): void {
    this.state.update((current) => ({ ...current, status, message }));
  }

  restoreIdle(rawQuery: string, mode: PhraseTextMode): void {
    this.mode.set(mode);
    this.state.update((current) => ({
      ...current,
      rawQuery: rawQuery || current.rawQuery,
      mode,
      status: rawQuery ? 'idle' : current.status === 'invalid' ? 'invalid' : 'idle',
      selectedResolutionRef: null,
    }));
  }

  markLoading(rawQuery: string, resolutionRef: string): void {
    this.state.update((current) => ({
      ...current,
      rawQuery,
      selectedResolutionRef: resolutionRef,
      status: 'loading',
    }));
  }

  restoreFromBranches(rawQuery: string, response: PhraseContextBranchesResponse): void {
    const mode: PhraseTextMode = response.query.mode === 'tashkil' ? 'tashkil' : 'simple';
    this.mode.set(mode);
    this.state.set(phraseResolutionFromBranches(rawQuery, mode, response));
  }
}
