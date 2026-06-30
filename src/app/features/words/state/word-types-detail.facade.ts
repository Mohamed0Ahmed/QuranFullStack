import { Injectable, Signal, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';

import {
  DEFAULT_WORD_TYPE_CASE,
  DEFAULT_WORD_TYPE_TENSE,
  DEFAULT_WORD_TYPE_VOICE,
  DEFAULT_WORD_TYPES_DETAIL_VIEW,
  WordTypeDetailView,
  WordTypeRowIdentity,
  WordTypesDetailState,
} from '../models/word-types.models';
import { parseWordTypesQueryParams } from './word-types-url-sync';

@Injectable({ providedIn: 'root' })
export class WordTypesDetailFacade {
  private routeSub?: Subscription;
  private readonly state = signal<WordTypesDetailState>({
    status: 'idle',
    selectedRow: null,
    view: DEFAULT_WORD_TYPES_DETAIL_VIEW,
    summary: null,
    ayahs: null,
    surahs: null,
    errorMessage: '',
  });

  readonly panelState: Signal<WordTypesDetailState> = this.state.asReadonly();

  bindToRoute(route: ActivatedRoute): void {
    this.unbindFromRoute();
    this.routeSub = route.queryParamMap.subscribe((params) => {
      const parsed = parseWordTypesQueryParams(params);
      const selectedRow = parsed.word === null ? null : toIdentity(parsed);
      this.state.update((current) => ({ ...current, selectedRow, view: parsed.view }));
    });
  }

  unbindFromRoute(): void {
    this.routeSub?.unsubscribe();
    this.routeSub = undefined;
  }

  setView(view: WordTypeDetailView): void {
    this.state.update((current) => ({ ...current, view }));
  }

  clearSelection(): void {
    this.state.update((current) => ({ ...current, selectedRow: null, summary: null, ayahs: null, surahs: null }));
  }
}

function toIdentity(parsed: { tashkeelWordId: number; contextCode: string; case?: string; tense?: string; voice?: string }): WordTypeRowIdentity {
  return {
    tashkeelWordId: parsed.tashkeelWordId,
    contextCode: parsed.contextCode,
    case: parsed.case === 'nominative' || parsed.case === 'accusative' || parsed.case === 'genitive' || parsed.case === 'null' ? parsed.case : DEFAULT_WORD_TYPE_CASE,
    tense: parsed.tense === 'past' || parsed.tense === 'present' || parsed.tense === 'imperative' ? parsed.tense : DEFAULT_WORD_TYPE_TENSE,
    voice: parsed.voice === 'active' || parsed.voice === 'passive' ? parsed.voice : DEFAULT_WORD_TYPE_VOICE,
  };
}
