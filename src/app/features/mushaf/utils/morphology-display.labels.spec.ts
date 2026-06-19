import { describe, expect, it } from 'vitest';

import { morphologyFeatureLabelAr, morphologyTextOrDash } from './morphology-display.labels';

describe('morphologyFeatureLabelAr', () => {
  it('maps known API morphology values to Arabic labels', () => {
    expect(morphologyFeatureLabelAr('past')).toBe('ماضٍ');
    expect(morphologyFeatureLabelAr('present')).toBe('مضارع');
    expect(morphologyFeatureLabelAr('imperative')).toBe('أمر');
    expect(morphologyFeatureLabelAr('active')).toBe('معلوم');
    expect(morphologyFeatureLabelAr('passive')).toBe('مجهول');
    expect(morphologyFeatureLabelAr('nominative')).toBe('مرفوع');
    expect(morphologyFeatureLabelAr('accusative')).toBe('منصوب');
    expect(morphologyFeatureLabelAr('genitive')).toBe('مجرور');
  });

  it('returns an em dash for unknown or empty values', () => {
    expect(morphologyFeatureLabelAr(null)).toBe('—');
    expect(morphologyFeatureLabelAr(undefined)).toBe('—');
    expect(morphologyFeatureLabelAr('unknown-value')).toBe('—');
  });
});

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
