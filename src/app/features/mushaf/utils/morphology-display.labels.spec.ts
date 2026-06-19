import { describe, expect, it } from 'vitest';

import { morphologyTextOrDash } from './morphology-display.labels';

describe('morphologyTextOrDash', () => {
  it('returns trimmed text when present', () => {
    expect(morphologyTextOrDash('ك ف ر')).toBe('ك ف ر');
    expect(morphologyTextOrDash('  اسم  ')).toBe('اسم');
  });

  it('returns an em dash for empty, null, or undefined values', () => {
    expect(morphologyTextOrDash(null)).toBe('—');
    expect(morphologyTextOrDash(undefined)).toBe('—');
    expect(morphologyTextOrDash('')).toBe('—');
    expect(morphologyTextOrDash('   ')).toBe('—');
  });
});
