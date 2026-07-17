import { describe, expect, it } from 'vitest';

import { WordAnalysisViewModel, WordIdentityDto, WordMorphologyDto } from '../models/mushaf.models';
import { wordTypeDetailFrameFromAnalysis } from './word-type-detail-frame.adapter';

const SYNTH_WORD_TEXT = 'SYNTH_كلمة-تجريبية';
const SYNTH_WORD_KEY = 'SYNTH_مفتاح-كلمة';

function buildIdentity(overrides: Partial<WordIdentityDto['uniqueTashkeel']> = {}): WordIdentityDto {
  return {
    orderedTashkeel: { occurrencesCount: 7, ayahsCount: 7, surahsCount: 3 },
    orderedSimple: { occurrencesCount: 9, ayahsCount: 9, surahsCount: 4 },
    uniqueTashkeel: { id: 101, occurrencesCount: 7, ayahsCount: 7, surahsCount: 3, ...overrides },
    uniqueSimple: {
      id: 202,
      occurrencesCount: 9,
      ayahsCount: 9,
      surahsCount: 4,
      wordKeyImlaeiSimple: SYNTH_WORD_KEY,
    },
  };
}

function buildAnalysis(
  morphology: Partial<WordMorphologyDto> = {},
  identity: WordIdentityDto = buildIdentity(),
): WordAnalysisViewModel {
  return {
    word: {
      quranWordId: 2003,
      wordLocation: '2:25:3',
      verseKey: '2:25',
      surahNumber: 2,
      ayahNumber: 25,
      wordNumber: 3,
      pageNumber: 5,
      lineNumber: 1,
      lineWordOrder: 3,
      textUthmani: SYNTH_WORD_TEXT,
      textUthmaniSimple: SYNTH_WORD_TEXT,
      textImlaeiSimple: SYNTH_WORD_TEXT,
      qpcGlyph: null,
    },
    identity,
    morphology: {
      headPos: 'V',
      headPosLabel: { ar: 'فعل', en: 'Verb' },
      root: null,
      lemma: null,
      stem: null,
      isVerb: true,
      verbTense: 'past',
      verbVoice: 'active',
      caseFeature: null,
      ...morphology,
    },
    segments: [],
  };
}

describe('wordTypeDetailFrameFromAnalysis (plan §5.7, locked Option A)', () => {
  it('uses the verb tense as the context code for a verb with a tense', () => {
    const frame = wordTypeDetailFrameFromAnalysis(buildAnalysis({ isVerb: true, verbTense: 'past' }));

    expect(frame).not.toBeNull();
    expect(frame?.contextCode).toBe('past');
    expect(frame?.tashkeelWordId).toBe(101);
  });

  it('maps a verb with a null tense to the "unspecified" context code', () => {
    const frame = wordTypeDetailFrameFromAnalysis(buildAnalysis({ isVerb: true, verbTense: null }));

    expect(frame?.contextCode).toBe('unspecified');
  });

  it('uses the head POS as the context code for a non-verb', () => {
    const frame = wordTypeDetailFrameFromAnalysis(
      buildAnalysis({ isVerb: false, verbTense: null, verbVoice: null, headPos: 'N', headPosLabel: { ar: 'اسم', en: 'Noun' } }),
    );

    expect(frame?.contextCode).toBe('N');
  });

  it.each([
    ['zero id', 0],
    ['negative id', -5],
    ['non-integer id', 101.5],
    ['missing id', undefined as unknown as number],
  ])('returns null when the unique tashkeel identity has a %s', (_label, id) => {
    const frame = wordTypeDetailFrameFromAnalysis(buildAnalysis({}, buildIdentity({ id })));

    expect(frame).toBeNull();
  });

  it.each([
    ['empty', ''],
    ['blank', '   '],
  ])('returns null for a non-verb with a %s head POS instead of guessing from the label', (_label, headPos) => {
    const frame = wordTypeDetailFrameFromAnalysis(buildAnalysis({ isVerb: false, headPos }));

    expect(frame).toBeNull();
  });

  it('returns null for a verb whose tense is a blank string instead of guessing', () => {
    const frame = wordTypeDetailFrameFromAnalysis(buildAnalysis({ isVerb: true, verbTense: '   ' }));

    expect(frame).toBeNull();
  });

  it.each([
    ['verb with tense', buildAnalysis({ isVerb: true, verbTense: 'present' })],
    ['verb without tense', buildAnalysis({ isVerb: true, verbTense: null })],
    [
      'non-verb with case and voice features on the clicked occurrence',
      buildAnalysis({ isVerb: false, headPos: 'N', caseFeature: 'genitive', verbVoice: 'passive' }),
    ],
  ])('always opens the complete type row at all/all/all, ayahs view, page 1 — %s', (_label, analysis) => {
    const frame = wordTypeDetailFrameFromAnalysis(analysis);

    expect(frame).not.toBeNull();
    expect(frame?.kind).toBe('wordType');
    expect(frame?.case).toBe('all');
    expect(frame?.tense).toBe('all');
    expect(frame?.voice).toBe('all');
    expect(frame?.view).toBe('ayahs');
    expect(frame?.detailPage).toBe(1);
  });

  it('never narrows the frame to the clicked occurrence tense or voice', () => {
    const frame = wordTypeDetailFrameFromAnalysis(
      buildAnalysis({ isVerb: true, verbTense: 'past', verbVoice: 'passive', caseFeature: 'accusative' }),
    );

    expect(frame?.tense).toBe('all');
    expect(frame?.voice).toBe('all');
    expect(frame?.case).toBe('all');
  });
});
