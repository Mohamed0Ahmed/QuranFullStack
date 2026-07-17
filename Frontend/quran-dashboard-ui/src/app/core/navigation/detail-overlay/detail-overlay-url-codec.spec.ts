import { describe, expect, it } from 'vitest';

import {
  DETAIL_OVERLAY_MAX_FRAMES,
  DetailFrame,
  LemmaDetailFrame,
  RootDetailFrame,
  StemDetailFrame,
  UniqueDetailFrame,
  WordTypeDetailFrame,
  detailFramesEqual,
} from './detail-overlay.models';
import {
  parseDetailFrame,
  parseDetailOverlayParams,
  serializeDetailFrame,
  serializeDetailOverlayState,
} from './detail-overlay-url-codec';

const uniqueFrame: UniqueDetailFrame = { kind: 'unique', mode: 'simple', id: 42, view: 'ayahs', ayahPage: 3 };
const rootFrame: RootDetailFrame = {
  kind: 'root',
  id: 999,
  view: 'words',
  wordView: 'simple',
  surahView: 'mentioned',
  detailPage: 1,
};
const lemmaFrame: LemmaDetailFrame = {
  kind: 'lemma',
  id: 555,
  view: 'ayahs',
  wordView: 'simple',
  surahView: 'mentioned',
  detailPage: 1,
  typeCode: null,
};
const stemFrame: StemDetailFrame = {
  kind: 'stem',
  id: 77,
  view: 'lemmas',
  wordView: 'tashkeel',
  surahView: 'missing',
  detailPage: 4,
  typeCode: 'V',
};
const wordTypeFrame: WordTypeDetailFrame = {
  kind: 'wordType',
  tashkeelWordId: 191001,
  contextCode: 'past',
  case: 'all',
  tense: 'all',
  voice: 'all',
  view: 'ayahs',
  detailPage: 1,
};

