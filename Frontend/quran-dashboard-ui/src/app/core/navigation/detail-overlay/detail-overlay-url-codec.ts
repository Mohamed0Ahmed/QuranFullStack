import {
  CLOSED_DETAIL_OVERLAY_STATE,
  DETAIL_OVERLAY_FRAME_VERSION,
  DETAIL_OVERLAY_MAX_FRAMES,
  DetailFrame,
  DetailOverlayUrlState,
  FrameSurahView,
  FrameWordView,
  LemmaFrameView,
  RootFrameView,
  StemFrameView,
  UniqueFrameMode,
  UniqueFrameView,
  WordTypeFrameCase,
  WordTypeFrameTense,
  WordTypeFrameVoice,
  WordTypeFrameView,
} from './detail-overlay.models';

/**
 * Pure parse/serialize/canonicalize logic for the detail-overlay URL contract
 * (Feature 029, Change B). Strict on input (fail closed), explicit on output
 * (defaults are always serialized so future default changes cannot re-interpret
 * an old shared URL).
 */

const FIELD_SEPARATOR = '~';
const NULL_SENTINEL = '-';

const UNIQUE_MODES: readonly UniqueFrameMode[] = ['simple', 'tashkeel'];
const UNIQUE_VIEWS: readonly UniqueFrameView[] = ['surahs', 'missing', 'ayahs'];
const ROOT_VIEWS: readonly RootFrameView[] = ['words', 'ayahs', 'surahs', 'lemmas', 'stems'];
const LEMMA_VIEWS: readonly LemmaFrameView[] = ['words', 'ayahs', 'surahs', 'stems'];
const STEM_VIEWS: readonly StemFrameView[] = ['words', 'ayahs', 'surahs', 'lemmas'];
const WORD_VIEWS: readonly FrameWordView[] = ['simple', 'tashkeel'];
const SURAH_VIEWS: readonly FrameSurahView[] = ['mentioned', 'missing'];
const WORD_TYPE_CASES: readonly WordTypeFrameCase[] = ['all', 'nominative', 'accusative', 'genitive', 'null'];
const WORD_TYPE_TENSES: readonly WordTypeFrameTense[] = ['all', 'past', 'present', 'imperative'];
const WORD_TYPE_VOICES: readonly WordTypeFrameVoice[] = ['all', 'active', 'passive'];
const WORD_TYPE_VIEWS: readonly WordTypeFrameView[] = ['words', 'ayahs', 'surahs'];

/**
 * Encodes one string field. `~` is unreserved for `encodeURIComponent`, so it is
 * escaped explicitly to keep the separator unambiguous; a value that is exactly
 * the `-` sentinel is escaped so it cannot be misread as null.
 */
function encodeField(value: string): string {
  const encoded = encodeURIComponent(value).replace(/~/g, '%7E');
  return encoded === NULL_SENTINEL ? '%2D' : encoded;
}

function decodeField(raw: string): string | null {
  if (raw.length === 0 || raw.includes(FIELD_SEPARATOR)) {
    return null;
  }
  try {
    return decodeURIComponent(raw);
  } catch {
    return null;
  }
}

/**
 * Decimal syntax alone is not enough: a digit run above `Number.MAX_SAFE_INTEGER`
 * would round to a different integer (or to `Infinity`), so a shared URL could
 * silently resolve to another entity. The parsed value must survive the round trip
 * exactly, hence the safe-integer guard.
 */
function parsePositiveInt(raw: string): number | null {
  if (!/^[1-9]\d*$/.test(raw)) {
    return null;
  }
  const parsed = Number(raw);
  return Number.isSafeInteger(parsed) && parsed > 0 ? parsed : null;
}

function parseEnum<T extends string>(raw: string, allowed: readonly T[]): T | null {
  return (allowed as readonly string[]).includes(raw) ? (raw as T) : null;
}

/** `-` means null; anything else must decode to a non-empty string. */
function parseNullableCode(raw: string): { value: string | null } | null {
  if (raw === NULL_SENTINEL) {
    return { value: null };
  }
  const decoded = decodeField(raw);
  return decoded === null || decoded.length === 0 ? null : { value: decoded };
}

export function serializeDetailFrame(frame: DetailFrame): string {
  const v = DETAIL_OVERLAY_FRAME_VERSION;
  switch (frame.kind) {
    case 'unique':
      return [v, 'unique', frame.mode, String(frame.id), frame.view, String(frame.ayahPage)].join(FIELD_SEPARATOR);
    case 'root':
      return [
        v,
        'root',
        String(frame.id),
        frame.view,
        frame.wordView,
        frame.surahView,
        String(frame.detailPage),
      ].join(FIELD_SEPARATOR);
    case 'lemma':
    case 'stem':
      return [
        v,
        frame.kind,
        String(frame.id),
        frame.view,
        frame.wordView,
        frame.surahView,
        String(frame.detailPage),
        frame.typeCode === null ? NULL_SENTINEL : encodeField(frame.typeCode),
      ].join(FIELD_SEPARATOR);
    case 'wordType':
      return [
        v,
        'wordType',
        String(frame.tashkeelWordId),
        encodeField(frame.contextCode),
        frame.case,
        frame.tense,
        frame.voice,
        frame.view,
        String(frame.detailPage),
      ].join(FIELD_SEPARATOR);
  }
}

