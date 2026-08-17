import {
  LinkingSourceDescriptor,
  LinkingSourceTypeOption,
} from '../models/linking-source.models';

export function linkingSourceTypeCodes(source: LinkingSourceDescriptor): readonly string[] {
  switch (source.kind) {
    case 'unique-word':
    case 'root':
    case 'lemma':
    case 'stem':
      return source.typeCodes;
    default:
      return [];
  }
}

export function linkingSourceSupportsTypeFilters(source: LinkingSourceDescriptor): boolean {
  return ['unique-word', 'root', 'lemma', 'stem'].includes(source.kind);
}

export function withLinkingSourceTypeCodes(
  source: LinkingSourceDescriptor,
  typeCodes: readonly string[],
): LinkingSourceDescriptor {
  const normalized = normalizeLinkingSourceTypeCodes(typeCodes);
  switch (source.kind) {
    case 'unique-word':
    case 'root':
    case 'lemma':
    case 'stem':
      return { ...source, typeCodes: normalized };
    default:
      return source;
  }
}

export function normalizeSelectedSourceTypeCodes(
  typeCodes: readonly string[],
  availableTypes: readonly LinkingSourceTypeOption[],
): readonly string[] {
  const normalized = normalizeLinkingSourceTypeCodes(typeCodes);
  const availableCodes = new Set(availableTypes.map((type) => type.code));
  const selected = normalized.filter((code) => availableCodes.has(code));
  return selected.length === availableCodes.size ? [] : selected;
}

function normalizeLinkingSourceTypeCodes(typeCodes: readonly string[]): readonly string[] {
  return [...new Set(typeCodes.map((code) => code.trim()).filter(Boolean))].sort();
}
