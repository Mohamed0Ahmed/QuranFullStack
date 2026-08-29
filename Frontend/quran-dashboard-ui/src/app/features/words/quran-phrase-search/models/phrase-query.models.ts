import { PhraseResolutionCandidateDto } from '../../../../core/api/generated/models/phrase-resolution-candidate-dto';

import { PhraseLoadStatus, PhraseTextMode } from './phrase-repetitions.models';

export type PhraseResolutionStatus =
  | 'idle'
  | 'loading'
  | 'resolved'
  | 'ambiguous'
  | 'unresolved'
  | 'invalid'
  | 'error'
  | 'rate-limited'
  | 'stale'
  | 'unavailable';

export interface PhraseResolutionViewState {
  readonly rawQuery: string;
  readonly mode: PhraseTextMode;
  readonly status: PhraseResolutionStatus;
  readonly candidates: readonly PhraseResolutionCandidateDto[];
  readonly selectedResolutionRef: string | null;
  readonly message: string;
}

export interface PhraseCapabilitiesViewState {
  readonly status: PhraseLoadStatus;
  readonly message: string;
}

export const PHRASE_INDEX_CHANGED_MESSAGE = 'تغير فهرس البحث، أعد اختيار النتيجة';
export const PHRASE_INDEX_UNAVAILABLE_MESSAGE =
  'فهرس البحث غير متاح الآن. أعد المحاولة بعد اكتمال بنائه.';
export const PHRASE_LONG_STATE_NOTICE =
  'هذه الحالة الطويلة محفوظة لهذه الجلسة فقط. نسخ الرابط يعيد عبارة البحث الأساسية دون كل الاختيارات.';
export const PHRASE_LONG_STATE_OMITTED_QUERY_NOTICE =
  'هذه الحالة الطويلة محفوظة لهذه الجلسة فقط. عبارة البحث أطول من أن تُحفظ بأمان في الرابط، ونسخ الرابط يعيد الصفحة الأساسية دون العبارة أو الاختيارات.';
export const PHRASE_LONG_STATE_WITHOUT_QUERY_NOTICE =
  'هذه الحالة الطويلة محفوظة لهذه الجلسة فقط. نسخ الرابط يعيد الصفحة الأساسية دون الاختيارات.';
