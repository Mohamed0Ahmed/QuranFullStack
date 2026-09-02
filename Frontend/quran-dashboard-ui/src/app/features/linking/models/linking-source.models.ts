import {
  isLinkingManualMushafAyahSource,
  LinkingManualMushafAyahSource,
} from './linking-manual-mushaf.models';

export type LinkingSourceKind =
  | 'manual-mushaf-ayahs'
  | 'unique-word'
  | 'root'
  | 'lemma'
  | 'stem'
  | 'word-type';

export type LinkingUniqueWordMode = 'simple' | 'tashkeel';
export type LinkingWordTypeMainType = 'noun' | 'verb' | 'particle' | 'inl';
export type LinkingWordTypeCase = 'all' | 'nominative' | 'accusative' | 'genitive' | 'null';
export type LinkingWordTypeTense = 'all' | 'past' | 'present' | 'imperative';
export type LinkingWordTypeVoice = 'all' | 'active' | 'passive';

export interface LinkingSourceTypeOption {
  code: string;
  arabicLabel: string;
  occurrencesCount: number;
}

export interface LinkingWordTypeScope {
  type: LinkingWordTypeMainType;
  childCode: string | null;
  case: LinkingWordTypeCase;
  tense: LinkingWordTypeTense;
  voice: LinkingWordTypeVoice;
}

export type LinkingWordTypeSelection =
  | {
      kind: 'word';
      tashkeelWordId: number;
      contextCode: string;
      case: LinkingWordTypeCase;
      tense: LinkingWordTypeTense;
      voice: LinkingWordTypeVoice;
      scope: LinkingWordTypeScope;
    }
  | { kind: 'root'; rootId: number; scope: LinkingWordTypeScope }
  | { kind: 'stem'; stemId: number; scope: LinkingWordTypeScope }
  | { kind: 'lemma'; lemmaId: number; scope: LinkingWordTypeScope };

export type LinkingSourceDescriptor =
  | ({ kind: 'manual-mushaf-ayahs'; label: string } & LinkingManualMushafAyahSource)
  | {
      kind: 'unique-word';
      mode: LinkingUniqueWordMode;
      wordId: number;
      typeCodes: readonly string[];
      label: string;
    }
  | { kind: 'root'; rootId: number; typeCodes: readonly string[]; label: string }
  | { kind: 'lemma'; lemmaId: number; typeCodes: readonly string[]; label: string }
  | { kind: 'stem'; stemId: number; typeCodes: readonly string[]; label: string }
  | { kind: 'word-type'; selection: LinkingWordTypeSelection; label: string };

const wordTypeMainTypes: readonly LinkingWordTypeMainType[] = ['noun', 'verb', 'particle', 'inl'];
const wordTypeCases: readonly LinkingWordTypeCase[] = ['all', 'nominative', 'accusative', 'genitive', 'null'];
const wordTypeTenses: readonly LinkingWordTypeTense[] = ['all', 'past', 'present', 'imperative'];
const wordTypeVoices: readonly LinkingWordTypeVoice[] = ['all', 'active', 'passive'];

export function isLinkingSourceDescriptor(value: unknown): value is LinkingSourceDescriptor {
  if (!isRecord(value) || !isNonBlankString(value['label'])) {
    return false;
  }

  switch (value['kind']) {
    case 'manual-mushaf-ayahs':
      return isLinkingManualMushafAyahSource(value);
    case 'unique-word':
      return (
        (value['mode'] === 'simple' || value['mode'] === 'tashkeel') &&
        isPositiveSafeInteger(value['wordId']) &&
        isTypeCodes(value['typeCodes'])
      );
    case 'root':
      return isPositiveSafeInteger(value['rootId']) && isTypeCodes(value['typeCodes']);
    case 'lemma':
      return isPositiveSafeInteger(value['lemmaId']) && isTypeCodes(value['typeCodes']);
    case 'stem':
      return isPositiveSafeInteger(value['stemId']) && isTypeCodes(value['typeCodes']);
    case 'word-type':
      return isLinkingWordTypeSelection(value['selection']);
    default:
      return false;
  }
}

export function isLinkingWordTypeSelection(value: unknown): value is LinkingWordTypeSelection {
  if (!isRecord(value) || !isLinkingWordTypeScope(value['scope'])) {
    return false;
  }

  switch (value['kind']) {
    case 'word':
      return (
        isPositiveSafeInteger(value['tashkeelWordId']) &&
        isNonBlankString(value['contextCode']) &&
        isWordTypeCase(value['case']) &&
        isWordTypeTense(value['tense']) &&
        isWordTypeVoice(value['voice'])
      );
    case 'root':
      return isPositiveSafeInteger(value['rootId']);
    case 'stem':
      return isPositiveSafeInteger(value['stemId']);
    case 'lemma':
      return isPositiveSafeInteger(value['lemmaId']);
    default:
      return false;
  }
}

export function isLinkingWordTypeScope(value: unknown): value is LinkingWordTypeScope {
  return (
    isRecord(value) &&
    isWordTypeMainType(value['type']) &&
    (value['childCode'] === null || isNonBlankString(value['childCode'])) &&
    isWordTypeCase(value['case']) &&
    isWordTypeTense(value['tense']) &&
    isWordTypeVoice(value['voice'])
  );
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function isPositiveSafeInteger(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value > 0;
}

function isNonBlankString(value: unknown): value is string {
  return typeof value === 'string' && value.trim().length > 0;
}

function isTypeCodes(value: unknown): value is readonly string[] {
  return Array.isArray(value) && value.every(isNonBlankString);
}

function isWordTypeMainType(value: unknown): value is LinkingWordTypeMainType {
  return wordTypeMainTypes.includes(value as LinkingWordTypeMainType);
}

function isWordTypeCase(value: unknown): value is LinkingWordTypeCase {
  return wordTypeCases.includes(value as LinkingWordTypeCase);
}

function isWordTypeTense(value: unknown): value is LinkingWordTypeTense {
  return wordTypeTenses.includes(value as LinkingWordTypeTense);
}

function isWordTypeVoice(value: unknown): value is LinkingWordTypeVoice {
  return wordTypeVoices.includes(value as LinkingWordTypeVoice);
}
