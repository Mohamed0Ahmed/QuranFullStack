import { describe, expect, it } from 'vitest';

import { toMushafWordDisplayText } from './mushaf-word-display-text';

const SYNTHETIC_WORD = 'كلمة-تجريبية-١';
const WAQF_THREE_DOTS = '\u06DB';
const WAQF_HIGH_JEEM = '\u06DA';
const ROUNDED_ZERO = '\u06DF';

describe('toMushafWordDisplayText', () => {
  it('removes trailing space before a Quranic waqf mark so the mark stays on the word', () => {
    const raw = `${SYNTHETIC_WORD} ${WAQF_THREE_DOTS}`;

    expect(toMushafWordDisplayText(raw)).toBe(`${SYNTHETIC_WORD}${WAQF_THREE_DOTS}`);
  });

  it('keeps the Quranic mark in the visual output', () => {
    const raw = `${SYNTHETIC_WORD} ${WAQF_HIGH_JEEM}`;

    expect(toMushafWordDisplayText(raw)).toContain(WAQF_HIGH_JEEM);
  });

  it('does not strip marks attached directly to the word body', () => {
    const raw = `${SYNTHETIC_WORD}${ROUNDED_ZERO}`;

    expect(toMushafWordDisplayText(raw)).toBe(raw);
  });

  it('leaves words without waqf marks unchanged', () => {
    expect(toMushafWordDisplayText(SYNTHETIC_WORD)).toBe(SYNTHETIC_WORD);
  });
});