/** Strict single-frame parse; any malformed field fails the whole frame. */
export function parseDetailFrame(raw: string): DetailFrame | null {
  const fields = raw.split(FIELD_SEPARATOR);
  if (fields[0] !== DETAIL_OVERLAY_FRAME_VERSION) {
    return null;
  }

  switch (fields[1]) {
    case 'unique': {
      if (fields.length !== 6) {
        return null;
      }
      const mode = parseEnum(fields[2], UNIQUE_MODES);
      const id = parsePositiveInt(fields[3]);
      const view = parseEnum(fields[4], UNIQUE_VIEWS);
      const ayahPage = parsePositiveInt(fields[5]);
      if (mode === null || id === null || view === null || ayahPage === null) {
        return null;
      }
      return { kind: 'unique', mode, id, view, ayahPage };
    }
    case 'root': {
      if (fields.length !== 7) {
        return null;
      }
      const id = parsePositiveInt(fields[2]);
      const view = parseEnum(fields[3], ROOT_VIEWS);
      const wordView = parseEnum(fields[4], WORD_VIEWS);
      const surahView = parseEnum(fields[5], SURAH_VIEWS);
      const detailPage = parsePositiveInt(fields[6]);
      if (id === null || view === null || wordView === null || surahView === null || detailPage === null) {
        return null;
      }
      return { kind: 'root', id, view, wordView, surahView, detailPage };
    }
    case 'lemma':
    case 'stem': {
      if (fields.length !== 8) {
        return null;
      }
      const kind = fields[1];
      const id = parsePositiveInt(fields[2]);
      const view = kind === 'lemma' ? parseEnum(fields[3], LEMMA_VIEWS) : parseEnum(fields[3], STEM_VIEWS);
      const wordView = parseEnum(fields[4], WORD_VIEWS);
      const surahView = parseEnum(fields[5], SURAH_VIEWS);
      const detailPage = parsePositiveInt(fields[6]);
      const typeCode = parseNullableCode(fields[7]);
      if (id === null || view === null || wordView === null || surahView === null || detailPage === null || typeCode === null) {
        return null;
      }
      return kind === 'lemma'
        ? { kind: 'lemma', id, view: view as LemmaFrameView, wordView, surahView, detailPage, typeCode: typeCode.value }
        : { kind: 'stem', id, view: view as StemFrameView, wordView, surahView, detailPage, typeCode: typeCode.value };
    }
    case 'wordType': {
      if (fields.length !== 9) {
        return null;
      }
      const tashkeelWordId = parsePositiveInt(fields[2]);
      const contextCodeRaw = decodeField(fields[3]);
      const caseValue = parseEnum(fields[4], WORD_TYPE_CASES);
      const tense = parseEnum(fields[5], WORD_TYPE_TENSES);
      const voice = parseEnum(fields[6], WORD_TYPE_VOICES);
      const view = parseEnum(fields[7], WORD_TYPE_VIEWS);
      const detailPage = parsePositiveInt(fields[8]);
      if (
        tashkeelWordId === null ||
        contextCodeRaw === null ||
        contextCodeRaw.length === 0 ||
        caseValue === null ||
        tense === null ||
        voice === null ||
        view === null ||
        detailPage === null
      ) {
        return null;
      }
      return {
        kind: 'wordType',
        tashkeelWordId,
        contextCode: contextCodeRaw,
        case: caseValue,
        tense,
        voice,
        view,
        detailPage,
      };
    }
    default:
      return null;
  }
}

export interface SerializedDetailOverlayParams {
  /** Values for the repeated `qdDetail` key, bottom → top. */
  readonly frames: readonly string[];
  /** Value for `qdDetailOpen`, or null when the key must be absent. */
  readonly open: '1' | null;
}

export function serializeDetailOverlayState(state: DetailOverlayUrlState): SerializedDetailOverlayParams {
  return {
    frames: state.stack.map(serializeDetailFrame),
    open: state.visibility === 'open' && state.stack.length > 0 ? '1' : null,
  };
}

export interface ParsedDetailOverlayParams {
  readonly state: DetailOverlayUrlState;
  /** False when the raw params must be rewritten (replace semantics) to match `state`. */
  readonly isCanonical: boolean;
}

/**
 * Parses the raw repeated `qdDetail` values plus the raw `qdDetailOpen` value.
 *
 * - An invalid first frame means no overlay.
 * - A malformed later frame truncates the stack immediately before it.
 * - More than the cap keeps only the first `DETAIL_OVERLAY_MAX_FRAMES` frames.
 * - `qdDetailOpen=1` without a valid frame canonicalizes to closed/no overlay.
 */
export function parseDetailOverlayParams(
  rawFrames: readonly string[],
  rawOpen: string | null,
): ParsedDetailOverlayParams {
  const stack: DetailFrame[] = [];
  for (const raw of rawFrames) {
    const frame = parseDetailFrame(raw);
    if (frame === null || stack.length >= DETAIL_OVERLAY_MAX_FRAMES) {
      break;
    }
    stack.push(frame);
  }

  const state: DetailOverlayUrlState =
    stack.length === 0
      ? CLOSED_DETAIL_OVERLAY_STATE
      : { visibility: rawOpen === '1' ? 'open' : 'closed', stack };

  const canonical = serializeDetailOverlayState(state);
  const isCanonical =
    canonical.frames.length === rawFrames.length &&
    canonical.frames.every((value, index) => value === rawFrames[index]) &&
    canonical.open === rawOpen;

  return { state, isCanonical };
}
