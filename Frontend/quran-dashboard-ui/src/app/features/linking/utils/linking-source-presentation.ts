import { WORDS_SHARED_HEADERS } from '../../words/models/words-shared.labels';
import { LinkingSourceDescriptor, LinkingWordTypeScope } from '../models/linking-source.models';
import { manualMushafVerseKeys } from './manual-link-shape';

export function linkingSourcePresentation(source: LinkingSourceDescriptor): string {
  switch (source.kind) {
    case 'manual-mushaf-ayahs': {
      const verseCount = manualMushafVerseKeys(source).length;
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
  return [scope.type, scope.childCode, scope.case, scope.tense, scope.voice]
    .filter((value): value is string => value !== null && value !== 'all')
    .join(' · ');
}
