import { Injectable, signal } from '@angular/core';

import {
  PHRASE_INDEX_CHANGED_MESSAGE,
  PHRASE_LONG_STATE_NOTICE,
  PHRASE_LONG_STATE_OMITTED_QUERY_NOTICE,
  PHRASE_LONG_STATE_WITHOUT_QUERY_NOTICE,
} from '../models/phrase-query.models';
import { PhraseNavigationOutcome } from './phrase-route-navigation.coordinator';

type PhraseNoticeKind =
  | 'none'
  | 'index-changed'
  | 'long-query-retained'
  | 'long-query-omitted'
  | 'long-without-query';

@Injectable()
export class PhraseNoticeStore {
  private readonly kind = signal<PhraseNoticeKind>('none');
  readonly message = signal('');
  readonly sessionOnly = signal(false);

  applyNavigation(outcome: PhraseNavigationOutcome): void {
    this.sessionOnly.set(outcome.sessionOnly);
    if (!outcome.sessionOnly) {
      if (this.kind().startsWith('long-')) {
        this.set('none', '');
      }
      return;
    }
    if (outcome.queryDisposition === 'retained') {
      this.set('long-query-retained', PHRASE_LONG_STATE_NOTICE);
    } else if (outcome.queryDisposition === 'omitted') {
      this.set('long-query-omitted', PHRASE_LONG_STATE_OMITTED_QUERY_NOTICE);
    } else {
      this.set('long-without-query', PHRASE_LONG_STATE_WITHOUT_QUERY_NOTICE);
    }
  }

  indexChanged(): void {
    this.sessionOnly.set(false);
    this.set('index-changed', PHRASE_INDEX_CHANGED_MESSAGE);
  }

  dismiss(): void {
    this.set('none', '');
  }

  private set(kind: PhraseNoticeKind, message: string): void {
    this.kind.set(kind);
    this.message.set(message);
  }
}
