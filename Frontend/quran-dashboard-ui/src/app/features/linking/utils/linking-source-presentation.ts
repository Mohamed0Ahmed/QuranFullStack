import { WORDS_SHARED_HEADERS } from '../../words/models/words-shared.labels';
import {
  WORD_TYPE_CASE_LABELS,
  WORD_TYPE_CHILD_LABELS,
  WORD_TYPE_MAIN_LABELS,
  WORD_TYPE_TENSE_LABELS,
  WORD_TYPE_VOICE_LABELS,
} from '../../words/models/word-types.labels';
import { LinkingSourceDescriptor, LinkingWordTypeScope } from '../models/linking-source.models';

export function linkingSourcePresentation(source: LinkingSourceDescriptor): string {
  switch (source.kind) {
    case 'manual-mushaf-ayahs': {
      const verseCount = source.verseKeys.length;
      return verseCount === 1 ? 'آية من المصحف' : `${verseCount} آيات من المصحف`;
    }
    case 'unique-word':
      return source.mode === 'simple' ? 'كلمات فريدة بدون تشكيل' : 'كلمات فريدة بالتشكيل';
    case 'root':
      return WORDS_SHARED_HEADERS.root;
    case 'lemma':
      return WORDS_SHARED_HEADERS.lemma;
    case 'stem':
      return WORDS_SHARED_HEADERS.stem;
    case 'word-type':
      return `نوع الكلمة: ${wordTypeScopePresentation(source.selection.scope)}`;
  }
}

function wordTypeScopePresentation(scope: LinkingWordTypeScope): string {
  const childLabel = scope.childCode === null
    ? null
    : WORD_TYPE_CHILD_LABELS[scope.childCode] ?? scope.childCode;

  return [
    WORD_TYPE_MAIN_LABELS[scope.type],
    childLabel,
    scope.case === 'all' ? null : WORD_TYPE_CASE_LABELS[scope.case],
    scope.tense === 'all' ? null : WORD_TYPE_TENSE_LABELS[scope.tense],
    scope.voice === 'all' ? null : WORD_TYPE_VOICE_LABELS[scope.voice],
  ]
    .filter((value): value is string => value !== null)
    .join(' · ');
}
