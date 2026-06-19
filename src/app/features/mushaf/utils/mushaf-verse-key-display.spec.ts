import { describe, expect, it } from 'vitest';

import { formatCoverageAyahNumbers, toStudyAyahDisplayText } from './mushaf-verse-key-display';

describe('formatCoverageAyahNumbers', () => {
  it('extracts ayah numbers from verse keys', () => {
    expect(formatCoverageAyahNumbers(['2:30', '2:31', '2:32'])).toBe('30، 31، 32');
  });

  it('returns keys without a colon unchanged', () => {
    expect(formatCoverageAyahNumbers(['orphan-key'])).toBe('orphan-key');
  });
});

describe('toStudyAyahDisplayText', () => {
  it('removes a trailing ayah-end number from the text', () => {
    expect(toStudyAyahDisplayText('بِسْمِ اللَّهِ الرَّحْمَٰنِ الرَّحِيمِ ١')).toBe(
      'بِسْمِ اللَّهِ الرَّحْمَٰنِ الرَّحِيمِ',
    );
  });

  it('returns plain text unchanged when no trailing marker exists', () => {
    expect(toStudyAyahDisplayText('نص تجريبي للآية')).toBe('نص تجريبي للآية');
  });
});
