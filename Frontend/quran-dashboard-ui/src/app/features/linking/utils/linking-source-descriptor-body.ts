import { LinkingSourceDescriptorBody } from '../../../core/api/generated/models/linking-source-descriptor-body';
import { LinkingWordTypeSelectionBody } from '../../../core/api/generated/models/linking-word-type-selection-body';
import { LinkingManualMushafAyahReference } from '../models/linking-manual-mushaf.models';
import {
  isLinkingSourceDescriptor,
  LinkingSourceDescriptor,
  LinkingWordTypeSelection,
} from '../models/linking-source.models';
import { manualMushafVerseKeys } from './manual-link-shape';

export function toLinkingSourceDescriptorBody(
  source: LinkingSourceDescriptor,
): LinkingSourceDescriptorBody {
  const body: LinkingSourceDescriptorBody = {
    contextKey: null,
    kind: source.kind,
    label: source.label,
    lemmaId: null,
    manualAyahs: null,
    mode: null,
    rootId: null,
    selection: null,
    stemId: null,
    typeCode: null,
    typeCodes: null,
    wordId: null,
  };

  switch (source.kind) {
    case 'manual-mushaf-ayahs':
      return {
        ...body,
        manualAyahs: manualMushafVerseKeys(source).map((verseKey) => ({ verseKey })),
      };
    case 'unique-word':
      return { ...body, mode: source.mode, wordId: source.wordId, typeCodes: [...source.typeCodes] };
    case 'root':
      return { ...body, rootId: source.rootId, typeCodes: [...source.typeCodes] };
    case 'lemma':
      return { ...body, lemmaId: source.lemmaId, typeCodes: [...source.typeCodes] };
    case 'stem':
      return { ...body, stemId: source.stemId, typeCodes: [...source.typeCodes] };
    case 'word-type':
      return { ...body, selection: toSelectionBody(source.selection) };
  }
}

export function fromLinkingSourceDescriptorBody(
  body: LinkingSourceDescriptorBody,
  manualAyahs: readonly LinkingManualMushafAyahReference[],
): LinkingSourceDescriptor | null {
  const candidate = toDescriptorCandidate(body, manualAyahs);
  return isLinkingSourceDescriptor(candidate) ? candidate : null;
}

function toDescriptorCandidate(
  body: LinkingSourceDescriptorBody,
  manualAyahs: readonly LinkingManualMushafAyahReference[],
): unknown {
  const label = body.label;
  switch (body.kind) {
    case 'manual-mushaf-ayahs':
      return { kind: body.kind, label, manualAyahs };
    case 'unique-word':
      return {
        kind: body.kind,
        label,
        mode: body.mode,
        wordId: body.wordId,
        typeCodes: readTypeCodes(body),
      };
    case 'root':
      return { kind: body.kind, label, rootId: body.rootId, typeCodes: readTypeCodes(body) };
    case 'lemma':
      return { kind: body.kind, label, lemmaId: body.lemmaId, typeCodes: readTypeCodes(body) };
    case 'stem':
      return { kind: body.kind, label, stemId: body.stemId, typeCodes: readTypeCodes(body) };
    case 'word-type':
      return { kind: body.kind, label, selection: toSelectionCandidate(body.selection) };
    default:
      return null;
  }
}

function readTypeCodes(body: LinkingSourceDescriptorBody): readonly string[] {
  if (body.typeCodes !== null) {
    return body.typeCodes;
  }
  return typeof body.typeCode === 'string' ? [body.typeCode] : [];
}

function toSelectionCandidate(selection: LinkingWordTypeSelectionBody | null): unknown {
  if (selection === null || selection.scope === null) {
    return null;
  }
  const scope = {
    type: selection.scope.type,
    childCode: selection.scope.childCode,
    case: selection.scope.case,
    tense: selection.scope.tense,
    voice: selection.scope.voice,
  };
  switch (selection.kind) {
    case 'word':
      return {
        kind: selection.kind,
        tashkeelWordId: selection.tashkeelWordId,
        contextCode: selection.contextCode,
        case: selection.case,
        tense: selection.tense,
        voice: selection.voice,
        scope,
      };
    case 'root':
      return { kind: selection.kind, rootId: selection.rootId, scope };
    case 'stem':
      return { kind: selection.kind, stemId: selection.stemId, scope };
    case 'lemma':
      return { kind: selection.kind, lemmaId: selection.lemmaId, scope };
    default:
      return null;
  }
}

function toSelectionBody(selection: LinkingWordTypeSelection): LinkingWordTypeSelectionBody {
  const body: LinkingWordTypeSelectionBody = {
    case: null,
    contextCode: null,
    kind: selection.kind,
    lemmaId: null,
    rootId: null,
    scope: {
      case: selection.scope.case,
      childCode: selection.scope.childCode,
      tense: selection.scope.tense,
      type: selection.scope.type,
      voice: selection.scope.voice,
    },
    stemId: null,
    tashkeelWordId: null,
    tense: null,
    voice: null,
  };

  switch (selection.kind) {
    case 'word':
      return {
        ...body,
        case: selection.case,
        contextCode: selection.contextCode,
        tashkeelWordId: selection.tashkeelWordId,
        tense: selection.tense,
        voice: selection.voice,
      };
    case 'root':
      return { ...body, rootId: selection.rootId };
    case 'stem':
      return { ...body, stemId: selection.stemId };
    case 'lemma':
      return { ...body, lemmaId: selection.lemmaId };
  }
}
