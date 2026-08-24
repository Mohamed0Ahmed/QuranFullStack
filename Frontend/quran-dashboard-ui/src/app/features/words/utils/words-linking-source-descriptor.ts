import { LinkingSourceDescriptor } from '../../linking/models/linking-source.models';
import { LemmaListItemViewModel } from '../models/lemmas.models';
import { RootListItemViewModel } from '../models/roots.models';
import { StemListItemViewModel } from '../models/stems.models';
import { UniqueWordListItemViewModel } from '../models/unique-words.models';
import { WordTypeDetailScope } from '../models/word-types-detail.models';
import {
  WordTypeTableRowDto,
  normalizeWordTableRow,
} from '../models/word-types.models';

export function uniqueWordLinkingSource(row: UniqueWordListItemViewModel): LinkingSourceDescriptor {
  return { kind: 'unique-word', mode: row.kind, wordId: row.id, typeCodes: [], label: row.displayText };
}

export function rootLinkingSource(row: RootListItemViewModel): LinkingSourceDescriptor {
  return { kind: 'root', rootId: row.id, typeCodes: [], label: row.displayText };
}

export function lemmaLinkingSource(row: LemmaListItemViewModel): LinkingSourceDescriptor {
  return { kind: 'lemma', lemmaId: row.id, typeCodes: [], label: row.displayText };
}

export function stemLinkingSource(row: StemListItemViewModel): LinkingSourceDescriptor {
  return { kind: 'stem', stemId: row.id, typeCodes: [], label: row.displayText };
}

export function wordTypeLinkingSource(
  row: WordTypeTableRowDto,
  scope: WordTypeDetailScope,
): LinkingSourceDescriptor {
  const linkingScope = { ...scope };
  switch (row.kind) {
    case 'word': {
      const identity = normalizeWordTableRow(row);
      return {
        kind: 'word-type',
        selection: {
          kind: 'word',
          tashkeelWordId: identity.tashkeelWordId,
          contextCode: identity.contextCode,
          case: identity.case,
          tense: identity.tense,
          voice: identity.voice,
          scope: linkingScope,
        },
        label: row.displayText,
      };
    }
    case 'root':
      return { kind: 'word-type', selection: { kind: 'root', rootId: row.rootId, scope: linkingScope }, label: row.displayText };
    case 'stem':
      return { kind: 'word-type', selection: { kind: 'stem', stemId: row.stemId, scope: linkingScope }, label: row.displayText };
    case 'lemma':
      return { kind: 'word-type', selection: { kind: 'lemma', lemmaId: row.lemmaId, scope: linkingScope }, label: row.displayText };
  }
}

export function linkingSourcesByRow<Row>(
  rows: readonly Row[],
  descriptor: (row: Row) => LinkingSourceDescriptor,
): ReadonlyMap<Row, LinkingSourceDescriptor> {
  return new Map(rows.map((row) => [row, descriptor(row)] as const));
}
