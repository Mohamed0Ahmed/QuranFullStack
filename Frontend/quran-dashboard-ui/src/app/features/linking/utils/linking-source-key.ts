import { LinkingSourceDescriptor, LinkingWordTypeScope } from '../models/linking-source.models';
import { manualMushafVerseKeys } from './manual-link-shape';

export function linkingSourceKey(source: LinkingSourceDescriptor): string {
  switch (source.kind) {
    case 'manual-mushaf-ayahs':
      return joinKey(source.kind, ...manualMushafVerseKeys(source));
    case 'unique-word':
      return joinKey(source.kind, source.mode, source.wordId, ...source.typeCodes);
    case 'root':
      return joinKey(source.kind, source.rootId, ...source.typeCodes);
    case 'lemma':
      return dimensionKey(source.kind, source.lemmaId, source.typeCodes);
    case 'stem':
      return dimensionKey(source.kind, source.stemId, source.typeCodes);
    case 'word-type':
      return linkingWordTypeKey(source);
  }
}

function dimensionKey(kind: 'lemma' | 'stem', id: number, typeCodes: readonly string[]): string {
  return typeCodes.length === 0
    ? joinKey(kind, id, null)
    : joinKey(kind, id, ...typeCodes);
}

function linkingWordTypeKey(source: Extract<LinkingSourceDescriptor, { kind: 'word-type' }>): string {
  const { selection } = source;
  const scope = scopeKeyParts(selection.scope);

  switch (selection.kind) {
    case 'word':
      return joinKey(
        source.kind,
        selection.kind,
        selection.tashkeelWordId,
        selection.contextCode,
        selection.case,
        selection.tense,
        selection.voice,
        ...scope,
      );
    case 'root':
      return joinKey(source.kind, selection.kind, selection.rootId, ...scope);
    case 'stem':
      return joinKey(source.kind, selection.kind, selection.stemId, ...scope);
    case 'lemma':
      return joinKey(source.kind, selection.kind, selection.lemmaId, ...scope);
  }
}

function scopeKeyParts(scope: LinkingWordTypeScope): readonly (string | number | null)[] {
  return [scope.type, scope.childCode, scope.case, scope.tense, scope.voice];
}

function joinKey(...parts: readonly (string | number | null)[]): string {
  return parts.map((part) => encodeURIComponent(String(part ?? ''))).join('|');
}
