import { LinkingSourceDescriptor, LinkingWordTypeScope } from '../models/linking-source.models';

export function linkingSourceKey(source: LinkingSourceDescriptor): string {
  switch (source.kind) {
    case 'mushaf-word':
      return joinKey(
        source.kind,
        source.quranWordId,
        source.wordLocation,
        source.verseKey,
        source.pageNumber,
      );
    case 'unique-word':
      return joinKey(source.kind, source.mode, source.wordId);
    case 'root':
      return joinKey(source.kind, source.rootId);
    case 'lemma':
      return joinKey(source.kind, source.lemmaId, source.typeCode);
    case 'stem':
      return joinKey(source.kind, source.stemId, source.typeCode);
    case 'word-type':
      return linkingWordTypeKey(source);
  }
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