describe('detail-overlay-url-codec', () => {
  describe('serialize/parse round trips', () => {
    it.each([
      ['unique', uniqueFrame, 'v1~unique~simple~42~ayahs~3'],
      ['root', rootFrame, 'v1~root~999~words~simple~mentioned~1'],
      ['lemma with null typeCode', lemmaFrame, 'v1~lemma~555~ayahs~simple~mentioned~1~-'],
      ['stem with typeCode', stemFrame, 'v1~stem~77~lemmas~tashkeel~missing~4~V'],
      ['wordType', wordTypeFrame, 'v1~wordType~191001~past~all~all~all~ayahs~1'],
    ] as const)('serializes and re-parses the %s frame (defaults always explicit)', (_label, frame, expected) => {
      const serialized = serializeDetailFrame(frame);
      expect(serialized).toBe(expected);
      const parsed = parseDetailFrame(serialized);
      expect(parsed).not.toBeNull();
      expect(detailFramesEqual(parsed as DetailFrame, frame)).toBe(true);
    });

    it('escapes the separator and the null sentinel inside string fields', () => {
      const oddCode: WordTypeDetailFrame = { ...wordTypeFrame, contextCode: 'a~b' };
      const serializedOdd = serializeDetailFrame(oddCode);
      expect(serializedOdd).toContain('a%7Eb');
      expect(parseDetailFrame(serializedOdd)).toEqual(oddCode);

      const sentinelCode: StemDetailFrame = { ...stemFrame, typeCode: '-' };
      const serializedSentinel = serializeDetailFrame(sentinelCode);
      expect(serializedSentinel.endsWith('~%2D')).toBe(true);
      expect(parseDetailFrame(serializedSentinel)).toEqual(sentinelCode);
    });
  });

  describe('strict single-frame parsing', () => {
    it.each([
      ['unknown version', 'v2~root~999~words~simple~mentioned~1'],
      ['unknown kind', 'v1~gate~999~words~simple~mentioned~1'],
      ['zero id', 'v1~root~0~words~simple~mentioned~1'],
      ['negative id', 'v1~root~-3~words~simple~mentioned~1'],
      ['non-numeric id', 'v1~root~abc~words~simple~mentioned~1'],
      ['an id just above Number.MAX_SAFE_INTEGER', 'v1~root~9007199254740993~words~simple~mentioned~1'],
      ['an id from a very long digit run', `v1~root~${'9'.repeat(400)}~words~simple~mentioned~1`],
      ['an unsafe page', 'v1~root~999~words~simple~mentioned~9007199254740993'],
      ['an unsafe unique id', 'v1~unique~simple~9007199254740993~ayahs~3'],
      ['an unsafe tashkeel word id', `v1~wordType~${'1'.repeat(30)}~past~all~all~all~ayahs~1`],
      ['invalid view enum', 'v1~root~999~sentences~simple~mentioned~1'],
      ['invalid wordView enum', 'v1~root~999~words~uthmani~mentioned~1'],
      ['invalid page', 'v1~root~999~words~simple~mentioned~0'],
      ['missing field', 'v1~root~999~words~simple~mentioned'],
      ['extra field', 'v1~root~999~words~simple~mentioned~1~extra'],
      ['stems view on lemma frame', 'v1~lemma~555~lemmas~simple~mentioned~1~-'],
      ['empty typeCode', 'v1~stem~77~words~simple~mentioned~1~'],
      ['empty contextCode', 'v1~wordType~1~~all~all~all~ayahs~1'],
      ['invalid case enum', 'v1~wordType~1~N~vocative~all~all~ayahs~1'],
      ['malformed percent-encoding', 'v1~wordType~1~%E0%A4%A~all~all~all~ayahs~1'],
    ] as const)('rejects a frame with %s', (_label, raw) => {
      expect(parseDetailFrame(raw)).toBeNull();
    });

    it('still accepts the largest exactly-representable id and preserves it', () => {
      const parsed = parseDetailFrame(`v1~root~${Number.MAX_SAFE_INTEGER}~words~simple~mentioned~1`);
      expect(parsed).not.toBeNull();
      expect((parsed as RootDetailFrame).id).toBe(Number.MAX_SAFE_INTEGER);
      // The id must survive the round trip exactly, not round to a neighbouring entity.
      expect(serializeDetailFrame(parsed as DetailFrame)).toBe(`v1~root~${Number.MAX_SAFE_INTEGER}~words~simple~mentioned~1`);
    });
  });

  describe('parseDetailOverlayParams', () => {
    const validRoot = serializeDetailFrame(rootFrame);
    const validLemma = serializeDetailFrame(lemmaFrame);
    const validUnique = serializeDetailFrame(uniqueFrame);

    it('retains repeated-key order bottom → top', () => {
      const { state, isCanonical } = parseDetailOverlayParams([validRoot, validLemma, validUnique], '1');
      expect(state.visibility).toBe('open');
      expect(state.stack.map((frame) => frame.kind)).toEqual(['root', 'lemma', 'unique']);
      expect(isCanonical).toBe(true);
    });

    it('yields no overlay when the first frame is invalid', () => {
      const { state, isCanonical } = parseDetailOverlayParams(['v1~garbage', validLemma], '1');
      expect(state.visibility).toBe('closed');
      expect(state.stack).toHaveLength(0);
      expect(isCanonical).toBe(false);
    });

    it('truncates the stack immediately before a malformed later frame', () => {
      const { state, isCanonical } = parseDetailOverlayParams([validRoot, 'v1~root~x', validUnique], '1');
      expect(state.stack.map((frame) => frame.kind)).toEqual(['root']);
      expect(isCanonical).toBe(false);
    });

    it('truncates the stack before a frame whose id exceeds the safe-integer range', () => {
      const { state, isCanonical } = parseDetailOverlayParams(
        [validRoot, 'v1~root~9007199254740993~words~simple~mentioned~1', validUnique],
        '1',
      );
      expect(state.stack.map((frame) => frame.kind)).toEqual(['root']);
      expect(isCanonical).toBe(false);
    });

    it('yields no overlay when the only frame carries an unsafe id', () => {
      const { state, isCanonical } = parseDetailOverlayParams(['v1~root~9007199254740993~words~simple~mentioned~1'], '1');
      expect(state.visibility).toBe('closed');
      expect(state.stack).toHaveLength(0);
      expect(isCanonical).toBe(false);
    });

    it('caps parsed stacks at the maximum frame count', () => {
      const frames = Array.from({ length: DETAIL_OVERLAY_MAX_FRAMES + 2 }, () => validRoot);
      const { state, isCanonical } = parseDetailOverlayParams(frames, '1');
      expect(state.stack).toHaveLength(DETAIL_OVERLAY_MAX_FRAMES);
      expect(isCanonical).toBe(false);
    });

    it('canonicalizes qdDetailOpen=1 without a valid frame to closed/no overlay', () => {
      const { state, isCanonical } = parseDetailOverlayParams([], '1');
      expect(state.visibility).toBe('closed');
      expect(state.stack).toHaveLength(0);
      expect(isCanonical).toBe(false);
    });

    it('parses a retained closed stack (frames present, open key absent)', () => {
      const { state, isCanonical } = parseDetailOverlayParams([validRoot, validLemma], null);
      expect(state.visibility).toBe('closed');
      expect(state.stack).toHaveLength(2);
      expect(isCanonical).toBe(true);
    });

    it('treats a non-1 open value as closed and non-canonical', () => {
      const { state, isCanonical } = parseDetailOverlayParams([validRoot], 'yes');
      expect(state.visibility).toBe('closed');
      expect(isCanonical).toBe(false);
    });

    it('flags a non-canonical frame encoding for a rewrite', () => {
      // Same meaning, different spelling: contextCode 'past' written as '%70ast'.
      const { state, isCanonical } = parseDetailOverlayParams(['v1~wordType~191001~%70ast~all~all~all~ayahs~1'], '1');
      expect(state.stack).toHaveLength(1);
      expect(isCanonical).toBe(false);
    });
  });

  describe('URL growth budget', () => {
    it('keeps a maximum-size serialized stack under 1500 characters', () => {
      const bigLemma: LemmaDetailFrame = {
        kind: 'lemma',
        id: 2147483647,
        view: 'surahs',
        wordView: 'tashkeel',
        surahView: 'mentioned',
        detailPage: 99999,
        typeCode: 'LONGCODE',
      };
      const bigWordType: WordTypeDetailFrame = {
        kind: 'wordType',
        tashkeelWordId: 2147483647,
        contextCode: 'imperative',
        case: 'nominative',
        tense: 'imperative',
        voice: 'passive',
        view: 'surahs',
        detailPage: 99999,
      };
      const stack: DetailFrame[] = [bigLemma, bigWordType, bigLemma, bigWordType, bigLemma, bigWordType, bigLemma, bigWordType];
      const { frames } = serializeDetailOverlayState({ visibility: 'open', stack });
      const query = frames.map((value) => `qdDetail=${encodeURIComponent(value)}`).join('&') + '&qdDetailOpen=1';
      expect(stack).toHaveLength(DETAIL_OVERLAY_MAX_FRAMES);
      expect(query.length).toBeLessThan(1500);
    });
  });
});
